using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Infrastructure.Services;

public class AudioAnalysisLipSyncService : ILipSyncService
{
    private readonly ILogger<AudioAnalysisLipSyncService> _logger;
    private readonly Dictionary<string, MouthShape> _phonemeToMouthShape;

    public AudioAnalysisLipSyncService(ILogger<AudioAnalysisLipSyncService> logger)
    {
        _logger = logger;
        _phonemeToMouthShape = InitializePhonemeMapping();
    }

    public async Task<LipSyncData> AnalyzeAudioForLipSyncAsync(byte[] audioData, TimeSpan duration)
    {
        try
        {
            _logger.LogDebug("Analyzing audio for lip-sync: {Duration}ms, {Bytes} bytes", 
                duration.TotalMilliseconds, audioData.Length);

            var volumeEnvelope = await ExtractVolumeEnvelopeAsync(audioData);
            var phonemes = await ExtractPhonemesAsync(audioData);
            var averageVolume = volumeEnvelope.Count > 0 ? volumeEnvelope.Average(v => v.Volume) : 0.0;

            return new LipSyncData
            {
                Phonemes = phonemes,
                TotalDuration = duration,
                AverageVolume = averageVolume,
                VolumeEnvelope = volumeEnvelope
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing audio for lip-sync");
            return new LipSyncData
            {
                TotalDuration = duration,
                VolumeEnvelope = await ExtractVolumeEnvelopeAsync(audioData)
            };
        }
    }

    public async Task<List<PhonemeData>> ExtractPhonemesAsync(byte[] audioData)
    {
        try
        {
            // Simplified phoneme extraction based on audio analysis
            // In a real implementation, this would use advanced audio processing libraries
            // or Azure Speech Services phoneme recognition
            
            var phonemes = new List<PhonemeData>();
            var frameSize = 1024; // samples per frame
            var sampleRate = 16000; // Hz
            var frameDurationMs = (frameSize * 1000.0) / sampleRate;
            
            for (int i = 0; i < audioData.Length - frameSize; i += frameSize)
            {
                var frameData = audioData.Skip(i).Take(frameSize).ToArray();
                var energy = CalculateFrameEnergy(frameData);
                var frequency = EstimateDominantFrequency(frameData, sampleRate);
                
                if (energy > 0.1) // Only process frames with significant energy
                {
                    var phoneme = EstimatePhoneme(frequency, energy);
                    var startTime = TimeSpan.FromMilliseconds(i * frameDurationMs / frameSize);
                    
                    phonemes.Add(new PhonemeData
                    {
                        Phoneme = phoneme,
                        StartTime = startTime,
                        Duration = TimeSpan.FromMilliseconds(frameDurationMs),
                        MouthShape = GetMouthShapeForPhoneme(phoneme),
                        Intensity = energy
                    });
                }
            }

            return await Task.FromResult(phonemes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting phonemes from audio");
            return new List<PhonemeData>();
        }
    }

    public async Task<List<VolumePoint>> ExtractVolumeEnvelopeAsync(byte[] audioData)
    {
        try
        {
            var volumePoints = new List<VolumePoint>();
            var frameSize = 512; // samples per frame for volume analysis
            var sampleRate = 16000; // Hz
            var frameDurationMs = (frameSize * 1000.0) / sampleRate;

            for (int i = 0; i < audioData.Length - frameSize; i += frameSize)
            {
                var frameData = audioData.Skip(i).Take(frameSize).ToArray();
                var volume = CalculateRMSVolume(frameData);
                var timestamp = TimeSpan.FromMilliseconds(i * frameDurationMs / frameSize);

                volumePoints.Add(new VolumePoint
                {
                    Timestamp = timestamp,
                    Volume = volume
                });
            }

            return await Task.FromResult(volumePoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting volume envelope from audio");
            return new List<VolumePoint>();
        }
    }

    public async Task<List<AnimationFrame>> GenerateLipSyncFramesAsync(LipSyncData lipSyncData, string callId)
    {
        try
        {
            var frames = new List<AnimationFrame>();
            var frameRate = TimeSpan.FromMilliseconds(33); // 30 FPS
            var totalFrames = (int)(lipSyncData.TotalDuration.TotalMilliseconds / frameRate.TotalMilliseconds);

            for (int i = 0; i < totalFrames; i++)
            {
                var currentTime = TimeSpan.FromMilliseconds(i * frameRate.TotalMilliseconds);
                
                // Find the active phoneme at this time
                var activePhoneme = lipSyncData.Phonemes
                    .FirstOrDefault(p => currentTime >= p.StartTime && 
                                   currentTime < p.StartTime.Add(p.Duration));

                // Find the volume at this time
                var volumePoint = lipSyncData.VolumeEnvelope
                    .OrderBy(v => Math.Abs((v.Timestamp - currentTime).TotalMilliseconds))
                    .FirstOrDefault();

                var volume = volumePoint?.Volume ?? 0.0;
                var intensity = CalculateIntensityFromVolume(volume);

                frames.Add(new AnimationFrame
                {
                    CallId = callId,
                    Timestamp = DateTime.UtcNow.Add(currentTime),
                    State = AvatarState.Speaking,
                    Duration = frameRate,
                    MouthPosition = GenerateMouthPosition(activePhoneme, intensity),
                    EyePosition = new EyePosition
                    {
                        LeftEyeOpen = true,
                        RightEyeOpen = true,
                        BlinkAmount = 0.0,
                        AttentionLevel = 0.8
                    },
                    Expression = new FacialExpression
                    {
                        Happiness = 0.6,
                        Attention = 0.8,
                        Concentration = 0.5 + (intensity * 0.3)
                    }
                });
            }

            return await Task.FromResult(frames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lip-sync frames");
            return new List<AnimationFrame>();
        }
    }

    public MouthShape GetMouthShapeForPhoneme(string phoneme)
    {
        if (string.IsNullOrEmpty(phoneme))
            return MouthShape.Neutral;

        return _phonemeToMouthShape.GetValueOrDefault(phoneme.ToUpper(), MouthShape.Neutral);
    }

    public double CalculateIntensityFromVolume(double volume)
    {
        // Normalize volume to intensity range [0, 1]
        return Math.Min(Math.Max(volume * 2.0, 0.0), 1.0);
    }

    private Dictionary<string, MouthShape> InitializePhonemeMapping()
    {
        return new Dictionary<string, MouthShape>
        {
            // Vowels
            { "A", MouthShape.A },
            { "AA", MouthShape.A },
            { "AE", MouthShape.A },
            { "E", MouthShape.E },
            { "EH", MouthShape.E },
            { "ER", MouthShape.E },
            { "I", MouthShape.I },
            { "IH", MouthShape.I },
            { "IY", MouthShape.I },
            { "O", MouthShape.O },
            { "OH", MouthShape.O },
            { "UH", MouthShape.U },
            { "UW", MouthShape.U },
            
            // Consonants
            { "M", MouthShape.M },
            { "P", MouthShape.M },
            { "B", MouthShape.M },
            { "F", MouthShape.F },
            { "V", MouthShape.F },
            { "L", MouthShape.L },
            { "R", MouthShape.L },
            { "S", MouthShape.S },
            { "Z", MouthShape.S },
            { "SH", MouthShape.S },
            { "ZH", MouthShape.S },
            
            // Default mappings for common sounds
            { "T", MouthShape.E },
            { "D", MouthShape.E },
            { "N", MouthShape.E },
            { "K", MouthShape.A },
            { "G", MouthShape.A },
            { "H", MouthShape.A },
            { "Y", MouthShape.I },
            { "W", MouthShape.U }
        };
    }

    private double CalculateFrameEnergy(byte[] frameData)
    {
        if (frameData.Length == 0) return 0.0;

        long sum = 0;
        for (int i = 0; i < frameData.Length - 1; i += 2)
        {
            short sample = (short)(frameData[i] | (frameData[i + 1] << 8));
            sum += Math.Abs(sample);
        }

        return (double)sum / (frameData.Length / 2) / 32768.0;
    }

    private double CalculateRMSVolume(byte[] frameData)
    {
        if (frameData.Length == 0) return 0.0;

        long sum = 0;
        for (int i = 0; i < frameData.Length - 1; i += 2)
        {
            short sample = (short)(frameData[i] | (frameData[i + 1] << 8));
            sum += sample * sample;
        }

        var rms = Math.Sqrt(sum / (frameData.Length / 2.0));
        return Math.Min(rms / 32768.0, 1.0);
    }

    private double EstimateDominantFrequency(byte[] frameData, int sampleRate)
    {
        // Simplified frequency estimation using zero-crossing rate
        // In a real implementation, you might use FFT or other spectral analysis
        
        int zeroCrossings = 0;
        for (int i = 2; i < frameData.Length - 3; i += 2)
        {
            short current = (short)(frameData[i] | (frameData[i + 1] << 8));
            short next = (short)(frameData[i + 2] | (frameData[i + 3] << 8));
            
            if ((current >= 0 && next < 0) || (current < 0 && next >= 0))
            {
                zeroCrossings++;
            }
        }

        // Estimate frequency based on zero crossings
        return (zeroCrossings * sampleRate) / (2.0 * frameData.Length / 2);
    }

    private string EstimatePhoneme(double frequency, double energy)
    {
        // Simplified phoneme estimation based on frequency and energy
        // This is a very basic approach - real implementations would use
        // machine learning models or advanced signal processing
        
        if (energy < 0.1) return "SILENCE";
        
        return frequency switch
        {
            < 300 => "U",    // Low frequency - likely 'oo' sound
            < 500 => "O",    // Low-mid frequency - likely 'oh' sound
            < 700 => "A",    // Mid frequency - likely 'ah' sound
            < 1200 => "E",   // Mid-high frequency - likely 'eh' sound
            < 2000 => "I",   // High frequency - likely 'ee' sound
            _ => "S"         // Very high frequency - likely sibilant
        };
    }

    private MouthPosition GenerateMouthPosition(PhonemeData? phoneme, double intensity)
    {
        if (phoneme == null)
        {
            return new MouthPosition
            {
                OpenAmount = intensity * 0.3,
                Width = 0.5,
                Shape = MouthShape.Neutral,
                Intensity = intensity
            };
        }

        var baseOpenAmount = GetBaseOpenAmountForShape(phoneme.MouthShape);
        var openAmount = baseOpenAmount + (intensity * 0.4);
        var width = GetBaseWidthForShape(phoneme.MouthShape) + (intensity * 0.2);

        return new MouthPosition
        {
            OpenAmount = Math.Min(openAmount, 1.0),
            Width = Math.Min(width, 1.0),
            Shape = phoneme.MouthShape,
            Intensity = intensity
        };
    }

    private double GetBaseOpenAmountForShape(MouthShape shape)
    {
        return shape switch
        {
            MouthShape.A => 0.8,
            MouthShape.E => 0.5,
            MouthShape.I => 0.3,
            MouthShape.O => 0.7,
            MouthShape.U => 0.4,
            MouthShape.M => 0.0,
            MouthShape.F => 0.2,
            MouthShape.L => 0.3,
            MouthShape.S => 0.2,
            _ => 0.1
        };
    }

    private double GetBaseWidthForShape(MouthShape shape)
    {
        return shape switch
        {
            MouthShape.A => 0.8,
            MouthShape.E => 0.6,
            MouthShape.I => 0.4,
            MouthShape.O => 0.5,
            MouthShape.U => 0.3,
            MouthShape.M => 0.5,
            MouthShape.F => 0.6,
            MouthShape.L => 0.5,
            MouthShape.S => 0.4,
            _ => 0.5
        };
    }
}
