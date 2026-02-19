using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEODesk.Application.Common;
using SEODesk.Application.Features.Dashboard.Commands;
using SEODesk.Application.Features.Dashboard.Queries;
using SEODesk.Application.Features.Dashboard.Response;
using SEODesk.Application.Features.Sites.Commands;
using SEODesk.Application.Features.Sites.Handlers;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Dashboard.Handlers;

public sealed class GetDashboardHandler(ApplicationDbContext _dbContext,
    DiscoverSitesHandler _discoverSitesHandler, IConfiguration _configuration)
{
    public async Task<Result<GetDashboardResponse>> HandleAsync(GetDashboardQuery query)
    {
        // 1. Отримуємо сайти користувача з фільтрацією
        var sitesQuery = _dbContext.Sites
            .Include(s => s.SiteTags)
            .ThenInclude(st => st.Tag)
            .Where(s => s.UserId == query.UserId);

        var sites = await sitesQuery.ToListAsync();

        // 🔥 Якщо сайтів 0 - пробуємо "підтягнути" їх автоматично
        if (sites.Count == 0)
        {
            var clientId = _configuration["Google:ClientId"];
            var clientSecret = _configuration["Google:ClientSecret"];

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                await _discoverSitesHandler.HandleAsync(
                    new DiscoverSitesCommand { UserId = query.UserId });

                // Перезапитуємо сайти
                sites = await sitesQuery.ToListAsync();
            }
        }

        var totalUserSitesCount = sites.Count;

        // Фільтр по групі (якщо вони вже є)
        if (query.GroupId.HasValue && query.GroupId.Value != Guid.Empty)
        {
            sites = sites.Where(s => s.GroupId == query.GroupId.Value).ToList();
        }

        // Фільтр по тегу
        if (query.TagId.HasValue && query.TagId.Value != Guid.Empty)
        {
            sites = sites.Where(s => s.SiteTags.Any(st => st.TagId == query.TagId.Value)).ToList();
        }

        if (sites.Count == 0 && totalUserSitesCount > 0)
        {
            return Result<GetDashboardResponse>.Success(new GetDashboardResponse { TotalUserSites = totalUserSitesCount });
        }

        if (sites.Count == 0)
        {
            return Result<GetDashboardResponse>.Success(new GetDashboardResponse());
        }

        // 2. Отримуємо метрики для кожного сайту
        var siteIds = sites.Select(s => s.Id).ToList();
        var metrics = await _dbContext.SiteMetrics
            .Where(m => siteIds.Contains(m.SiteId) &&
                        m.Date >= query.DateFrom &&
                        m.Date <= query.DateTo)
            .ToListAsync();

        // 3. Формуємо DTO для сайтів
        var siteDtos = sites.Select(site =>
        {
            var siteMetrics = metrics.Where(m => m.SiteId == site.Id).ToList();

            return new SiteCommand
            {
                Id = site.Id,
                PropertyId = site.PropertyId,
                Domain = site.Domain,
                IsFavorite = site.IsFavorite,
                LastSynced = site.LastSyncedAt,
                SyncError = site.SyncError,
                Tags = site.SiteTags.Select(st => st.TagId.ToString()).ToList(),
                Totals = CalculateTotals(siteMetrics),
                TimeSeries = siteMetrics
                    .OrderBy(m => m.Date)
                    .Select(m => new TimeSeriesPointDto
                    {
                        Date = m.Date.ToString("yyyy-MM-dd"),
                        Clicks = m.Clicks,
                        Impressions = m.Impressions,
                        Ctr = m.Ctr,
                        AvgPosition = m.AvgPosition,
                        KeywordsCount = m.KeywordsCount
                    })
                    .ToList()
            };
        }).ToList();

        // 4. Сортування
        siteDtos = SortSites(siteDtos, query.SortBy, query.SortDir);

        // 5. Агрегація загальних метрик
        var summary = CalculateSummary(siteDtos);

        // 6. Агрегація time series
        var timeSeries = AggregateTimeSeries(siteDtos);

        return Result<GetDashboardResponse>.Success(new GetDashboardResponse
        {
            Summary = summary,
            TimeSeries = timeSeries,
            Sites = siteDtos,
            TotalUserSites = totalUserSitesCount
        });
    }

    /// <summary>
    /// Розрахунок totals для сайту
    /// </summary>
    private MetricDto CalculateTotals(List<Domain.Entities.SiteMetric> metrics)
    {
        if (metrics.Count == 0)
        {
            return new MetricDto();
        }

        var totalClicks = metrics.Sum(m => m.Clicks);
        var totalImpressions = metrics.Sum(m => m.Impressions);
        var totalWeightedPosition = metrics.Sum(m => m.AvgPosition * m.Impressions);
        var totalKeywords = metrics.Sum(m => m.KeywordsCount);

        return new MetricDto
        {
            Clicks = totalClicks,
            Impressions = totalImpressions,
            Ctr = totalImpressions > 0 ? (double)totalClicks / totalImpressions : 0,
            AvgPosition = totalImpressions > 0 ? totalWeightedPosition / totalImpressions : 0,
            KeywordsCount = totalKeywords
        };
    }

    /// <summary>
    /// Сортування сайтів
    /// </summary>
    private List<SiteCommand> SortSites(List<SiteCommand> sites, string sortBy, string sortDir)
    {
        var sorted = sortBy.ToLower() switch
        {
            "clicks" => sites.OrderBy(s => s.Totals.Clicks),
            "impressions" => sites.OrderBy(s => s.Totals.Impressions),
            "name" => sites.OrderBy(s => s.Domain),
            _ => sites.OrderBy(s => s.Totals.Clicks)
        };

        return sortDir.ToLower() == "desc"
            ? sorted.Reverse().ToList()
            : sorted.ToList();
    }

    /// <summary>
    /// Розрахунок загальних метрик
    /// </summary>
    private MetricDto CalculateSummary(List<SiteCommand> sites)
    {
        var totalClicks = sites.Sum(s => s.Totals.Clicks);
        var totalImpressions = sites.Sum(s => s.Totals.Impressions);
        var totalWeightedPosition = sites.Sum(s => s.Totals.AvgPosition * s.Totals.Impressions);
        var totalKeywords = sites.Sum(s => s.Totals.KeywordsCount);

        return new MetricDto
        {
            Clicks = totalClicks,
            Impressions = totalImpressions,
            Ctr = totalImpressions > 0 ? (double)totalClicks / totalImpressions : 0,
            AvgPosition = totalImpressions > 0 ? totalWeightedPosition / totalImpressions : 0,
            KeywordsCount = totalKeywords
        };
    }

    /// <summary>
    /// Агрегація time series по всіх сайтах
    /// </summary>
    private List<TimeSeriesPointDto> AggregateTimeSeries(List<SiteCommand> sites)
    {
        var dateGroups = sites
            .SelectMany(s => s.TimeSeries)
            .GroupBy(t => t.Date)
            .OrderBy(g => g.Key);

        return dateGroups.Select(group =>
        {
            var totalClicks = group.Sum(t => t.Clicks);
            var totalImpressions = group.Sum(t => t.Impressions);
            var totalWeightedPosition = group.Sum(t => t.AvgPosition * t.Impressions);
            var totalKeywords = group.Sum(t => t.KeywordsCount);

            return new TimeSeriesPointDto
            {
                Date = group.Key,
                Clicks = totalClicks,
                Impressions = totalImpressions,
                Ctr = totalImpressions > 0 ? (double)totalClicks / totalImpressions : 0,
                AvgPosition = totalImpressions > 0 ? totalWeightedPosition / totalImpressions : 0,
                KeywordsCount = totalKeywords
            };
        }).ToList();
    }
}
