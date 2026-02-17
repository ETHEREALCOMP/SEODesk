using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Sites;

// ===== UPDATE SITE TAGS =====

public class UpdateSiteTagsCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
}

public class UpdateSiteTagsHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateSiteTagsHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> HandleAsync(UpdateSiteTagsCommand command)
    {
        // Перевіряємо що сайт належить користувачу
        var site = await _dbContext.Sites
            .Include(s => s.SiteTags)
            .FirstOrDefaultAsync(s => s.Id == command.SiteId && s.UserId == command.UserId);

        if (site == null)
        {
            return Result<bool>.Failure("Site not found");
        }

        // Перевіряємо що всі теги належать користувачу
        var validTags = await _dbContext.Tags
            .Where(t => t.UserId == command.UserId && command.TagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        if (validTags.Count != command.TagIds.Count)
        {
            return Result<bool>.Failure("Some tags not found");
        }

        // Видаляємо старі зв'язки
        _dbContext.SiteTags.RemoveRange(site.SiteTags);

        // Додаємо нові зв'язки
        foreach (var tagId in command.TagIds)
        {
            site.SiteTags.Add(new SiteTag
            {
                SiteId = site.Id,
                TagId = tagId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}

// ===== TOGGLE FAVORITE =====

public class ToggleFavoriteCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public bool IsFavorite { get; set; }
}

public class ToggleFavoriteHandler
{
    private readonly ApplicationDbContext _dbContext;

    public ToggleFavoriteHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> HandleAsync(ToggleFavoriteCommand command)
    {
        var site = await _dbContext.Sites
            .FirstOrDefaultAsync(s => s.Id == command.SiteId && s.UserId == command.UserId);

        if (site == null)
        {
            return Result<bool>.Failure("Site not found");
        }

        site.IsFavorite = command.IsFavorite;
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}

// ===== SYNC SITE DATA =====

public class SyncSiteDataCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class SyncSiteDataHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Infrastructure.Services.GoogleSearchConsoleService _gscService;

    public SyncSiteDataHandler(
        ApplicationDbContext dbContext,
        Infrastructure.Services.GoogleSearchConsoleService gscService)
    {
        _dbContext = dbContext;
        _gscService = gscService;
    }

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
            // Створюємо GSC сервіс
            var gscService = _gscService.CreateService(
                site.User.GoogleRefreshToken,
                clientId,
                clientSecret);

            // Отримуємо базові метрики
            var metrics = await _gscService.GetSiteMetricsAsync(
                gscService,
                site.PropertyId,
                command.StartDate,
                command.EndDate);

            // Отримуємо keywords count по датах
            var keywordsCounts = await _gscService.GetKeywordsCountByDateAsync(
                gscService,
                site.PropertyId,
                command.StartDate,
                command.EndDate);

            // Оновлюємо або створюємо записи метрик
            foreach (var metric in metrics)
            {
                var existing = await _dbContext.SiteMetrics
                    .FirstOrDefaultAsync(m => m.SiteId == site.Id && m.Date == metric.Date);

                var date = DateOnly.FromDateTime(metric.Date);

                var keywordsCount = keywordsCounts.TryGetValue(date, out var count) ? count : 0;

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

// ===== EXPORT SITE DATA =====

public class ExportSiteDataQuery
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class ExportSiteDataHandler
{
    private readonly ApplicationDbContext _dbContext;

    public ExportSiteDataHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<TimeSeriesPointDto>>> HandleAsync(ExportSiteDataQuery query)
    {
        var site = await _dbContext.Sites
            .FirstOrDefaultAsync(s => s.Id == query.SiteId && s.UserId == query.UserId);

        if (site == null)
        {
            return Result<List<TimeSeriesPointDto>>.Failure("Site not found");
        }

        var metrics = await _dbContext.SiteMetrics
            .Where(m => m.SiteId == query.SiteId &&
                        m.Date >= query.DateFrom &&
                        m.Date <= query.DateTo)
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
            .ToListAsync();

        return Result<List<TimeSeriesPointDto>>.Success(metrics);
    }
}