using SEODesk.Application.Common;
using SEODesk.Application.Features.Groups.Commands;
using SEODesk.Application.Features.Groups.Queries;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Groups.Handlers;

public sealed class GetGroupsHandler(ApplicationDbContext _dbContext)
{
    public async Task<Result<List<GroupCommand>>> HandleAsync(GetGroupsQuery query)
    {
        var groups = await _dbContext.Groups
            .Where(g => g.UserId == query.UserId)
            .OrderByDescending(g => g.IsDefault)
            .ThenBy(g => g.CreatedAt)
            .Select(g => new GroupCommand
            {
                Id = g.Id,
                DisplayName = g.DisplayName,
                EmailOwner = g.EmailOwner,
                IsDefault = g.IsDefault
            })
            .ToListAsync();

        // Додаємо "All" групу (віртуальна, не зберігається в БД)
        var allGroup = new GroupCommand
        {
            Id = Guid.Empty,
            DisplayName = "All",
            EmailOwner = string.Empty,
            IsDefault = true
        };

        groups.Insert(0, allGroup);

        return Result<List<GroupCommand>>.Success(groups);
    }
}
