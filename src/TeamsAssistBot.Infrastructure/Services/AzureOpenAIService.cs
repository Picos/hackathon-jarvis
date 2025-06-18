using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;
using System.Text;

namespace TeamsAssistBot.Infrastructure.Services;

public class AzureOpenAIService : IAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly ISpeechService _speechService;

    public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger, ISpeechService speechService)
    {
        _logger = logger;
        _speechService = speechService;

        var endpoint = configuration["AzureServices:OpenAI:Endpoint"];
        var apiKey = configuration["AzureServices:OpenAI:ApiKey"];
        _deploymentName = configuration["AzureServices:OpenAI:DeploymentName"] ?? "gpt-4";
        _maxTokens = int.Parse(configuration["AzureServices:OpenAI:MaxTokens"] ?? "1000");
        _temperature = 0.7;

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Azure OpenAI configuration is missing");
        }

        _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<AIResponse> GenerateResponseAsync(string userMessage, string callId, string context = "")
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(context);
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userMessage)
            };

            var chatCompletionsOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                MaxTokens = _maxTokens,
                Temperature = (float)_temperature,
                NucleusSamplingFactor = 0.95f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var responseText = response.Value.Choices[0].Message.Content;

            var cleanedResponse = await CleanupTextAsync(responseText);

            _logger.LogInformation("Generated AI response for call {CallId}: {Response}", callId, cleanedResponse);

            return new AIResponse
            {
                ResponseText = cleanedResponse,
                CallId = callId,
                Timestamp = DateTime.UtcNow,
                Type = ResponseType.Text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for call {CallId}", callId);
            return new AIResponse
            {
                ResponseText = "I apologize, but I'm having trouble processing your request right now. Please try again.",
                CallId = callId,
                Timestamp = DateTime.UtcNow,
                Type = ResponseType.Text
            };
        }
    }

    public async Task<string> ProcessTranscriptionAsync(string transcribedText)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
            return string.Empty;

        // Remove filler words and clean up the transcription
        var cleanedText = await CleanupTextAsync(transcribedText);

        // Extract question or intent from the transcription
        var processedText = ExtractIntent(cleanedText);

        _logger.LogInformation("Processed transcription: '{Original}' -> '{Processed}'", transcribedText, processedText);

        return processedText;
    }

    public async Task<AIResponse> CreateAudioResponseAsync(string responseText, string callId)
    {
        try
        {
            var audioData = await _speechService.SynthesizeSpeechAsync(responseText);

            return new AIResponse
            {
                ResponseText = responseText,
                CallId = callId,
                Timestamp = DateTime.UtcNow,
                Type = ResponseType.Both,
                AudioResponse = audioData,
                AudioFormat = "wav"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audio response for call {CallId}", callId);
            return new AIResponse
            {
                ResponseText = responseText,
                CallId = callId,
                Timestamp = DateTime.UtcNow,
                Type = ResponseType.Text
            };
        }
    }

    public async Task<bool> ValidateInputAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Check for inappropriate content, length limits, etc.
        if (input.Length > 2000)
            return false;

        // Basic content filtering (can be enhanced with Azure Content Safety)
        var inappropriateWords = new[] { "spam", "abuse", "inappropriate" };
        if (inappropriateWords.Any(word => input.ToLower().Contains(word)))
            return false;

        return await Task.FromResult(true);
    }

    public async Task<string> CleanupTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = text
            .Replace("um", "")
            .Replace("uh", "")
            .Replace("like", "")
            .Replace("you know", "")
            .Trim();

        // Remove extra spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        return await Task.FromResult(cleaned);
    }

    public async Task<string> GetContextualResponseAsync(string question, List<string> conversationHistory)
    {
        try
        {
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Previous conversation context:");

            foreach (var item in conversationHistory.TakeLast(5)) // Last 5 messages for context
            {
                contextBuilder.AppendLine($"- {item}");
            }

            var context = contextBuilder.ToString();
            var response = await GenerateResponseAsync(question, "contextual", context);

            return response.ResponseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contextual response");
            return "I'm sorry, I couldn't process your request with the current context.";
        }
    }

    private string BuildSystemPrompt(string context = "")
    {
        var systemPrompt = new StringBuilder();
        systemPrompt.AppendLine("You are Jarvis, an AI assistant integrated into Microsoft Teams meetings.");
        systemPrompt.AppendLine("You help users by answering questions, providing information, and assisting with meeting-related tasks.");
        systemPrompt.AppendLine("Keep your responses concise, helpful, and professional.");
        systemPrompt.AppendLine("You can only respond when explicitly addressed with 'Hey Jarvis' or similar wake words.");
        systemPrompt.AppendLine("If the user's request is unclear, ask for clarification.");
        systemPrompt.AppendLine("Focus on being helpful while being mindful of meeting context.");

        if (!string.IsNullOrEmpty(context))
        {
            systemPrompt.AppendLine($"\nAdditional context: {context}");
        }

        return systemPrompt.ToString();
    }

    private string ExtractIntent(string text)
    {
        // Simple intent extraction - can be enhanced with more sophisticated NLP
        var questionWords = new[] { "what", "how", "when", "where", "why", "who", "can you", "could you", "please" };
        
        if (questionWords.Any(word => text.ToLower().Contains(word)))
        {
            return text; // It's likely a question, return as-is
        }

        // If it's not clearly a question, it might be a statement or command
        return text;
    }
}
