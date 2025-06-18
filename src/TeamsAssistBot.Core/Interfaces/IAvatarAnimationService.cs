using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface IAvatarAnimationService
{
    Task<bool> InitializeAvatarAsync(string callId, AvatarConfiguration config);
    Task UpdateAvatarStateAsync(string callId, AvatarState newState);
    Task StartListeningAnimationAsync(string callId, AudioStreamData audioData);
    Task StartSpeakingAnimationAsync(string callId, byte[] audioData, TimeSpan duration);
    Task StartProcessingAnimationAsync(string callId);
    Task SetIdleAnimationAsync(string callId);
    Task<AnimationFrame> GetCurrentFrameAsync(string callId);
    Task<List<AnimationFrame>> GenerateLipSyncFramesAsync(byte[] audioData, TimeSpan duration);
    Task SendAnimationUpdateAsync(string callId, AnimationFrame frame);
    Task DisposeAvatarAsync(string callId);
    
    event EventHandler<AvatarAnimationEventArgs> AnimationStateChanged;
    event EventHandler<AnimationFrameEventArgs> FrameGenerated;
}
