using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Infrastructure.Services;

public class TeamsMeetingService : ITeamsMeetingService
{
    private readonly ILogger<TeamsMeetingService> _logger;
    private readonly IAvatarAnimationService? _avatarService;
    private readonly Dictionary<string, bool> _activeCalls;
    private readonly Dictionary<string, bool> _avatarStreamingActive;

    public event EventHandler<AudioStreamData>? AudioDataReceived;
    public event EventHandler<string>? CallStateChanged;
    public event EventHandler<string>? ParticipantJoined;
    public event EventHandler<string>? ParticipantLeft;

    public TeamsMeetingService(
        IConfiguration configuration, 
        ILogger<TeamsMeetingService> logger,
        IAvatarAnimationService? avatarService = null)
    {
        _logger = logger;
        _avatarService = avatarService;
        _activeCalls = new Dictionary<string, bool>();
        _avatarStreamingActive = new Dictionary<string, bool>();
        
        _logger.LogInformation("Teams Meeting Service initialized with avatar support: {AvatarEnabled}", 
            _avatarService != null);
    }

    public async Task<string> JoinMeetingAsync(string meetingUrl, string displayName)
    {
        try
        {
            _logger.LogInformation("Attempting to join meeting: {MeetingUrl} as {DisplayName}", meetingUrl, displayName);

            // Generate a call ID for this meeting
            var callId = Guid.NewGuid().ToString();
            _activeCalls[callId] = true;

            _logger.LogInformation("Successfully joined meeting. Call ID: {CallId}", callId);
            
            // Simulate call state change
            CallStateChanged?.Invoke(this, callId);
            
            return callId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining meeting: {MeetingUrl}", meetingUrl);
            throw;
        }
    }

