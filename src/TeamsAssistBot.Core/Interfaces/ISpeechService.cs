using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface ISpeechService
{
    Task<TranscriptionResult> TranscribeAudioAsync(AudioStreamData audioData);
    Task<TranscriptionResult> StartContinuousRecognitionAsync(string callId);
    Task StopContinuousRecognitionAsync(string callId);
    Task<WakeWordDetectionResult> DetectWakeWordAsync(AudioStreamData audioData);
    Task<byte[]> SynthesizeSpeechAsync(string text, string voice = "en-US-AriaNeural");
    Task<bool> IsWakeWordDetectedAsync(string transcribedText, string wakeWord);
}
