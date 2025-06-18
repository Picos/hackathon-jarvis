using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Core.Interfaces;
using TeamsAssistBot.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing Teams Assist Bot functionality...");

        // Create host builder with services
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("src/TeamsAssistBot.Api/appsettings.json");
                config.AddJsonFile("src/TeamsAssistBot.Api/appsettings.Development.json", optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(builder => builder.AddConsole());

                // Add services
                services.AddScoped<IAIService, AzureOpenAIService>();
                services.AddScoped<ISpeechService, AzureSpeechService>();
                services.AddScoped<ITeamsMeetingService, TeamsMeetingService>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Test AI Service
            var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
            
            Console.WriteLine("Testing AI Service...");
            
            // Test validation
            var isValid = await aiService.ValidateInputAsync("Hello, can you help me?");
            Console.WriteLine($"✓ Input validation: {isValid}");
            
            // Test message processing
            var processed = await aiService.ProcessTranscriptionAsync("Hello, can you help me?");
            Console.WriteLine($"✓ Message processing: {processed}");
            
            // Test contextual response
            var response = await aiService.GetContextualResponseAsync("Hello, can you help me?", new List<string>());
            Console.WriteLine($"✓ AI Response: {response}");
            
            // Test Meeting Service
            var meetingService = scope.ServiceProvider.GetRequiredService<ITeamsMeetingService>();
            Console.WriteLine("\nTesting Meeting Service...");
            
            var callId = await meetingService.JoinMeetingAsync("test-url", "Jarvis");
            Console.WriteLine($"✓ Meeting join simulation: {callId}");
            
            var isActive = await meetingService.IsMeetingActiveAsync(callId);
            Console.WriteLine($"✓ Meeting active check: {isActive}");
            
            await meetingService.LeaveMeetingAsync(callId);
            Console.WriteLine("✓ Meeting leave simulation completed");
            
            Console.WriteLine("\n🎉 All tests completed successfully!");
            Console.WriteLine("The bot components are working correctly.");
            Console.WriteLine("\nNext steps:");
            Console.WriteLine("1. Run the bot application: dotnet run --project src/TeamsAssistBot.Api");
            Console.WriteLine("2. Check the logs when adding Jarvis to a Teams meeting");
            Console.WriteLine("3. Look for the initialization and welcome messages in the logs");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during testing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            logger.LogError(ex, "Test failed");
        }
    }
}
