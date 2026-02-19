namespace SEODesk.Infrastructure.Services.Interfaces;

public interface IGoogleSearchConsoleService
{
    Task<List<string>> GetUserSitesAsync(string refreshToken);
    Task<List<MetricData>> GetSiteMetricsAsync(string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate);
    Task<int> GetKeywordsCountAsync(string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate);
    Task<Dictionary<DateOnly, int>> GetKeywordsCountByDateAsync(string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate);
}
