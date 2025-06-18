using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Api.Bot;

public class TeamsAssistBotHandler : TeamsActivityHandler
{
    private readonly ILogger<TeamsAssistBotHandler> _logger;
    private readonly IAIService _aiService;
    private readonly ISpeechService _speechService;
    private readonly ITeamsMeetingService _meetingService;
    private readonly Dictionary<string, List<string>> _conversationHistory;

    public TeamsAssistBotHandler(
        ILogger<TeamsAssistBotHandler> logger,
        IAIService aiService,
        ISpeechService speechService,
        ITeamsMeetingService meetingService)
    {
        _logger = logger;
        _aiService = aiService;
        _speechService = speechService;
        _meetingService = meetingService;
        _conversationHistory = new Dictionary<string, List<string>>();

        // Subscribe to meeting service events
        _meetingService.AudioDataReceived += OnAudioDataReceived;
        _meetingService.CallStateChanged += OnCallStateChanged;
        _meetingService.ParticipantJoined += OnParticipantJoined;
        _meetingService.ParticipantLeft += OnParticipantLeft;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            var userMessage = turnContext.Activity.Text;
            var conversationId = turnContext.Activity.Conversation.Id;

            _logger.LogInformation("Received message: {Message} from conversation: {ConversationId}", userMessage, conversationId);

            // Validate input
            if (!await _aiService.ValidateInputAsync(userMessage))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I'm sorry, but I can't process that message."), cancellationToken);
                return;
            }

            // Process the message and generate response
            var processedMessage = await _aiService.ProcessTranscriptionAsync(userMessage);
            
            // Get conversation history for context
            var history = GetConversationHistory(conversationId);
            var contextualResponse = await _aiService.GetContextualResponseAsync(processedMessage, history);

            // Add to conversation history
            AddToConversationHistory(conversationId, userMessage);
            AddToConversationHistory(conversationId, $"Jarvis: {contextualResponse}");

            // Send response
            await turnContext.SendActivityAsync(MessageFactory.Text(contextualResponse), cancellationToken);

            _logger.LogInformation("Sent response: {Response} to conversation: {ConversationId}", contextualResponse, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message activity");
            await turnContext.SendActivityAsync(MessageFactory.Text("I'm sorry, I encountered an error processing your message."), cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Hello! I'm Jarvis, your AI assistant for this Teams meeting. " +
                         "You can interact with me by saying 'Hey Jarvis' followed by your question or request. " +
                         "I can help answer questions, provide information, and assist with meeting-related tasks.";

        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
            }
        }
    }

    protected override async Task OnTeamsMeetingStartAsync(MeetingStartEventDetails meetingStartEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Teams meeting started: {MeetingId}", meetingStartEventDetails.Id);

            var callId = await _meetingService.JoinMeetingAsync(meetingStartEventDetails.JoinUrl?.ToString() ?? "", "Jarvis");
            await _meetingService.StartMediaStreamingAsync(callId);

            var welcomeMessage = "Jarvis has joined the meeting and is ready to assist. " +
                               "Say 'Hey Jarvis' to get my attention!";
            
            await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling meeting start event");
        }
    }

    protected override async Task OnTeamsMeetingEndAsync(MeetingEndEventDetails meetingEndEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Teams meeting ended: {MeetingId}", meetingEndEventDetails.Id);

            await _meetingService.LeaveMeetingAsync(meetingEndEventDetails.Id);
            
            // Clean up conversation history for this meeting
            if (_conversationHistory.ContainsKey(meetingEndEventDetails.Id))
            {
                _conversationHistory.Remove(meetingEndEventDetails.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling meeting end event");
        }
    }

    private async void OnAudioDataReceived(object? sender, AudioStreamData audioData)
    {
        try
        {
            _logger.LogDebug("Received audio data for call: {CallId}", audioData.CallId);

            // Transcribe the audio
            var transcription = await _speechService.TranscribeAudioAsync(audioData);
            
            if (transcription.ContainsWakeWord && !string.IsNullOrEmpty(transcription.Text))
            {
                _logger.LogInformation("Wake word detected in call {CallId}: {Text}", audioData.CallId, transcription.Text);

                // Process the transcribed text
                var processedText = await _aiService.ProcessTranscriptionAsync(transcription.Text);
                
                if (!string.IsNullOrEmpty(processedText))
                {
                    // Generate AI response
                    var aiResponse = await _aiService.GenerateResponseAsync(processedText, audioData.CallId);
                    
                    // Send response via chat
                    await _meetingService.SendChatMessageAsync(audioData.CallId, aiResponse.ResponseText);
                    
                    // Optionally inject audio response
                    if (aiResponse.Type == ResponseType.Both || aiResponse.Type == ResponseType.Audio)
                    {
                        if (aiResponse.AudioResponse != null)
                        {
                            await _meetingService.InjectAudioAsync(audioData.CallId, aiResponse.AudioResponse);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data for call: {CallId}", audioData.CallId);
        }
    }

    private void OnCallStateChanged(object? sender, string callId)
    {
        _logger.LogInformation("Call state changed for call: {CallId}", callId);
    }

    private void OnParticipantJoined(object? sender, string participantId)
    {
        _logger.LogInformation("Participant joined: {ParticipantId}", participantId);
    }

    private void OnParticipantLeft(object? sender, string participantId)
    {
        _logger.LogInformation("Participant left: {ParticipantId}", participantId);
    }

    private List<string> GetConversationHistory(string conversationId)
    {
        return _conversationHistory.GetValueOrDefault(conversationId, new List<string>());
    }

    private void AddToConversationHistory(string conversationId, string message)
    {
        if (!_conversationHistory.ContainsKey(conversationId))
        {
            _conversationHistory[conversationId] = new List<string>();
        }

        _conversationHistory[conversationId].Add(message);

        // Keep only last 20 messages to manage memory
        if (_conversationHistory[conversationId].Count > 20)
        {
            _conversationHistory[conversationId].RemoveAt(0);
        }
    }

    protected override async Task OnTeamsChannelCreatedAsync(ChannelInfo channelInfo, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeMessage = $"Welcome to the {channelInfo.Name} channel! I'm Jarvis, ready to help with your team activities.";
        await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
    }
}
