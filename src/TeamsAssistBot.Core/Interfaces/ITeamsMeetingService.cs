using TeamsAssistBot.Core.Models;

namespace TeamsAssistBot.Core.Interfaces;

public interface ITeamsMeetingService
{
    Task<string> JoinMeetingAsync(string meetingUrl, string displayName);
    Task LeaveMeetingAsync(string callId);
    Task<bool> IsMeetingActiveAsync(string callId);
    Task StartMediaStreamingAsync(string callId);
    Task StopMediaStreamingAsync(string callId);
    Task SendChatMessageAsync(string callId, string message);
    Task<bool> InjectAudioAsync(string callId, byte[] audioData);
    Task HandleCallStateChangeAsync(string callId, string newState);
    Task<List<string>> GetMeetingParticipantsAsync(string callId);
    event EventHandler<AudioStreamData> AudioDataReceived;
    event EventHandler<string> CallStateChanged;
    event EventHandler<string> ParticipantJoined;
    event EventHandler<string> ParticipantLeft;
}
