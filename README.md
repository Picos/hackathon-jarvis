# Teams AssistBot - AI-Powered Microsoft Teams Meeting Assistant

A comprehensive C# ASP.NET Core application that provides an AI-powered assistant for Microsoft Teams meetings with real-time audio processing, speech-to-text transcription, wake word detection, and intelligent responses using Azure OpenAI.

## Features

- **Microsoft Teams Integration**: Seamlessly joins Teams meetings via Microsoft Graph Communications API
- **Real-time Audio Processing**: Captures and processes live audio streams from meetings
- **Speech-to-Text Transcription**: Uses Azure Cognitive Services for accurate transcription
- **Wake Word Detection**: Responds only when addressed with "Hey AssistBot"
- **AI-Powered Responses**: Leverages Azure OpenAI for intelligent, context-aware responses
- **Text-to-Speech**: Converts responses to natural-sounding speech
- **Audio Injection**: Can inject voice responses directly into the meeting (where supported)
- **Teams Chat Integration**: Sends responses via Teams chat messages
- **Comprehensive Logging**: Full logging and error handling for production use
- **Docker Support**: Ready for containerized deployment

## Architecture

### Project Structure

```
TeamsAssistBot/
├── src/
│   ├── TeamsAssistBot.Api/          # ASP.NET Core Web API
│   ├── TeamsAssistBot.Core/         # Business logic and domain models
│   ├── TeamsAssistBot.Infrastructure/ # External service integrations
│   └── TeamsAssistBot.Tests/        # Unit and integration tests
├── deployment/
│   ├── docker/                      # Docker configurations
│   └── azure/                       # Azure deployment scripts
└── config/
    ├── appsettings.json            # Local development config
    └── appsettings.Production.json # Production config
```

### Core Components

- **Bot Framework Handler**: Manages Teams interactions and events
- **Azure Speech Service**: Handles speech-to-text and text-to-speech
- **Azure OpenAI Service**: Provides intelligent responses
- **Teams Meeting Service**: Manages meeting participation and audio streaming
- **Audio Processing Service**: Processes and validates audio streams

## Prerequisites

- .NET 8 SDK
- Azure subscription with the following services:
  - Azure Bot Service
  - Azure Speech Services
  - Azure OpenAI Service
  - Azure Key Vault (optional)
  - Application Insights (optional)

## Setup and Configuration

### 1. Azure Bot Service Registration

1. Create a new Azure Bot Service resource
2. Register your bot with Microsoft Teams channel
3. Configure the messaging endpoint: `https://your-domain.com/api/messages`
4. Note down the Microsoft App ID and App Password

### 2. Microsoft Graph Application Registration

1. Register an application in Azure AD
2. Grant the following permissions:
   - `Calls.AccessMedia.All`
   - `Calls.Initiate.All`
   - `Calls.JoinGroupCall.All`
   - `OnlineMeetings.ReadWrite.All`
3. Configure authentication and certificates

### 3. Azure Services Configuration

Create the following Azure resources:
- **Speech Services**: For speech-to-text and text-to-speech
- **OpenAI Service**: For AI-powered responses
- **Key Vault**: For secure secret storage (optional)
- **Application Insights**: For monitoring and logging

### 4. Local Development Setup

