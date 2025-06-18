using Azure.AI.OpenAI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Infrastructure.Services;

public class AzureSpeechService : ISpeechService
{
    private readonly ILogger<AzureSpeechService> _logger;
    private readonly SpeechConfig _speechConfig;
    private readonly Dictionary<string, SpeechRecognizer> _activeRecognizers;
    private readonly string _wakeWord;

    public AzureSpeechService(IConfiguration configuration, ILogger<AzureSpeechService> logger)
    {
        _logger = logger;
        _activeRecognizers = new Dictionary<string, SpeechRecognizer>();

        var speechKey = configuration["AzureServices:SpeechService:SubscriptionKey"];
        var speechRegion = configuration["AzureServices:SpeechService:Region"];
        _wakeWord = configuration["AzureServices:SpeechService:WakeWord"] ?? "Hey Jarvis";

        if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
        {
            throw new InvalidOperationException("Speech service configuration is missing");
        }

        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechRecognitionLanguage = configuration["AzureServices:SpeechService:Language"] ?? "en-US";
    }

    public async Task<TranscriptionResult> TranscribeAudioAsync(AudioStreamData audioData)
    {
        try
        {
            using var audioStream = new MemoryStream(audioData.AudioData);
            using var audioFormat = AudioStreamFormat.GetWaveFormatPCM(
                (uint)audioData.SampleRate, 
                (byte)audioData.BitsPerSample, 
                (byte)audioData.Channels);
            using var audioInput = AudioConfig.FromStreamInput(audioStream);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioInput);

            var result = await recognizer.RecognizeOnceAsync();

            var transcriptionResult = new TranscriptionResult
            {
                Text = result.Text,
                Confidence = result.Reason == ResultReason.RecognizedSpeech ? 0.9 : 0.0,
                Timestamp = audioData.Timestamp,
                CallId = audioData.CallId,
                ParticipantId = audioData.ParticipantId,
                IsFinal = result.Reason == ResultReason.RecognizedSpeech,
                ContainsWakeWord = await IsWakeWordDetectedAsync(result.Text, _wakeWord)
            };

            _logger.LogInformation("Transcribed audio: {Text} (Confidence: {Confidence})", 
                transcriptionResult.Text, transcriptionResult.Confidence);

            return transcriptionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio for call {CallId}", audioData.CallId);
            return new TranscriptionResult
            {
                Text = string.Empty,
                Confidence = 0.0,
                Timestamp = audioData.Timestamp,
                CallId = audioData.CallId,
                ParticipantId = audioData.ParticipantId,
                IsFinal = false,
                ContainsWakeWord = false
            };
        }
    }

    public async Task<TranscriptionResult> StartContinuousRecognitionAsync(string callId)
    {
        try
        {
            if (_activeRecognizers.ContainsKey(callId))
            {
                await StopContinuousRecognitionAsync(callId);
            }

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var tcs = new TaskCompletionSource<TranscriptionResult>();

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    var result = new TranscriptionResult
                    {
                        Text = e.Result.Text,
                        Confidence = 0.9,
                        Timestamp = DateTime.UtcNow,
                        CallId = callId,
                        ParticipantId = "continuous",
                        IsFinal = true,
                        ContainsWakeWord = IsWakeWordDetectedAsync(e.Result.Text, _wakeWord).Result
                    };
                    tcs.TrySetResult(result);
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                _logger.LogWarning("Speech recognition canceled for call {CallId}: {Reason}", 
                    callId, e.Reason);
                tcs.TrySetResult(new TranscriptionResult { CallId = callId });
            };

            _activeRecognizers[callId] = recognizer;
            await recognizer.StartContinuousRecognitionAsync();

            _logger.LogInformation("Started continuous recognition for call {CallId}", callId);
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting continuous recognition for call {CallId}", callId);
            return new TranscriptionResult { CallId = callId };
        }
    }

    public async Task StopContinuousRecognitionAsync(string callId)
    {
        if (_activeRecognizers.TryGetValue(callId, out var recognizer))
        {
            try
            {
                await recognizer.StopContinuousRecognitionAsync();
                recognizer.Dispose();
                _activeRecognizers.Remove(callId);
                _logger.LogInformation("Stopped continuous recognition for call {CallId}", callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping continuous recognition for call {CallId}", callId);
            }
        }
    }

    public async Task<WakeWordDetectionResult> DetectWakeWordAsync(AudioStreamData audioData)
    {
        var transcription = await TranscribeAudioAsync(audioData);
        var isDetected = await IsWakeWordDetectedAsync(transcription.Text, _wakeWord);

        return new WakeWordDetectionResult
        {
            WakeWordDetected = isDetected,
            Confidence = isDetected ? transcription.Confidence : 0.0,
            DetectedPhrase = isDetected ? transcription.Text : string.Empty,
            Timestamp = audioData.Timestamp
        };
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, string voice = "en-US-AriaNeural")
    {
        try
        {
            _speechConfig.SpeechSynthesisVoiceName = voice;
            using var synthesizer = new SpeechSynthesizer(_speechConfig);
            
            var result = await synthesizer.SpeakTextAsync(text);
            
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Synthesized speech for text: {Text}", text);
                return result.AudioData;
            }
            else
            {
                _logger.LogWarning("Speech synthesis failed: {Reason}", result.Reason);
                return Array.Empty<byte>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech for text: {Text}", text);
            return Array.Empty<byte>();
        }
    }

    public async Task<bool> IsWakeWordDetectedAsync(string transcribedText, string wakeWord)
    {
        if (string.IsNullOrWhiteSpace(transcribedText) || string.IsNullOrWhiteSpace(wakeWord))
            return false;

        return await Task.FromResult(
            transcribedText.Contains(wakeWord, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        foreach (var recognizer in _activeRecognizers.Values)
        {
            recognizer?.Dispose();
        }
        _activeRecognizers.Clear();
    }
}
