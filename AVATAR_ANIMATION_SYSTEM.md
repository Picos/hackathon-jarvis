# Avatar Animation System for Teams Assistant Bot

## Overview

The Teams Assistant Bot has been enhanced with a comprehensive avatar animation system that provides real-time visual feedback during bot interactions. The avatar responds to audio input with lifelike animations including blinking, lip-sync, and state-based expressions.

## Features

### 🎯 Real-time Audio Analysis
- **Listening Animations**: Avatar pulses and shows attention based on incoming audio intensity
- **Audio Responsiveness**: Dynamic visual feedback that responds to voice activity levels
- **Wake Word Detection**: Enhanced visual cues when the bot detects "Hey Jarvis"

### 💬 Advanced Lip-sync Technology
- **Phoneme Analysis**: Sophisticated audio processing to extract speech phonemes
- **Mouth Shape Mapping**: Realistic mouth movements corresponding to different sounds
- **Volume-based Animation**: Fallback animation system based on audio volume for robust performance
- **Real-time Synchronization**: Precise timing alignment between audio and visual animations

### 😊 Natural Facial Expressions
- **State-based Expressions**: Different facial expressions for idle, listening, speaking, and processing states
- **Emotion Levels**: Configurable happiness, attention, and concentration parameters
- **Context-aware Animations**: Expressions that match the bot's current activity

### 👁️ Intelligent Blinking System
- **Natural Patterns**: Configurable blinking frequencies (Minimal, Natural, Frequent)
- **Random Timing**: Human-like random intervals between blinks
- **State-aware Blinking**: Adjusted blinking behavior based on current avatar state

### ⚡ State Management
The avatar seamlessly transitions between four main states:

1. **Idle**: Subtle breathing animations with natural blinking
2. **Listening**: Attentive pulsing that responds to audio input intensity
3. **Speaking**: Lip-sync animations synchronized with speech synthesis
4. **Processing**: Thinking animations while AI generates responses

## Architecture

### Core Components

#### 1. IAvatarAnimationService
- Main service interface for avatar control
- Manages avatar sessions and state transitions
- Coordinates with audio processing pipeline
- Handles animation frame generation and timing

#### 2. ILipSyncService
- Audio analysis for phoneme extraction
- Volume envelope processing
- Lip-sync frame generation
- Mouth shape mapping for different sounds

#### 3. Animation Models
- **AvatarState**: Enum defining bot states (Idle, Listening, Speaking, Processing)
- **AnimationFrame**: Complete frame data including facial expressions, mouth position, and eye state
- **LipSyncData**: Phoneme and volume analysis results
- **AvatarConfiguration**: Customizable settings for animation behavior

### Integration Points

The avatar system integrates seamlessly with existing bot functionality:

- **Audio Pipeline**: Hooks into `OnAudioDataReceived` for listening animations
- **Speech Synthesis**: Coordinates with `SynthesizeSpeechAsync` for lip-sync
- **AI Processing**: Shows processing states during `ProcessTranscriptionAsync`
- **Meeting Events**: Initializes/disposes avatars during meeting start/end

## Configuration

### Avatar Settings (appsettings.json)

```json
{
  "AvatarSettings": {
    "EnableAnimations": true,
    "AnimationStyle": "Professional",
    "BlinkFrequency": "Natural",
    "LipSyncSensitivity": "Medium",
    "IdleAnimations": true,
    "FrameRate": 30,
    "AvatarImageUrl": "",
    "EnableDebugLogging": false
  }
}
```

### Configuration Options

- **EnableAnimations**: Master switch for all avatar animations
- **AnimationStyle**: Visual style preset (Professional, Casual, Expressive)
- **BlinkFrequency**: Controls blinking patterns (Minimal, Natural, Frequent)
- **LipSyncSensitivity**: Adjusts lip-sync responsiveness (Low, Medium, High)
- **IdleAnimations**: Enables/disables subtle idle movements
- **FrameRate**: Animation frame rate (default: 30 FPS)
- **AvatarImageUrl**: Custom avatar image (optional)
- **EnableDebugLogging**: Additional logging for animation events

## Technical Implementation

### Animation Pipeline

1. **Teams Call Join** → Avatar initialization and video streaming starts
2. **Audio Input** → Avatar listening animation triggered
3. **Wake Word Detection** → Enhanced attention state
4. **AI Processing** → Processing animation displayed
5. **Speech Synthesis** → Lip-sync generation begins
6. **Audio Output** → Synchronized lip-sync playback with video stream
7. **Completion** → Return to idle state
8. **Call End** → Avatar cleanup and video streaming stops

