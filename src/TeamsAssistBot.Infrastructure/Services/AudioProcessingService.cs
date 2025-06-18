using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Infrastructure.Services;

public class AudioProcessingService : IAudioProcessingService
{
    private readonly ILogger<AudioProcessingService> _logger;
    private readonly Dictionary<string, List<AudioStreamData>> _audioBuffer;

    public AudioProcessingService(IConfiguration configuration, ILogger<AudioProcessingService> logger)
    {
        _logger = logger;
        _audioBuffer = new Dictionary<string, List<AudioStreamData>>();
        
        _logger.LogInformation("Audio Processing Service initialized");
    }

    public async Task<AudioStreamData> ProcessRawAudioAsync(byte[] audioData, string callId, string participantId)
    {
        try
        {
            _logger.LogDebug("Processing raw audio data for call {CallId}, participant {ParticipantId}, size: {Size} bytes", 
                callId, participantId, audioData.Length);

            var processedAudio = new AudioStreamData
            {
                AudioData = audioData,
                Timestamp = DateTime.UtcNow,
                CallId = callId,
                ParticipantId = participantId,
                SampleRate = 16000, // Default to 16kHz for speech processing
                Channels = 1,       // Mono audio
                BitsPerSample = 16  // 16-bit PCM
            };

            // Perform basic audio processing
            processedAudio.AudioData = await NormalizeAudioAsync(audioData);
            processedAudio.AudioData = await ApplyNoiseReductionAsync(processedAudio.AudioData);

            return processedAudio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw audio data for call {CallId}", callId);
            throw;
        }
    }

    public async Task<bool> ValidateAudioFormatAsync(AudioStreamData audioData)
    {
        try
        {
            // Check if audio format is valid for speech processing
            if (audioData.SampleRate < 8000 || audioData.SampleRate > 48000)
            {
                _logger.LogWarning("Invalid sample rate: {SampleRate}. Should be between 8000-48000 Hz", 
                    audioData.SampleRate);
                return false;
            }

            if (audioData.Channels < 1 || audioData.Channels > 2)
            {
                _logger.LogWarning("Invalid channel count: {Channels}. Should be 1 or 2", 
                    audioData.Channels);
                return false;
            }

            if (audioData.BitsPerSample != 16 && audioData.BitsPerSample != 24 && audioData.BitsPerSample != 32)
            {
                _logger.LogWarning("Invalid bits per sample: {BitsPerSample}. Should be 16, 24, or 32", 
                    audioData.BitsPerSample);
                return false;
            }

            if (audioData.AudioData == null || audioData.AudioData.Length == 0)
            {
                _logger.LogWarning("Audio data is empty");
                return false;
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating audio format");
            return false;
        }
    }

    public async Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, int targetSampleRate, int targetChannels)
    {
        try
        {
            _logger.LogDebug("Converting audio format to {SampleRate}Hz, {Channels} channels", 
                targetSampleRate, targetChannels);

            // For this demo, we'll do a simple format conversion
            // In a production system, you'd use NAudio or similar for proper conversion
            
            using var inputStream = new MemoryStream(audioData);
            using var outputStream = new MemoryStream();

            // Simplified conversion - in reality you'd need proper audio format conversion
            // This is just a placeholder that copies the data
            await inputStream.CopyToAsync(outputStream);

            var convertedData = outputStream.ToArray();
            
            _logger.LogDebug("Audio format conversion completed. Input: {InputSize} bytes, Output: {OutputSize} bytes", 
                audioData.Length, convertedData.Length);

            return convertedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio format");
            throw;
        }
    }

    public async Task StoreAudioStreamAsync(AudioStreamData audioData)
    {
        try
        {
            if (!_audioBuffer.ContainsKey(audioData.CallId))
            {
                _audioBuffer[audioData.CallId] = new List<AudioStreamData>();
            }

            _audioBuffer[audioData.CallId].Add(audioData);

            // Keep only the last 100 audio chunks per call to manage memory
            if (_audioBuffer[audioData.CallId].Count > 100)
            {
                _audioBuffer[audioData.CallId].RemoveAt(0);
            }

            _logger.LogDebug("Stored audio stream for call {CallId}. Buffer size: {BufferSize}", 
                audioData.CallId, _audioBuffer[audioData.CallId].Count);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing audio stream for call {CallId}", audioData.CallId);
            throw;
        }
    }

    public async Task<AudioStreamData?> GetAudioStreamAsync(string callId, DateTime timestamp)
    {
        try
        {
            if (!_audioBuffer.ContainsKey(callId))
            {
                return null;
            }

            // Find the audio stream closest to the requested timestamp
            var audioStream = _audioBuffer[callId]
                .OrderBy(a => Math.Abs((a.Timestamp - timestamp).TotalMilliseconds))
                .FirstOrDefault();

            _logger.LogDebug("Retrieved audio stream for call {CallId} at timestamp {Timestamp}", 
                callId, timestamp);

            return await Task.FromResult(audioStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audio stream for call {CallId}", callId);
            return null;
        }
    }

    private async Task<byte[]> NormalizeAudioAsync(byte[] audioData)
    {
        try
        {
            // Simplified audio normalization
            // In a real implementation, you'd analyze the audio levels and normalize accordingly
            
            _logger.LogDebug("Normalizing audio data of {Size} bytes", audioData.Length);

            // For demo purposes, just return the original data
            // Real normalization would involve analyzing amplitude and adjusting levels
            return await Task.FromResult(audioData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing audio");
            return audioData;
        }
    }

    private async Task<byte[]> ApplyNoiseReductionAsync(byte[] audioData)
    {
        try
        {
            // Simplified noise reduction
            // In a real implementation, you'd apply filters to reduce background noise
            
            _logger.LogDebug("Applying noise reduction to audio data of {Size} bytes", audioData.Length);

            // For demo purposes, just return the original data
            // Real noise reduction would involve spectral analysis and filtering
            return await Task.FromResult(audioData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying noise reduction");
            return audioData;
        }
    }

    public void Dispose()
    {
        try
        {
            _audioBuffer.Clear();
            _logger.LogInformation("Audio Processing Service disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Audio Processing Service");
        }
    }
}
