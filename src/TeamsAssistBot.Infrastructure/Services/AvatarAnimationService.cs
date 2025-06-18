using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;
using System.Collections.Concurrent;

namespace TeamsAssistBot.Infrastructure.Services;

public class AvatarAnimationService : IAvatarAnimationService
{
    private readonly ILogger<AvatarAnimationService> _logger;
    private readonly ILipSyncService _lipSyncService;
    private readonly ITeamsMeetingService _meetingService;
    private readonly ConcurrentDictionary<string, AvatarSession> _activeSessions;
    private readonly Timer _animationTimer;

    public event EventHandler<AvatarAnimationEventArgs>? AnimationStateChanged;
    public event EventHandler<AnimationFrameEventArgs>? FrameGenerated;

    public AvatarAnimationService(
        ILogger<AvatarAnimationService> logger,
        ILipSyncService lipSyncService,
        ITeamsMeetingService meetingService)
    {
        _logger = logger;
        _lipSyncService = lipSyncService;
        _meetingService = meetingService;
        _activeSessions = new ConcurrentDictionary<string, AvatarSession>();
        
        // Start animation loop at 30 FPS
        _animationTimer = new Timer(UpdateAnimations, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(33));
    }

    public async Task<bool> InitializeAvatarAsync(string callId, AvatarConfiguration config)
    {
        try
        {
            var session = new AvatarSession
            {
                CallId = callId,
                Configuration = config,
                CurrentState = AvatarState.Idle,
                LastUpdate = DateTime.UtcNow,
                AnimationCancellation = new CancellationTokenSource()
            };

            // Initialize blink timing
            InitializeBlinkTiming(session.BlinkTiming, config.BlinkFrequency);

            _activeSessions[callId] = session;

            // Generate initial idle frame
            var initialFrame = await GenerateIdleFrameAsync(callId);
            session.LastFrame = initialFrame;

            await SendAnimationUpdateAsync(callId, initialFrame);

            _logger.LogInformation("Avatar initialized for call {CallId}", callId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing avatar for call {CallId}", callId);
            return false;
        }
    }

    public async Task UpdateAvatarStateAsync(string callId, AvatarState newState)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            _logger.LogWarning("No active avatar session found for call {CallId}", callId);
            return;
        }

        var previousState = session.CurrentState;
        session.CurrentState = newState;
        session.LastUpdate = DateTime.UtcNow;

        _logger.LogDebug("Avatar state changed for call {CallId}: {PreviousState} -> {NewState}", 
            callId, previousState, newState);

        // Trigger state change event
        AnimationStateChanged?.Invoke(this, new AvatarAnimationEventArgs
        {
            CallId = callId,
            PreviousState = previousState,
            NewState = newState,
            Timestamp = DateTime.UtcNow
        });

