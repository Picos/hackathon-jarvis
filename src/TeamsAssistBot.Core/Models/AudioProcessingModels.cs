namespace TeamsAssistBot.Core.Models;

public class AudioStreamData
{
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
    public string CallId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public int BitsPerSample { get; set; } = 16;
}

public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public string CallId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public bool ContainsWakeWord { get; set; }
}

public class AIResponse
{
    public string ResponseText { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ResponseType Type { get; set; } = ResponseType.Text;
    public byte[]? AudioResponse { get; set; }
    public string? AudioFormat { get; set; }
}

public enum ResponseType
{
    Text,
    Audio,
    Both
}

public class WakeWordDetectionResult
{
    public bool WakeWordDetected { get; set; }
    public double Confidence { get; set; }
    public string DetectedPhrase { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