1. Clone the repository:
```bash
git clone <repository-url>
cd TeamsAssistBot
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Configure user secrets:
```bash
cd src/TeamsAssistBot.Api
dotnet user-secrets init
dotnet user-secrets set "BotFramework:MicrosoftAppId" "your-app-id"
dotnet user-secrets set "BotFramework:MicrosoftAppPassword" "your-app-password"
dotnet user-secrets set "AzureServices:SpeechService:SubscriptionKey" "your-speech-key"
dotnet user-secrets set "AzureServices:SpeechService:Region" "your-speech-region"
dotnet user-secrets set "AzureServices:OpenAI:Endpoint" "your-openai-endpoint"
dotnet user-secrets set "AzureServices:OpenAI:ApiKey" "your-openai-key"
```

4. Update `appsettings.json` with your configuration

5. Run the application:
```bash
dotnet run --project src/TeamsAssistBot.Api
```

## Configuration

### Bot Framework Settings

```json
{
  "BotFramework": {
    "MicrosoftAppId": "your-microsoft-app-id",
    "MicrosoftAppPassword": "your-microsoft-app-password",
    "MicrosoftAppTenantId": "your-tenant-id",
    "BaseUrl": "https://your-bot-domain.com",
    "NotificationUrl": "https://your-bot-domain.com/api/calls"
  }
}
```

### Azure Services Settings

```json
{
  "AzureServices": {
    "SpeechService": {
      "SubscriptionKey": "your-speech-key",
      "Region": "eastus",
      "Language": "en-US",
      "WakeWord": "Hey Jarvis"
    },
    "OpenAI": {
      "Endpoint": "https://your-openai.openai.azure.com/",
      "ApiKey": "your-openai-key",
      "DeploymentName": "gpt-4",
      "MaxTokens": 1000,
      "Temperature": 0.7
    }
  }
}
```

## Deployment

### Docker Deployment

1. Build the Docker image:
```bash
docker build -f deployment/docker/Dockerfile -t teams-assist-bot .
```

2. Run the container:
```bash
docker run -p 8080:80 \
  -e BotFramework__MicrosoftAppId="your-app-id" \
  -e BotFramework__MicrosoftAppPassword="your-app-password" \
  teams-assist-bot
```

### Azure App Service Deployment

1. Create an Azure App Service
2. Configure environment variables or use Azure Key Vault
3. Deploy using Azure DevOps, GitHub Actions, or direct deployment

## Usage

### Adding the Bot to Teams

1. In Microsoft Teams, go to Apps
2. Search for your bot using the App ID
3. Add the bot to a team or meeting

### Interacting with the Bot

1. **Text Messages**: Send direct messages to the bot in Teams chat
2. **Meeting Participation**: The bot automatically joins when invited to meetings
3. **Voice Interaction**: Say "Hey Jarvis" followed by your question during meetings
4. **Meeting Assistant**: The bot can provide meeting summaries, answer questions, and assist with tasks

### Supported Commands

- `Hey Jarvis, what's the weather?`
- `Hey Jarvis, summarize this meeting`
- `Hey Jarvis, schedule a follow-up meeting`
- `Hey Jarvis, what are the action items?`

## API Endpoints

- `POST /api/messages` - Bot Framework messaging endpoint
- `GET /health` - Health check endpoint
- `GET /api/bot/info` - Bot information and capabilities

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Structure

- **Controllers**: Handle HTTP requests and Bot Framework messages
- **Services**: Business logic and external service integrations
- **Models**: Data transfer objects and domain models
- **Interfaces**: Abstraction layer for dependency injection

## Troubleshooting

### Common Issues

1. **Bot not responding**: Check App ID and Password configuration
2. **Audio not processing**: Verify Speech Services configuration and permissions
3. **AI responses not working**: Check OpenAI endpoint and API key
4. **Meeting join failures**: Verify Graph API permissions and certificates

### Logging

The application uses structured logging with the following levels:
- **Debug**: Detailed diagnostic information
- **Information**: General application flow
- **Warning**: Unexpected situations that don't stop the app
- **Error**: Error events that might still allow the app to continue

Check Application Insights or console logs for detailed error information.

## Security Considerations

- Store sensitive configuration in Azure Key Vault
- Use managed identities for Azure service authentication
- Implement proper authentication and authorization
- Ensure HTTPS is used for all endpoints
- Follow Microsoft's security best practices for bot development

## Performance

- Audio processing is optimized for real-time streaming
- AI responses are cached when appropriate
- Memory usage is managed through audio buffer limits
- Horizontal scaling is supported through stateless design

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:
1. Check the troubleshooting section
2. Review Azure service documentation
3. Check Microsoft Bot Framework documentation
4. Create an issue in the repository

## Acknowledgments

- Microsoft Bot Framework team
- Azure Cognitive Services team
- Microsoft Graph Communications API team
- Azure OpenAI Service team
