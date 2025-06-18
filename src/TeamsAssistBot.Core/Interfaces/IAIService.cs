using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface IAIService
{
    Task<AIResponse> GenerateResponseAsync(string userMessage, string callId, string context = "");
    Task<string> ProcessTranscriptionAsync(string transcribedText);
    Task<AIResponse> CreateAudioResponseAsync(string responseText, string callId);
    Task<bool> ValidateInputAsync(string input);
    Task<string> CleanupTextAsync(string text);
    Task<string> GetContextualResponseAsync(string question, List<string> conversationHistory);
}
