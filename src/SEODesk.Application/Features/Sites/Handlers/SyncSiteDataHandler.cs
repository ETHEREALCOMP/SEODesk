using SEODesk.Application.Common;
using SEODesk.Application.Features.Sites.Commands;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;
using SEODesk.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Sites.Handlers;

public sealed class SyncSiteDataHandler(ApplicationDbContext _dbContext,
    IGoogleSearchConsoleService _gscService)
{
    public async Task<Site?> GetSiteByPropertyIdAsync(Guid userId, string propertyId)
    {
        return await _dbContext.Sites
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PropertyId == propertyId);
    }

    public async Task<Result<bool>> HandleAsync(SyncSiteDataCommand command, string clientId, string clientSecret)
    {
        // Отримуємо сайт та користувача
        var site = await _dbContext.Sites
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == command.SiteId && s.UserId == command.UserId);

        if (site == null)
        {
            return Result<bool>.Failure("Site not found");
        }

        try
        {
            var metricsTask = _gscService.GetSiteMetricsAsync(
            site.User.GoogleRefreshToken, site.PropertyId, command.StartDate, command.EndDate);

            var keywordsTask = _gscService.GetKeywordsCountByDateAsync(
                site.User.GoogleRefreshToken, site.PropertyId, command.StartDate, command.EndDate);

            await Task.WhenAll(metricsTask, keywordsTask);

            var metrics = metricsTask.Result;
            var keywordsCounts = keywordsTask.Result;

            // Оновлюємо або створюємо записи метрик
            foreach (var metric in metrics)
            {
                var existing = await _dbContext.SiteMetrics
                    .FirstOrDefaultAsync(m => m.SiteId == site.Id && m.Date == metric.Date);

                var keywordsCount = keywordsCounts.TryGetValue(metric.Date, out var count) ? count : 0;

                if (existing != null)
                {
                    // Оновлюємо існуючий запис
                    existing.Clicks = metric.Clicks;
                    existing.Impressions = metric.Impressions;
                    existing.Ctr = metric.Ctr;
                    existing.AvgPosition = metric.AvgPosition;
                    existing.KeywordsCount = keywordsCount;
                }
                else
                {
                    // Створюємо новий запис
                    _dbContext.SiteMetrics.Add(new SiteMetric
                    {
                        Id = Guid.NewGuid(),
                        SiteId = site.Id,
                        Date = metric.Date,
                        Clicks = metric.Clicks,
                        Impressions = metric.Impressions,
                        Ctr = metric.Ctr,
                        AvgPosition = metric.AvgPosition,
                        KeywordsCount = keywordsCount,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Оновлюємо час останньої синхронізації
            site.LastSyncedAt = DateTime.UtcNow;
            site.SyncError = null;

            await _dbContext.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // Зберігаємо помилку синхронізації
            site.SyncError = ex.Message;
            await _dbContext.SaveChangesAsync();

            return Result<bool>.Failure($"Sync failed: {ex.Message}");
        }
    }
}
