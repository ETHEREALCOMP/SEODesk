namespace SEODesk.Infrastructure.Options;

public class GoogleSearchConsoleOptions
{
    public const string SectionName = "GoogleSearchConsole";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "SEODesk";
}
