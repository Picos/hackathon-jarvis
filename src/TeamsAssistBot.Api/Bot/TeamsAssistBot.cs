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
    private readonly IAvatarAnimationService _avatarService;
    private readonly Dictionary<string, List<string>> _conversationHistory;

    public TeamsAssistBotHandler(
        ILogger<TeamsAssistBotHandler> logger,
        IAIService aiService,
        ISpeechService speechService,
        ITeamsMeetingService meetingService,
        IAvatarAnimationService avatarService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _meetingService = meetingService ?? throw new ArgumentNullException(nameof(meetingService));
        _avatarService = avatarService ?? throw new ArgumentNullException(nameof(avatarService));
        _conversationHistory = new Dictionary<string, List<string>>();

        _logger.LogInformation("TeamsAssistBotHandler constructor called");

        try
        {
            // Subscribe to meeting service events
            _meetingService.AudioDataReceived += OnAudioDataReceived;
            _meetingService.CallStateChanged += OnCallStateChanged;
            _meetingService.ParticipantJoined += OnParticipantJoined;
            _meetingService.ParticipantLeft += OnParticipantLeft;
            
            _logger.LogInformation("TeamsAssistBotHandler initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TeamsAssistBotHandler initialization");
            throw;
        }

        // Subscribe to avatar animation events
        _avatarService.AnimationStateChanged += OnAvatarStateChanged;
        _avatarService.FrameGenerated += OnAvatarFrameGenerated;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            var userMessage = turnContext.Activity.Text;
            var conversationId = turnContext.Activity.Conversation.Id;

            _logger.LogInformation("Received message: {Message} from conversation: {ConversationId}", userMessage, conversationId);

            // Check if message is null or empty
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                _logger.LogWarning("Received empty or null message from conversation: {ConversationId}", conversationId);
                await turnContext.SendActivityAsync(MessageFactory.Text("I didn't receive any message. Could you please try again?"), cancellationToken);
                return;
            }

            // Validate input
            _logger.LogDebug("Validating input message");
            if (!await _aiService.ValidateInputAsync(userMessage))
            {
                _logger.LogWarning("Message validation failed for message: {Message}", userMessage);
                await turnContext.SendActivityAsync(MessageFactory.Text("I'm sorry, but I can't process that message."), cancellationToken);
                return;
            }

            // Process the message and generate response
            _logger.LogDebug("Processing message transcription");
            var processedMessage = await _aiService.ProcessTranscriptionAsync(userMessage);
            _logger.LogDebug("Processed message: {ProcessedMessage}", processedMessage);
            
            // Get conversation history for context
            var history = GetConversationHistory(conversationId);
            _logger.LogDebug("Retrieved conversation history with {Count} items", history.Count);
            
            var contextualResponse = await _aiService.GetContextualResponseAsync(processedMessage, history);
            _logger.LogDebug("Generated contextual response: {Response}", contextualResponse);

            // Add to conversation history
            AddToConversationHistory(conversationId, userMessage);
            AddToConversationHistory(conversationId, $"Jarvis: {contextualResponse}");

            // Send response
            _logger.LogDebug("Sending response to Teams");
            await turnContext.SendActivityAsync(MessageFactory.Text(contextualResponse), cancellationToken);

            _logger.LogInformation("Successfully sent response: {Response} to conversation: {ConversationId}", contextualResponse, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message activity. Message: {Message}, ConversationId: {ConversationId}", 
                turnContext.Activity?.Text, turnContext.Activity?.Conversation?.Id);
            
            try
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I'm sorry, I encountered an error processing your message. Please try again."), cancellationToken);
            }
            catch (Exception sendEx)
            {
                _logger.LogError(sendEx, "Failed to send error message to user");
            }
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("OnMembersAddedAsync called. Members count: {Count}, ConversationId: {ConversationId}", 
                membersAdded?.Count ?? 0, turnContext.Activity?.Conversation?.Id);

            var welcomeText = "Hello! I'm Jarvis, your AI assistant for this Teams meeting. " +
                             "You can interact with me by sending messages here, and I'll respond with helpful information. " +
                             "Feel free to ask me questions or request assistance!";

            foreach (var member in membersAdded)
            {
                _logger.LogInformation("Member added: {MemberId}, {MemberName}, Recipient: {RecipientId}", 
                    member.Id, member.Name, turnContext.Activity.Recipient.Id);

                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    _logger.LogInformation("Sending welcome message to new member: {MemberId}", member.Id);
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
                    _logger.LogInformation("Welcome message sent successfully to member: {MemberId}", member.Id);
                }
                else
                {
                    _logger.LogInformation("Skipping welcome message for bot itself: {MemberId}", member.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnMembersAddedAsync");
            
            // Try to send a simple fallback message
            try
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Jarvis is here and ready to help!"), cancellationToken);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to send fallback welcome message");
            }
        }
    }

    protected override async Task OnTeamsMeetingStartAsync(MeetingStartEventDetails meetingStartEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Teams meeting started: {MeetingId}, JoinUrl: {JoinUrl}", 
                meetingStartEventDetails.Id, meetingStartEventDetails.JoinUrl?.ToString());

            // Send welcome message immediately without trying to join the call
            // Initialize avatar for this meeting
            var avatarConfig = new AvatarConfiguration
            {
                EnableAnimations = true,
                AnimationStyle = "Professional",
                BlinkFrequency = BlinkPattern.Natural,
                LipSyncSensitivity = LipSyncSensitivity.Medium,
                IdleAnimations = true
            };

            await _avatarService.InitializeAvatarAsync(callId, avatarConfig);
            await _avatarService.SetIdleAnimationAsync(callId);

            var welcomeMessage = "Jarvis has joined the meeting and is ready to assist! " +
                               "You can send me messages in this chat, and I'll respond with helpful information.";
            
            await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
            _logger.LogInformation("Sent welcome message for meeting: {MeetingId}", meetingStartEventDetails.Id);

            // Optional: Try to join meeting call (in a simplified way)
            try
            {
                if (!string.IsNullOrEmpty(meetingStartEventDetails.JoinUrl?.ToString()))
                {
                    var callId = await _meetingService.JoinMeetingAsync(meetingStartEventDetails.JoinUrl.ToString(), "Jarvis");
                    _logger.LogInformation("Successfully joined meeting call with ID: {CallId}", callId);
                    
                    // Don't start media streaming for now to avoid issues
                    // await _meetingService.StartMediaStreamingAsync(callId);
                }
            }
            catch (Exception joinEx)
            {
                _logger.LogWarning(joinEx, "Failed to join meeting call, but bot will continue to work in chat mode");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling meeting start event for meeting: {MeetingId}", meetingStartEventDetails?.Id);
            
            // Try to send a fallback message
            try
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Jarvis is here and ready to help!"), cancellationToken);
            }
            catch (Exception msgEx)
            {
                _logger.LogError(msgEx, "Failed to send fallback welcome message");
            }
        }
    }

    protected override async Task OnTeamsMeetingEndAsync(MeetingEndEventDetails meetingEndEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Teams meeting ended: {MeetingId}", meetingEndEventDetails.Id);

            await _meetingService.LeaveMeetingAsync(meetingEndEventDetails.Id);
            
            // Clean up avatar for this meeting
            await _avatarService.DisposeAvatarAsync(meetingEndEventDetails.Id);
            
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

            // Update avatar listening animation based on incoming audio
            await _avatarService.StartListeningAnimationAsync(audioData.CallId, audioData);

            // Transcribe the audio
            var transcription = await _speechService.TranscribeAudioAsync(audioData);
            
            if (transcription.ContainsWakeWord && !string.IsNullOrEmpty(transcription.Text))
            {
                _logger.LogInformation("Wake word detected in call {CallId}: {Text}", audioData.CallId, transcription.Text);

                // Show processing animation while AI is thinking
                await _avatarService.StartProcessingAnimationAsync(audioData.CallId);

                // Process the transcribed text
                var processedText = await _aiService.ProcessTranscriptionAsync(transcription.Text);
                
                if (!string.IsNullOrEmpty(processedText))
                {
                    // Generate AI response
                    var aiResponse = await _aiService.GenerateResponseAsync(processedText, audioData.CallId);
                    
                    // Send response via chat
                    await _meetingService.SendChatMessageAsync(audioData.CallId, aiResponse.ResponseText);
                    
                    // Handle audio response with lip-sync animation
                    if (aiResponse.Type == ResponseType.Both || aiResponse.Type == ResponseType.Audio)
                    {
                        if (aiResponse.AudioResponse != null)
                        {
                            // Calculate audio duration for lip-sync
                            var audioDuration = CalculateAudioDuration(aiResponse.AudioResponse);
                            
                            // Start speaking animation with lip-sync
                            await _avatarService.StartSpeakingAnimationAsync(audioData.CallId, aiResponse.AudioResponse, audioDuration);
                            
                            // Inject the audio
                            await _meetingService.InjectAudioAsync(audioData.CallId, aiResponse.AudioResponse);
                        }
                    }
                    else
                    {
                        // Text-only response - return to idle
                        await _avatarService.SetIdleAnimationAsync(audioData.CallId);
                    }
                }
                else
                {
                    // No response generated - return to idle
                    await _avatarService.SetIdleAnimationAsync(audioData.CallId);
                }
            }
            else
            {
                // No wake word detected - return to idle after a short delay
                _ = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(async _ =>
                {
                    await _avatarService.SetIdleAnimationAsync(audioData.CallId);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data for call: {CallId}", audioData.CallId);
            
            // Ensure avatar returns to idle state on error
            try
            {
                await _avatarService.SetIdleAnimationAsync(audioData.CallId);
            }
            catch (Exception avatarEx)
            {
                _logger.LogError(avatarEx, "Error setting avatar to idle state after audio processing error");
            }
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

    private void OnAvatarStateChanged(object? sender, AvatarAnimationEventArgs e)
    {
        _logger.LogDebug("Avatar state changed for call {CallId}: {PreviousState} -> {NewState}", 
            e.CallId, e.PreviousState, e.NewState);
    }

    private void OnAvatarFrameGenerated(object? sender, AnimationFrameEventArgs e)
    {
        _logger.LogDebug("Avatar frame generated for call {CallId}: State={State}, MouthOpen={MouthOpen}", 
            e.CallId, e.Frame.State, e.Frame.MouthPosition.OpenAmount);
    }

    private TimeSpan CalculateAudioDuration(byte[] audioData)
    {
        // Calculate duration based on standard PCM audio format
        // Assuming 16-bit, 16kHz mono audio (which is typical for speech synthesis)
        const int sampleRate = 16000; // Hz
        const int bitsPerSample = 16;
        const int channels = 1;
        
        var bytesPerSecond = sampleRate * (bitsPerSample / 8) * channels;
        var durationSeconds = (double)audioData.Length / bytesPerSecond;
        
        return TimeSpan.FromSeconds(Math.Max(durationSeconds, 0.1)); // Minimum 100ms
    }
}
