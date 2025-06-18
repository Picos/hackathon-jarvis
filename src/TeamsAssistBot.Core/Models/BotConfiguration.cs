namespace TeamsAssistBot.Core.Models;

public class BotConfiguration
{
    public string MicrosoftAppId { get; set; } = string.Empty;
    public string MicrosoftAppPassword { get; set; } = string.Empty;
    public string MicrosoftAppTenantId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string MediaBaseUrl { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
}