    public async Task LeaveMeetingAsync(string callId)
    {
        try
        {
            if (_activeCalls.ContainsKey(callId))
            {
                _activeCalls.Remove(callId);
                _logger.LogInformation("Left meeting. Call ID: {CallId}", callId);
                
                // Simulate call state change
                CallStateChanged?.Invoke(this, callId);
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving meeting. Call ID: {CallId}", callId);
            throw;
        }
    }

    public async Task<bool> IsMeetingActiveAsync(string callId)
    {
        try
        {
            return await Task.FromResult(_activeCalls.ContainsKey(callId) && _activeCalls[callId]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking meeting status. Call ID: {CallId}", callId);
            return false;
        }
    }

    public async Task StartMediaStreamingAsync(string callId)
    {
        try
        {
            if (_activeCalls.ContainsKey(callId))
            {
                _logger.LogInformation("Started media streaming for call: {CallId}", callId);
                
                // Start avatar video streaming if avatar service is available
                if (_avatarService != null)
                {
                    await StartAvatarVideoStreamAsync(callId);
                }
                
                // Start a background task to simulate audio data
                _ = Task.Run(async () => await SimulateAudioStreamAsync(callId));
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting media streaming. Call ID: {CallId}", callId);
            throw;
        }
    }

    public async Task StopMediaStreamingAsync(string callId)
    {
        try
        {
            if (_activeCalls.ContainsKey(callId))
            {
                _activeCalls[callId] = false;
                _logger.LogInformation("Stopped media streaming for call: {CallId}", callId);
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping media streaming. Call ID: {CallId}", callId);
        }
    }

    public async Task SendChatMessageAsync(string callId, string message)
    {
        try
        {
            _logger.LogInformation("Sending chat message for call {CallId}: {Message}", callId, message);
            
            // In a real implementation, this would send a Teams chat message
            // For now, we'll just log it
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message. Call ID: {CallId}", callId);
        }
    }

    public async Task<bool> InjectAudioAsync(string callId, byte[] audioData)
    {
        try
        {
            if (_activeCalls.ContainsKey(callId) && _activeCalls[callId])
            {
                _logger.LogInformation("Audio injection simulated for call: {CallId} ({Length} bytes)", 
                    callId, audioData.Length);
                return await Task.FromResult(true);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting audio. Call ID: {CallId}", callId);
            return false;
        }
    }

    public async Task HandleCallStateChangeAsync(string callId, string newState)
    {
        try
        {
            _logger.LogInformation("Call state changed. Call ID: {CallId}, New State: {NewState}", callId, newState);
            CallStateChanged?.Invoke(this, callId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling call state change. Call ID: {CallId}", callId);
        }
    }

    public async Task<List<string>> GetMeetingParticipantsAsync(string callId)
    {
        try
        {
            if (_activeCalls.ContainsKey(callId))
            {
                // Return some dummy participants for demo
                return await Task.FromResult(new List<string> 
                { 
                    "User 1", 
                    "User 2", 
                    "Jarvis" 
                });
            }
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meeting participants. Call ID: {CallId}", callId);
            return new List<string>();
        }
    }

    private async Task SimulateAudioStreamAsync(string callId)
    {
        try
        {
            while (_activeCalls.ContainsKey(callId) && _activeCalls[callId])
            {
                // Simulate receiving audio data every 100ms
                await Task.Delay(100);

                if (_activeCalls.ContainsKey(callId) && _activeCalls[callId])
                {
                    // Create dummy audio data (in a real implementation, this would be actual audio)
                    var audioData = new AudioStreamData
                    {
                        AudioData = new byte[1600], // 100ms of 16kHz audio
                        Timestamp = DateTime.UtcNow,
                        CallId = callId,
                        ParticipantId = "participant-1",
                        SampleRate = 16000,
                        Channels = 1,
                        BitsPerSample = 16
                    };

                    // Occasionally simulate audio with content that might contain wake word
                    if (Random.Shared.Next(1, 100) <= 5) // 5% chance
                    {
                        _logger.LogDebug("Simulating audio data with potential speech for call: {CallId}", callId);
                        AudioDataReceived?.Invoke(this, audioData);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio stream simulation for call: {CallId}", callId);
        }
    }

    private async Task StartAvatarVideoStreamAsync(string callId)
    {
        try
        {
            if (_avatarService == null)
                return;

            _logger.LogInformation("Starting avatar video stream for call: {CallId}", callId);
            
            // Mark avatar streaming as active
            _avatarStreamingActive[callId] = true;
            
            // Start avatar video streaming loop
            _ = Task.Run(async () => await StreamAvatarVideoAsync(callId));
            
            _logger.LogInformation("Avatar video streaming started for call: {CallId}", callId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting avatar video stream for call: {CallId}", callId);
        }
    }

    private async Task StopAvatarVideoStreamAsync(string callId)
    {
        try
        {
            if (_avatarStreamingActive.ContainsKey(callId))
            {
                _avatarStreamingActive[callId] = false;
                _avatarStreamingActive.Remove(callId);
                _logger.LogInformation("Stopped avatar video streaming for call: {CallId}", callId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping avatar video stream for call: {CallId}", callId);
        }
    }

    private async Task StreamAvatarVideoAsync(string callId)
    {
        try
        {
            if (_avatarService == null)
                return;

            while (_activeCalls.ContainsKey(callId) && 
                   _activeCalls[callId] && 
                   _avatarStreamingActive.GetValueOrDefault(callId, false))
            {
                // Get current avatar frame
                var currentFrame = await _avatarService.GetCurrentFrameAsync(callId);
                
                if (currentFrame != null)
                {
                    // Convert avatar frame to video frame and send to Teams
                    await SendAvatarFrameToTeamsAsync(callId, currentFrame);
                }

                // Wait for next frame (30 FPS = ~33ms)
                await Task.Delay(33);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in avatar video streaming for call: {CallId}", callId);
        }
        finally
        {
            await StopAvatarVideoStreamAsync(callId);
        }
    }

    private async Task SendAvatarFrameToTeamsAsync(string callId, AnimationFrame frame)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Render the animation frame to a video frame
            // 2. Encode the frame for Teams video streaming
            // 3. Send the frame via Teams Graph API or Bot Framework
            
            // For simulation, we log the avatar state and key animation properties
            _logger.LogDebug("Streaming avatar frame for call {CallId}: State={State}, MouthOpen={MouthOpen}, Blink={Blink}", 
                callId, frame.State, frame.MouthPosition.OpenAmount, frame.EyePosition.BlinkAmount);

            // Simulate avatar visibility in Teams by creating virtual video feed
            await SimulateTeamsAvatarDisplayAsync(callId, frame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending avatar frame to Teams for call: {CallId}", callId);
        }
    }

    private async Task SimulateTeamsAvatarDisplayAsync(string callId, AnimationFrame frame)
    {
        // Simulate Teams avatar display by logging detailed animation state
        // In a real implementation, this would render the avatar and stream video
        
        var avatarStatus = $"Jarvis Avatar - {frame.State}";
        
        switch (frame.State)
        {
            case AvatarState.Idle:
                avatarStatus += " (Natural blinking, ready to assist)";
                break;
            case AvatarState.Listening:
                avatarStatus += $" (Attentive, focus level: {frame.Expression.Attention:F1})";
                break;
            case AvatarState.Speaking:
                avatarStatus += $" (Speaking, mouth: {frame.MouthPosition.Shape}, intensity: {frame.MouthPosition.Intensity:F1})";
                break;
            case AvatarState.Processing:
                avatarStatus += $" (Thinking, concentration: {frame.Expression.Concentration:F1})";
                break;
        }

        // Log avatar display every 1 second to avoid spam
        if (DateTime.UtcNow.Second % 1 == 0 && DateTime.UtcNow.Millisecond < 50)
        {
            _logger.LogInformation("Teams Avatar Display - Call {CallId}: {Status}", callId, avatarStatus);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var callId in _activeCalls.Keys.ToList())
        {
            try
            {
                // Stop avatar streaming
                if (_avatarStreamingActive.ContainsKey(callId))
                {
                    StopAvatarVideoStreamAsync(callId).Wait();
                }
                
                LeaveMeetingAsync(callId).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing call: {CallId}", callId);
            }
        }
        _activeCalls.Clear();
        _avatarStreamingActive.Clear();
    }
}
