using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface IAudioProcessingService
{
    Task<AudioStreamData> ProcessRawAudioAsync(byte[] audioData, string callId, string participantId);
    Task<bool> ValidateAudioFormatAsync(AudioStreamData audioData);
    Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, int targetSampleRate, int targetChannels);
    Task StoreAudioStreamAsync(AudioStreamData audioData);
    Task<AudioStreamData?> GetAudioStreamAsync(string callId, DateTime timestamp);
}
