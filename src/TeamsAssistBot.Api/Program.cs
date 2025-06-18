using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using TeamsAssistBot.Api.Bot;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Core.Models;
using TeamsAssistBot.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>();

// Add Azure Key Vault if configured
var keyVaultUrl = builder.Configuration["AzureServices:KeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Bot Framework
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddTransient<IBot, TeamsAssistBotHandler>();

// Add Core Services
builder.Services.AddScoped<IAIService, AzureOpenAIService>();
builder.Services.AddScoped<ISpeechService, AzureSpeechService>();
builder.Services.AddScoped<IAudioProcessingService, AudioProcessingService>();

// Add Avatar Animation Services
builder.Services.AddScoped<IAvatarAnimationService, AvatarAnimationService>();
builder.Services.AddScoped<ILipSyncService, AudioAnalysisLipSyncService>();

// Add Teams Meeting Service (after avatar service to ensure proper dependency injection)
builder.Services.AddScoped<ITeamsMeetingService, TeamsMeetingService>();

// Add Configuration Models
builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotFramework"));
builder.Services.Configure<AzureServicesConfiguration>(builder.Configuration.GetSection("AzureServices"));

// Add HTTP Client
builder.Services.AddHttpClient();

// Add Authentication
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
    });

builder.Services.AddAuthorization();

// Add Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsProduction())
{
    builder.Logging.AddApplicationInsights();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable serving static files from wwwroot
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Bot Framework endpoint
app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpContext context) =>
{
    await adapter.ProcessAsync(context.Request, context.Response, bot);
});

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});

// Avatar demo page
app.MapGet("/avatar-demo", () =>
{
    return Results.Redirect("/avatar-demo.html");
});

// Avatar status endpoint for Teams calls
app.MapGet("/api/avatar/status/{callId}", async (string callId, IAvatarAnimationService avatarService) =>
{
    try
    {
        var currentFrame = await avatarService.GetCurrentFrameAsync(callId);
        if (currentFrame != null)
        {
            return Results.Ok(new
            {
                callId = callId,
                isActive = true,
                currentState = currentFrame.State.ToString(),
                timestamp = currentFrame.Timestamp,
                expression = new
                {
                    happiness = currentFrame.Expression.Happiness,
                    attention = currentFrame.Expression.Attention,
                    concentration = currentFrame.Expression.Concentration
                },
                mouthPosition = new
                {
                    openAmount = currentFrame.MouthPosition.OpenAmount,
                    shape = currentFrame.MouthPosition.Shape.ToString(),
                    intensity = currentFrame.MouthPosition.Intensity
                },
                eyePosition = new
                {
                    leftEyeOpen = currentFrame.EyePosition.LeftEyeOpen,
                    rightEyeOpen = currentFrame.EyePosition.RightEyeOpen,
                    blinkAmount = currentFrame.EyePosition.BlinkAmount,
                    attentionLevel = currentFrame.EyePosition.AttentionLevel
                }
            });
        }
        else
        {
            return Results.Ok(new { callId = callId, isActive = false, message = "No avatar session found for this call" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error getting avatar status: {ex.Message}");
    }
});

// Bot info endpoint
app.MapGet("/api/bot/info", () =>
{
    return Results.Ok(new
    {
        name = "Jarvis",
        version = "2.0.0",
        description = "AI-powered Teams meeting assistant with animated avatar",
        capabilities = new[]
        {
            "Speech-to-text transcription",
            "Wake word detection",
            "AI-powered responses",
            "Text-to-speech synthesis",
            "Meeting participation",
            "Real-time audio processing",
            "Animated avatar with facial expressions",
            "Lip-sync during speaking",
            "Blinking and idle animations",
            "Audio-responsive listening animations",
            "State-based avatar expressions"
        }
    });
});

app.MapControllers();

app.Run();

// Custom adapter with error handling
public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
{
    public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger)
        : base(configuration, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "Exception caught in adapter: {Exception}", exception.Message);

            // Send a message to the user
            await turnContext.SendActivityAsync("The bot encountered an error or bug.");
            await turnContext.SendActivityAsync("Please try again or contact support if the problem persists.");

            if (turnContext.Activity.ChannelId == "msteams")
            {
                // Log the exception to Application Insights or other logging service
                logger.LogError(exception, "Teams bot error in conversation {ConversationId}", 
                    turnContext.Activity.Conversation?.Id);
            }
        };
    }
}