        // Generate appropriate animation based on new state
        await GenerateStateBasedAnimationAsync(callId, newState);
    }

    public async Task StartListeningAnimationAsync(string callId, AudioStreamData audioData)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            _logger.LogWarning("No active avatar session found for call {CallId}", callId);
            return;
        }

        await UpdateAvatarStateAsync(callId, AvatarState.Listening);

        // Generate listening animation with pulsing based on audio intensity
        var intensity = CalculateAudioIntensity(audioData.AudioData);
        var frame = await GenerateListeningFrameAsync(callId, intensity);
        
        await SendAnimationUpdateAsync(callId, frame);
    }

    public async Task StartSpeakingAnimationAsync(string callId, byte[] audioData, TimeSpan duration)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            _logger.LogWarning("No active avatar session found for call {CallId}", callId);
            return;
        }

        await UpdateAvatarStateAsync(callId, AvatarState.Speaking);

        try
        {
            // Generate lip-sync frames
            var lipSyncFrames = await GenerateLipSyncFramesAsync(audioData, duration);
            
            // Send frames with timing
            _ = Task.Run(async () =>
            {
                foreach (var frame in lipSyncFrames)
                {
                    if (session.AnimationCancellation?.Token.IsCancellationRequested == true)
                        break;

                    frame.CallId = callId;
                    await SendAnimationUpdateAsync(callId, frame);
                    
                    // Wait for frame duration
                    await Task.Delay(frame.Duration, session.AnimationCancellation?.Token ?? CancellationToken.None);
                }

                // Return to idle after speaking
                await SetIdleAnimationAsync(callId);
            }, session.AnimationCancellation?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting speaking animation for call {CallId}", callId);
            await SetIdleAnimationAsync(callId);
        }
    }

    public async Task StartProcessingAnimationAsync(string callId)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            _logger.LogWarning("No active avatar session found for call {CallId}", callId);
            return;
        }

        await UpdateAvatarStateAsync(callId, AvatarState.Processing);

        var frame = await GenerateProcessingFrameAsync(callId);
        await SendAnimationUpdateAsync(callId, frame);
    }

    public async Task SetIdleAnimationAsync(string callId)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            _logger.LogWarning("No active avatar session found for call {CallId}", callId);
            return;
        }

        await UpdateAvatarStateAsync(callId, AvatarState.Idle);

        var frame = await GenerateIdleFrameAsync(callId);
        await SendAnimationUpdateAsync(callId, frame);
    }

    public async Task<AnimationFrame> GetCurrentFrameAsync(string callId)
    {
        if (_activeSessions.TryGetValue(callId, out var session) && session.LastFrame != null)
        {
            return session.LastFrame;
        }

        return await GenerateIdleFrameAsync(callId);
    }

    public async Task<List<AnimationFrame>> GenerateLipSyncFramesAsync(byte[] audioData, TimeSpan duration)
    {
        try
        {
            var lipSyncData = await _lipSyncService.AnalyzeAudioForLipSyncAsync(audioData, duration);
            return await _lipSyncService.GenerateLipSyncFramesAsync(lipSyncData, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lip-sync frames");
            
            // Fallback: generate simple volume-based animation
            return GenerateVolumeBasedFrames(audioData, duration);
        }
    }

    public async Task SendAnimationUpdateAsync(string callId, AnimationFrame frame)
    {
        try
        {
            if (_activeSessions.TryGetValue(callId, out var session))
            {
                session.LastFrame = frame;
            }

            // Trigger frame generated event
            FrameGenerated?.Invoke(this, new AnimationFrameEventArgs
            {
                CallId = callId,
                Frame = frame,
                Timestamp = DateTime.UtcNow
            });

            // Send to Teams client (this would be implemented based on Teams SDK capabilities)
            await SendFrameToTeamsClientAsync(callId, frame);

            _logger.LogDebug("Animation frame sent for call {CallId}, State: {State}", 
                callId, frame.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending animation update for call {CallId}", callId);
        }
    }

    public async Task DisposeAvatarAsync(string callId)
    {
        if (_activeSessions.TryRemove(callId, out var session))
        {
            session.AnimationCancellation?.Cancel();
            session.AnimationCancellation?.Dispose();
            
            _logger.LogInformation("Avatar disposed for call {CallId}", callId);
        }

        await Task.CompletedTask;
    }

    private async void UpdateAnimations(object? state)
    {
        var tasks = new List<Task>();

        foreach (var session in _activeSessions.Values)
        {
            if (!session.IsActive || session.AnimationCancellation?.Token.IsCancellationRequested == true)
                continue;

            tasks.Add(UpdateSessionAnimationAsync(session));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task UpdateSessionAnimationAsync(AvatarSession session)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // Check if we need to blink
            if (ShouldBlink(session.BlinkTiming, now))
            {
                var blinkFrame = await GenerateBlinkFrameAsync(session.CallId);
                await SendAnimationUpdateAsync(session.CallId, blinkFrame);
                
                UpdateBlinkTiming(session.BlinkTiming, now);
            }

            // Update idle animations
            if (session.CurrentState == AvatarState.Idle && session.Configuration.IdleAnimations)
            {
                var idleFrame = await GenerateIdleFrameAsync(session.CallId);
                await SendAnimationUpdateAsync(session.CallId, idleFrame);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating animation for session {CallId}", session.CallId);
        }
    }

    private async Task GenerateStateBasedAnimationAsync(string callId, AvatarState state)
    {
        AnimationFrame frame = state switch
        {
            AvatarState.Idle => await GenerateIdleFrameAsync(callId),
            AvatarState.Processing => await GenerateProcessingFrameAsync(callId),
            AvatarState.Listening => await GenerateListeningFrameAsync(callId, 0.3),
            _ => await GenerateIdleFrameAsync(callId)
        };

        await SendAnimationUpdateAsync(callId, frame);
    }

    private async Task<AnimationFrame> GenerateIdleFrameAsync(string callId)
    {
        return await Task.FromResult(new AnimationFrame
        {
            CallId = callId,
            Timestamp = DateTime.UtcNow,
            State = AvatarState.Idle,
            Expression = new FacialExpression
            {
                Happiness = 0.6,
                Attention = 0.5,
                Concentration = 0.3
            },
            MouthPosition = new MouthPosition
            {
                OpenAmount = 0.0,
                Width = 0.5,
                Shape = MouthShape.Neutral
            },
            EyePosition = new EyePosition
            {
                LeftEyeOpen = true,
                RightEyeOpen = true,
                BlinkAmount = 0.0,
                AttentionLevel = 0.5
            }
        });
    }

    private async Task<AnimationFrame> GenerateListeningFrameAsync(string callId, double intensity)
    {
        return await Task.FromResult(new AnimationFrame
        {
            CallId = callId,
            Timestamp = DateTime.UtcNow,
            State = AvatarState.Listening,
            Expression = new FacialExpression
            {
                Happiness = 0.5,
                Attention = 0.8 + (intensity * 0.2),
                Concentration = 0.7
            },
            MouthPosition = new MouthPosition
            {
                OpenAmount = 0.0,
                Width = 0.5,
                Shape = MouthShape.Neutral
            },
            EyePosition = new EyePosition
            {
                LeftEyeOpen = true,
                RightEyeOpen = true,
                BlinkAmount = 0.0,
                AttentionLevel = 0.8 + (intensity * 0.2)
            }
        });
    }

    private async Task<AnimationFrame> GenerateProcessingFrameAsync(string callId)
    {
        return await Task.FromResult(new AnimationFrame
        {
            CallId = callId,
            Timestamp = DateTime.UtcNow,
            State = AvatarState.Processing,
            Expression = new FacialExpression
            {
                Happiness = 0.4,
                Attention = 0.6,
                Concentration = 0.9
            },
            MouthPosition = new MouthPosition
            {
                OpenAmount = 0.0,
                Width = 0.4,
                Shape = MouthShape.Neutral
            },
            EyePosition = new EyePosition
            {
                LeftEyeOpen = true,
                RightEyeOpen = true,
                BlinkAmount = 0.0,
                AttentionLevel = 0.7
            }
        });
    }

    private async Task<AnimationFrame> GenerateBlinkFrameAsync(string callId)
    {
        if (!_activeSessions.TryGetValue(callId, out var session))
        {
            return await GenerateIdleFrameAsync(callId);
        }

        var baseFrame = session.LastFrame ?? await GenerateIdleFrameAsync(callId);
        
        return new AnimationFrame
        {
            CallId = callId,
            Timestamp = DateTime.UtcNow,
            State = baseFrame.State,
            Expression = baseFrame.Expression,
            MouthPosition = baseFrame.MouthPosition,
            EyePosition = new EyePosition
            {
                LeftEyeOpen = false,
                RightEyeOpen = false,
                BlinkAmount = 1.0,
                AttentionLevel = baseFrame.EyePosition.AttentionLevel
            },
            Duration = session.BlinkTiming.BlinkDuration
        };
    }

    private void InitializeBlinkTiming(BlinkTimingData blinkTiming, BlinkPattern pattern)
    {
        blinkTiming.Pattern = pattern;
        blinkTiming.NextBlinkTime = CalculateNextBlinkTime(pattern, blinkTiming.RandomGenerator);
    }

    private bool ShouldBlink(BlinkTimingData blinkTiming, DateTime now)
    {
        return now >= DateTime.UtcNow.Add(blinkTiming.NextBlinkTime);
    }

    private void UpdateBlinkTiming(BlinkTimingData blinkTiming, DateTime now)
    {
        blinkTiming.NextBlinkTime = CalculateNextBlinkTime(blinkTiming.Pattern, blinkTiming.RandomGenerator);
    }

    private TimeSpan CalculateNextBlinkTime(BlinkPattern pattern, Random random)
    {
        return pattern switch
        {
            BlinkPattern.Minimal => TimeSpan.FromSeconds(8 + random.NextDouble() * 7), // 8-15 seconds
            BlinkPattern.Natural => TimeSpan.FromSeconds(3 + random.NextDouble() * 4), // 3-7 seconds
            BlinkPattern.Frequent => TimeSpan.FromSeconds(1 + random.NextDouble() * 2), // 1-3 seconds
            _ => TimeSpan.FromSeconds(5)
        };
    }

    private double CalculateAudioIntensity(byte[] audioData)
    {
        if (audioData.Length == 0) return 0.0;

        // Simple RMS calculation for audio intensity
        long sum = 0;
        for (int i = 0; i < audioData.Length - 1; i += 2)
        {
            short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
            sum += sample * sample;
        }

        var rms = Math.Sqrt(sum / (audioData.Length / 2.0));
        return Math.Min(rms / 32768.0, 1.0); // Normalize to 0-1
    }

    private List<AnimationFrame> GenerateVolumeBasedFrames(byte[] audioData, TimeSpan duration)
    {
        var frames = new List<AnimationFrame>();
        var frameCount = (int)(duration.TotalMilliseconds / 33); // 30 FPS
        var samplesPerFrame = audioData.Length / frameCount;

        for (int i = 0; i < frameCount; i++)
        {
            var frameStart = i * samplesPerFrame;
            var frameEnd = Math.Min(frameStart + samplesPerFrame, audioData.Length);
            
            var frameData = audioData.Skip(frameStart).Take(frameEnd - frameStart).ToArray();
            var intensity = CalculateAudioIntensity(frameData);

            frames.Add(new AnimationFrame
            {
                Timestamp = DateTime.UtcNow.AddMilliseconds(i * 33),
                State = AvatarState.Speaking,
                MouthPosition = new MouthPosition
                {
                    OpenAmount = intensity * 0.7,
                    Width = 0.5 + (intensity * 0.3),
                    Shape = intensity > 0.3 ? MouthShape.A : MouthShape.Neutral,
                    Intensity = intensity
                },
                EyePosition = new EyePosition
                {
                    LeftEyeOpen = true,
                    RightEyeOpen = true,
                    AttentionLevel = 0.8
                },
                Expression = new FacialExpression
                {
                    Happiness = 0.6,
                    Attention = 0.8,
                    Concentration = 0.5
                }
            });
        }

        return frames;
    }

    private async Task SendFrameToTeamsClientAsync(string callId, AnimationFrame frame)
    {
        // This would integrate with Teams SDK to send animation data
        // For now, we'll log the frame data
        _logger.LogDebug("Sending animation frame for call {CallId}: State={State}, MouthOpen={MouthOpen}, Blink={Blink}",
            callId, frame.State, frame.MouthPosition.OpenAmount, frame.EyePosition.BlinkAmount);

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _animationTimer?.Dispose();
        
        foreach (var session in _activeSessions.Values)
        {
            session.AnimationCancellation?.Cancel();
            session.AnimationCancellation?.Dispose();
        }
        
        _activeSessions.Clear();
    }
}
