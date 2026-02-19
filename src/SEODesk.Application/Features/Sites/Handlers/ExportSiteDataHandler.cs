using SEODesk.Application.Common;
using SEODesk.Application.Features.Sites.Queries;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Sites.Handlers;

public sealed class ExportSiteDataHandler(ApplicationDbContext _dbContext)
{
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