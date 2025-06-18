namespace TeamsAssistBot.Core.Models;

public enum AvatarState
{
    Idle,
    Listening,
    Speaking,
    Processing,
    Inactive
}

public class AvatarConfiguration
{
    public bool EnableAnimations { get; set; } = true;
    public string AnimationStyle { get; set; } = "Professional";
    public BlinkPattern BlinkFrequency { get; set; } = BlinkPattern.Natural;
    public LipSyncSensitivity LipSyncSensitivity { get; set; } = LipSyncSensitivity.Medium;
    public bool IdleAnimations { get; set; } = true;
    public string AvatarImageUrl { get; set; } = string.Empty;
    public TimeSpan FrameRate { get; set; } = TimeSpan.FromMilliseconds(33); // ~30 FPS
}

public enum BlinkPattern
{
    Minimal,
    Natural,
    Frequent
}

public enum LipSyncSensitivity
{
    Low,
    Medium,
    High
}

public class AnimationFrame
{
    public string CallId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public AvatarState State { get; set; }
    public FacialExpression Expression { get; set; } = new();
    public MouthPosition MouthPosition { get; set; } = new();
    public EyePosition EyePosition { get; set; } = new();
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(33);
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class FacialExpression
{
    public double Happiness { get; set; } = 0.5;
    public double Attention { get; set; } = 0.5;
    public double Concentration { get; set; } = 0.5;
}

public class MouthPosition
{
    public double OpenAmount { get; set; } = 0.0; // 0.0 = closed, 1.0 = fully open
    public double Width { get; set; } = 0.5; // mouth width
    public MouthShape Shape { get; set; } = MouthShape.Neutral;
    public double Intensity { get; set; } = 0.0; // speaking intensity
}

public enum MouthShape
{
    Neutral,
    A, // "ah" sound
    E, // "eh" sound
    I, // "ee" sound
    O, // "oh" sound
    U, // "oo" sound
    M, // "mm" sound (lips closed)
    F, // "ff" sound
    L, // "ll" sound
    S  // "ss" sound
}

public class EyePosition
{
    public bool LeftEyeOpen { get; set; } = true;
    public bool RightEyeOpen { get; set; } = true;
    public double BlinkAmount { get; set; } = 0.0; // 0.0 = open, 1.0 = closed
    public double AttentionLevel { get; set; } = 0.5; // eye focus/attention
}

public class LipSyncData
{
    public List<PhonemeData> Phonemes { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public double AverageVolume { get; set; }
    public List<VolumePoint> VolumeEnvelope { get; set; } = new();
}

public class PhonemeData
{
    public string Phoneme { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public MouthShape MouthShape { get; set; }
    public double Intensity { get; set; }
}

public class VolumePoint
{
    public TimeSpan Timestamp { get; set; }
    public double Volume { get; set; }
}

public class AvatarAnimationEventArgs : EventArgs
{
    public string CallId { get; set; } = string.Empty;
    public AvatarState PreviousState { get; set; }
    public AvatarState NewState { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AnimationFrameEventArgs : EventArgs
{
    public string CallId { get; set; } = string.Empty;
    public AnimationFrame Frame { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class BlinkTimingData
{
    public TimeSpan NextBlinkTime { get; set; }
    public TimeSpan BlinkDuration { get; set; } = TimeSpan.FromMilliseconds(150);
    public BlinkPattern Pattern { get; set; }
    public Random RandomGenerator { get; set; } = new();
}

public class AvatarSession
{
    public string CallId { get; set; } = string.Empty;
    public AvatarConfiguration Configuration { get; set; } = new();
    public AvatarState CurrentState { get; set; } = AvatarState.Idle;
    public AnimationFrame? LastFrame { get; set; }
    public BlinkTimingData BlinkTiming { get; set; } = new();
    public DateTime LastUpdate { get; set; }
    public bool IsActive { get; set; } = true;
    public CancellationTokenSource? AnimationCancellation { get; set; }
}
