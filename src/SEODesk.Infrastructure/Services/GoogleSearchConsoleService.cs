using Google.Apis.SearchConsole.v1.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SEODesk.Infrastructure.Services.Interfaces;

namespace SEODesk.Infrastructure.Services;

/// <summary>
/// Service для роботи з Google Search Console API
/// </summary>
public sealed class GoogleSearchConsoleService(
    IGoogleAuthService _authService,
    IMemoryCache _cache,
    ILogger<GoogleSearchConsoleService> _logger) : IGoogleSearchConsoleService
{
    private const int RowLimit = 25000;

    public async Task<List<string>> GetUserSitesAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        var cacheKey = $"sites:{refreshToken.GetHashCode()}";

        if (_cache.TryGetValue(cacheKey, out List<string>? cached))
        {
            _logger.LogDebug("Sites returned from cache");
            return FilterDuplicateDomains(cached!);
        }

        var service = _authService.CreateService(refreshToken);
        var response = await ExecuteWithRetryAsync(() =>
            service.Sites.List().ExecuteAsync());

        var sites = response.SiteEntry?
            .Select(s => s.SiteUrl)
            .ToList() ?? new List<string>();

        _logger.LogInformation("Fetched {Count} sites from GSC", sites.Count);
        _cache.Set(cacheKey, sites, TimeSpan.FromMinutes(10));

        return FilterDuplicateDomains(sites);
    }

    public async Task<List<MetricData>> GetSiteMetricsAsync(
        string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate)
    {
        ValidateArgs(refreshToken, siteUrl, startDate, endDate);

        var rows = await FetchAnalyticsRowsAsync(
            refreshToken, siteUrl, startDate, endDate, ["date"]);

        return rows.Select(r => new MetricData
        {
            Date = DateOnly.Parse(r.Keys[0]),
            Clicks = (long)(r.Clicks ?? 0),
            Impressions = (long)(r.Impressions ?? 0),
            Ctr = r.Ctr ?? 0,
            AvgPosition = r.Position ?? 0
        }).ToList();
    }

    public async Task<int> GetKeywordsCountAsync(
        string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate)
    {
        ValidateArgs(refreshToken, siteUrl, startDate, endDate);

        var rows = await FetchAnalyticsRowsAsync(
            refreshToken, siteUrl, startDate, endDate, ["query"]);

        return rows.Count;
    }

    public async Task<Dictionary<DateOnly, int>> GetKeywordsCountByDateAsync(
        string refreshToken, string siteUrl, DateOnly startDate, DateOnly endDate)
    {
        ValidateArgs(refreshToken, siteUrl, startDate, endDate);

        var rows = await FetchAnalyticsRowsAsync(
            refreshToken, siteUrl, startDate, endDate, ["date", "query"]);

        return rows
            .GroupBy(r => DateOnly.Parse(r.Keys[0]))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<List<ApiDataRow>> FetchAnalyticsRowsAsync(
        string refreshToken, string siteUrl,
        DateOnly startDate, DateOnly endDate, string[] dimensions)
    {
        var service = _authService.CreateService(refreshToken);
        var allRows = new List<ApiDataRow>();
        int startRow = 0;

        while (true)
        {
            var request = new SearchAnalyticsQueryRequest
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Dimensions = dimensions,
                RowLimit = RowLimit,
                StartRow = startRow
            };

            var response = await ExecuteWithRetryAsync(() =>
                service.Searchanalytics.Query(request, siteUrl).ExecuteAsync());

            if (response.Rows == null || response.Rows.Count == 0)
                break;

            allRows.AddRange(response.Rows);

            _logger.LogDebug("Fetched rows {Start}-{End} for {Site}",
                startRow, startRow + response.Rows.Count, siteUrl);

            if (response.Rows.Count < RowLimit)
                break;

            startRow += RowLimit;
        }

        _logger.LogInformation("Total rows fetched: {Count} for {Site}", allRows.Count, siteUrl);
        return allRows;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        int attempt = 0;
        int[] delays = [1000, 2000, 5000];

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Google.GoogleApiException ex) when (
                ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                ex.HttpStatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                if (attempt >= delays.Length)
                {
                    _logger.LogError(ex, "GSC API failed after {Attempts} attempts", attempt);
                    throw;
                }

                _logger.LogWarning("GSC API returned {Status}, retry {Attempt} after {Delay}ms",
                    ex.HttpStatusCode, attempt + 1, delays[attempt]);

                await Task.Delay(delays[attempt]);
                attempt++;
            }
        }
    }

    private static void ValidateArgs(string refreshToken, string siteUrl,
        DateOnly startDate, DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        ArgumentException.ThrowIfNullOrEmpty(siteUrl);

        if (startDate > endDate)
            throw new ArgumentException($"startDate {startDate} cannot be after endDate {endDate}");
    }

    private static List<string> FilterDuplicateDomains(List<string> sites)
    {
        var domainProperties = sites
            .Where(s => s.StartsWith("sc-domain:"))
            .Select(s => s.Replace("sc-domain:", "").TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filteredUrls = sites
            .Where(s => !s.StartsWith("sc-domain:"))
            .Where(s =>
            {
                if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
                    return !domainProperties.Any(d =>
                        uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("www." + d, StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
                return true;
            }).ToList();

        return sites
            .Where(s => s.StartsWith("sc-domain:"))
            .Concat(filteredUrls)
            .ToList();
    }
}