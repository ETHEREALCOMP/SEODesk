using SEODesk.Application.Common;
using SEODesk.Application.Features.Sites.Commands;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Sites.Handlers;

public sealed class ToggleFavoriteHandler(ApplicationDbContext _dbContext)
{
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
