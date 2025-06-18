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
builder.Services.AddScoped<ITeamsMeetingService, TeamsMeetingService>();
builder.Services.AddScoped<IAudioProcessingService, AudioProcessingService>();

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

// Bot info endpoint
app.MapGet("/api/bot/info", () =>
{
    return Results.Ok(new
    {
        name = "AssistBot",
        version = "1.0.0",
        description = "AI-powered Teams meeting assistant",
        capabilities = new[]
        {
            "Speech-to-text transcription",
            "Wake word detection",
            "AI-powered responses",
            "Text-to-speech synthesis",
            "Meeting participation",
            "Real-time audio processing"
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