### Teams Integration

The avatar is actively displayed during Teams calls through:

- **Video Streaming**: 30 FPS avatar video feed sent to Teams participants
- **Real-time Rendering**: Animation frames converted to video frames
- **State Synchronization**: Avatar state matches bot activity in real-time
- **Participant Visibility**: All meeting participants can see the animated Jarvis avatar

### Performance Optimizations

- **Frame Buffering**: Smooth animation playback with minimal latency
- **Efficient Processing**: Optimized audio analysis algorithms
- **Resource Management**: Automatic cleanup of animation resources
- **Configurable Quality**: Adjustable frame rates and processing complexity

### Audio Analysis

The lip-sync system uses multiple approaches for robust performance:

1. **Primary**: Phoneme extraction from audio using frequency analysis
2. **Fallback**: Volume-based mouth movement generation
3. **Enhancement**: RMS volume calculation for intensity mapping
4. **Timing**: Precise frame-by-frame synchronization

## Development

### Adding New Animation States

```csharp
// 1. Add new state to enum
public enum AvatarState
{
    Idle,
    Listening,
    Speaking,
    Processing,
    CustomState // New state
}

// 2. Implement state generation
private async Task<AnimationFrame> GenerateCustomStateFrameAsync(string callId)
{
    return new AnimationFrame
    {
        CallId = callId,
        State = AvatarState.CustomState,
        // Define expression, mouth, and eye positions
    };
}

// 3. Add state handling in bot logic
await _avatarService.UpdateAvatarStateAsync(callId, AvatarState.CustomState);
```

### Custom Lip-sync Implementation

```csharp
public class CustomLipSyncService : ILipSyncService
{
    public async Task<LipSyncData> AnalyzeAudioForLipSyncAsync(byte[] audioData, TimeSpan duration)
    {
        // Custom audio analysis implementation
        // Return phoneme and volume data
    }
}
```

## Demo

A live demonstration of the avatar animation system is available at:
- **URL**: `/avatar-demo` (when bot is running)
- **Features**: Interactive controls to test all animation states
- **Real-time**: Live preview of avatar expressions and movements

## API Endpoints

### Bot Information
```
GET /api/bot/info
```
Returns comprehensive bot capabilities including avatar features.

### Health Check
```
GET /health
```
System health status including avatar service availability.

### Avatar Demo
```
GET /avatar-demo
```
Interactive demonstration page for testing avatar animations.

## Future Enhancements

### Planned Features
- **3D Avatar Rendering**: WebGL-based 3D avatar with advanced animations
- **Emotion Recognition**: Dynamic expressions based on conversation sentiment
- **Custom Avatar Upload**: Support for personalized avatar images
- **Advanced Lip-sync**: Machine learning-based phoneme prediction
- **Gesture Support**: Hand and body movement animations

### Integration Opportunities
- **Teams Native Rendering**: Direct integration with Teams client for avatar display
- **Real-time Streaming**: WebRTC-based avatar streaming to meeting participants
- **Multi-language Support**: Phoneme mapping for multiple languages
- **Accessibility Features**: Audio descriptions of avatar states for visually impaired users

## Troubleshooting

### Common Issues

1. **Avatar Not Animating**
   - Check `EnableAnimations` setting in configuration
   - Verify avatar service registration in DI container
   - Review logs for initialization errors

2. **Lip-sync Not Working**
   - Ensure audio data is properly formatted (16kHz, 16-bit, mono)
   - Check `LipSyncSensitivity` configuration
   - Verify phoneme analysis is running

3. **Performance Issues**
   - Reduce `FrameRate` setting
   - Disable `IdleAnimations` if not needed
   - Check system resources and CPU usage

### Debug Logging

Enable detailed logging by setting:
```json
{
  "AvatarSettings": {
    "EnableDebugLogging": true
  },
  "Logging": {
    "LogLevel": {
      "TeamsAssistBot.Infrastructure.Services.AvatarAnimationService": "Debug",
      "TeamsAssistBot.Infrastructure.Services.AudioAnalysisLipSyncService": "Debug"
    }
  }
}
```

## Contributing

To contribute to the avatar animation system:

1. **Fork** the repository
2. **Create** a feature branch for avatar enhancements
3. **Implement** your changes following the existing architecture
4. **Test** with the demo page to ensure animations work correctly
5. **Submit** a pull request with detailed description

## License

This avatar animation system is part of the Teams Assistant Bot project and follows the same licensing terms.
