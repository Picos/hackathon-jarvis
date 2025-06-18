namespace TeamsAssistBot.Core.Models;

public class AzureServicesConfiguration
{
    public SpeechServiceConfig SpeechService { get; set; } = new();
    public OpenAIConfig OpenAI { get; set; } = new();
    public KeyVaultConfig KeyVault { get; set; } = new();
    public ApplicationInsightsConfig ApplicationInsights { get; set; } = new();
}

public class SpeechServiceConfig
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Language { get; set; } = "en-US";
    public string WakeWord { get; set; } = "Hey AssistBot";
}

public class OpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string TextToSpeechVoice { get; set; } = "en-US-AriaNeural";
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
}

public class KeyVaultConfig
{
    public string VaultUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class ApplicationInsightsConfig
{
    public string InstrumentationKey { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
