using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAssistBot.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("src/TeamsAssistBot.Api/appsettings.json")
            .Build();

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<AzureOpenAIService>();

        try
        {
            // Test Azure OpenAI service
            var openAIService = new AzureOpenAIService(configuration, logger);
            
            Console.WriteLine("Testing Azure OpenAI service...");
            
            // Test input validation
            var isValid = await openAIService.ValidateInputAsync("Hello, can you help me?");
            Console.WriteLine($"Input validation result: {isValid}");
            
            // Test message processing
            var processed = await openAIService.ProcessTranscriptionAsync("Hello, can you help me?");
            Console.WriteLine($"Processed message: {processed}");
            
            // Test contextual response
            var response = await openAIService.GetContextualResponseAsync("Hello, can you help me?", new List<string>());
            Console.WriteLine($"AI Response: {response}");
            
            Console.WriteLine("Azure OpenAI service test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing Azure OpenAI service: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
