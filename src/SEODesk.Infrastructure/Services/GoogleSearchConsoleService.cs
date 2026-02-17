using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.SearchConsole.v1;
using Google.Apis.SearchConsole.v1.Data;
using Google.Apis.Services;

namespace SEODesk.Infrastructure.Services;

/// <summary>
/// Service для роботи з Google Search Console API
/// </summary>
public class GoogleSearchConsoleService
{
    /// <summary>
    /// Створює сервіс GSC з refresh token користувача
    /// </summary>
    public SearchConsoleService CreateService(string refreshToken, string clientId, string clientSecret)
    {
        var credential = new UserCredential(
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    }
                }),
            "user",
            new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = refreshToken
            });

        return new SearchConsoleService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SEODesk"
        });
    }

    /// <summary>
    /// Отримує список сайтів користувача з GSC
    /// </summary>
    public async Task<List<string>> GetUserSitesAsync(SearchConsoleService service)
    {
        var request = service.Sites.List();
        var response = await request.ExecuteAsync();
        
        return response.SiteEntry?
            .Select(site => site.SiteUrl)
            .ToList() ?? new List<string>();
    }

    /// <summary>
    /// Отримує метрики для сайту за період
    /// </summary>
    public async Task<List<MetricData>> GetSiteMetricsAsync(
        SearchConsoleService service,
        string siteUrl,
        DateOnly startDate,
        DateOnly endDate)
    {
        var request = new SearchAnalyticsQueryRequest
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Dimensions = new[] { "date" },
            RowLimit = 25000
        };

        var response = await service.Searchanalytics
            .Query(request, siteUrl)
            .ExecuteAsync();

        if (response.Rows == null || response.Rows.Count == 0)
        {
            return new List<MetricData>();
        }

        return response.Rows.Select(row => new MetricData
        {
            Date = DateOnly.Parse(row.Keys[0]),
            Clicks = (long)(row.Clicks ?? 0),
            Impressions = (long)(row.Impressions ?? 0),
            Ctr = row.Ctr ?? 0,
            AvgPosition = row.Position ?? 0
        }).ToList();
    }

    /// <summary>
    /// Отримує кількість унікальних keywords за період
    /// </summary>
    public async Task<int> GetKeywordsCountAsync(
        SearchConsoleService service,
        string siteUrl,
        DateOnly startDate,
        DateOnly endDate)
    {
        var request = new SearchAnalyticsQueryRequest
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Dimensions = new[] { "query" },
            RowLimit = 25000
        };

        var response = await service.Searchanalytics
            .Query(request, siteUrl)
            .ExecuteAsync();

        return response.Rows?.Count ?? 0;
    }

    /// <summary>
    /// Отримує кількість keywords по датах (для time series)
    /// </summary>
    public async Task<Dictionary<DateOnly, int>> GetKeywordsCountByDateAsync(
        SearchConsoleService service,
        string siteUrl,
        DateOnly startDate,
        DateOnly endDate)
    {
        var request = new SearchAnalyticsQueryRequest
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Dimensions = new[] { "date", "query" },
            RowLimit = 25000
        };

        var response = await service.Searchanalytics
            .Query(request, siteUrl)
            .ExecuteAsync();

        if (response.Rows == null)
        {
            return new Dictionary<DateOnly, int>();
        }

        return response.Rows
            .GroupBy(row => DateOnly.Parse(row.Keys[0]))
            .ToDictionary(
                group => group.Key,
                group => group.Count()
            );
    }
}

public class MetricData
{
    public DateOnly Date { get; set; }
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
}
