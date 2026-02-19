using SEODesk.Application.Common;
using SEODesk.Application.Features.Tags.Queries;
using SEODesk.Application.Features.Tags.Response;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Tags.Handlers;

public sealed class GetTagsHandler(ApplicationDbContext _dbContext)
{
    public async Task<Result<List<CreateTagResponse>>> HandleAsync(GetTagsQuery query)
    {
        var tags = await _dbContext.Tags
            .Where(t => t.UserId == query.UserId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new CreateTagResponse
            {
                Id = t.Id,
                Name = t.Name,
                IsDeletable = t.IsDeletable
            })
            .ToListAsync();

        return Result<List<CreateTagResponse>>.Success(tags);
    }
}
