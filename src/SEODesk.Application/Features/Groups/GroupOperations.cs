using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Groups;

// ===== GET GROUPS =====

public class GetGroupsQuery
{
    public Guid UserId { get; set; }
}

public class GroupDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EmailOwner { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class GetGroupsHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetGroupsHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<GroupDto>>> HandleAsync(GetGroupsQuery query)
    {
        var groups = await _dbContext.Groups
            .Where(g => g.UserId == query.UserId)
            .OrderByDescending(g => g.IsDefault)
            .ThenBy(g => g.CreatedAt)
            .Select(g => new GroupDto
            {
                Id = g.Id,
                DisplayName = g.DisplayName,
                EmailOwner = g.EmailOwner,
                IsDefault = g.IsDefault
            })
            .ToListAsync();

        // Додаємо "All" групу (віртуальна, не зберігається в БД)
        var allGroup = new GroupDto
        {
            Id = Guid.Empty,
            DisplayName = "All",
            EmailOwner = string.Empty,
            IsDefault = true
        };

        groups.Insert(0, allGroup);

        return Result<List<GroupDto>>.Success(groups);
    }
}

// ===== UPDATE GROUP =====

public class UpdateGroupCommand
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class UpdateGroupHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateGroupHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GroupDto>> HandleAsync(UpdateGroupCommand command)
    {
        // Валідація
        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return Result<GroupDto>.Failure("Group name is required");
        }

        if (command.DisplayName.Length > 40)
        {
            return Result<GroupDto>.Failure("Group name must be 40 characters or less");
        }

        // Знаходимо групу
        var group = await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.GroupId && g.UserId == command.UserId);

        if (group == null)
        {
            return Result<GroupDto>.Failure("Group not found");
        }

        if (group.IsDefault)
        {
            return Result<GroupDto>.Failure("Cannot rename default group");
        }

        // Оновлення
        group.DisplayName = command.DisplayName.Trim();
        await _dbContext.SaveChangesAsync();

        return Result<GroupDto>.Success(new GroupDto
        {
            Id = group.Id,
            DisplayName = group.DisplayName,
            EmailOwner = group.EmailOwner,
            IsDefault = group.IsDefault
        });
    }
}
