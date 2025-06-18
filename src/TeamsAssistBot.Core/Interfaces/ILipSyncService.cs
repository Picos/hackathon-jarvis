using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface ILipSyncService
{
    Task<LipSyncData> AnalyzeAudioForLipSyncAsync(byte[] audioData, TimeSpan duration);
    Task<List<PhonemeData>> ExtractPhonemesAsync(byte[] audioData);
    Task<List<VolumePoint>> ExtractVolumeEnvelopeAsync(byte[] audioData);
    Task<List<AnimationFrame>> GenerateLipSyncFramesAsync(LipSyncData lipSyncData, string callId);
    MouthShape GetMouthShapeForPhoneme(string phoneme);
    double CalculateIntensityFromVolume(double volume);
}
