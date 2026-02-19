using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Application.Features.Sites.Commands;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Sites.Handlers;

public sealed class UpdateSiteTagsHandler(ApplicationDbContext _dbContext)
{
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
