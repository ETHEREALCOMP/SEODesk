using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Application.Features.Tags.Commands;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Tags.Handlers;

public sealed class DeleteTagHandler(ApplicationDbContext _dbContext)
{
    public async Task<Result<bool>> HandleAsync(DeleteTagCommand command)
    {
        var tag = await _dbContext.Tags
            .Include(t => t.SiteTags)
            .FirstOrDefaultAsync(t => t.Id == command.TagId && t.UserId == command.UserId);

        if (tag == null)
        {
            return Result<bool>.Failure("Tag not found");
        }

        if (!tag.IsDeletable)
        {
            return Result<bool>.Failure("Cannot delete system tag");
        }

        // Видаляємо всі зв'язки з сайтами
        _dbContext.SiteTags.RemoveRange(tag.SiteTags);

        // Видаляємо тег
        _dbContext.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
