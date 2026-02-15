using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Users;

// ===== GET USER INFO =====

public class GetUserInfoQuery
{
    public Guid UserId { get; set; }
}

public class UserInfoResponse
{
    public UserDto User { get; set; } = new();
    public List<string> Promotions { get; set; } = new();
}

public class UserDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Plan { get; set; } = string.Empty;
}

public class GetUserInfoHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetUserInfoHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserInfoResponse>> HandleAsync(GetUserInfoQuery query)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == query.UserId);

        if (user == null)
        {
            return Result<UserInfoResponse>.Failure("User not found");
        }

        var response = new UserInfoResponse
        {
            User = new UserDto
            {
                Email = user.Email,
                Name = user.Name,
                Avatar = user.Avatar,
                Plan = user.Plan.ToString()
            },
            Promotions = GetActivePromotions(user.Plan)
        };

        return Result<UserInfoResponse>.Success(response);
    }

    /// <summary>
    /// Отримує активні промо для користувача
    /// </summary>
    private List<string> GetActivePromotions(Domain.Entities.PlanType plan)
    {
        var promotions = new List<string>();

        // Якщо TRIAL - показуємо знижку на річний план
        if (plan == Domain.Entities.PlanType.TRIAL)
        {
            promotions.Add("-20% annual");
        }

        return promotions;
    }
}

// ===== GET USER PREFERENCES =====

public class GetUserPreferencesQuery
{
    public Guid UserId { get; set; }
}

public class UserPreferencesDto
{
    public List<string> SelectedMetrics { get; set; } = new();
    public string LastRangePreset { get; set; } = "last28days";
    public Guid? LastGroupId { get; set; }
    public Guid? LastTagId { get; set; }
}

public class GetUserPreferencesHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetUserPreferencesHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserPreferencesDto>> HandleAsync(GetUserPreferencesQuery query)
    {
        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == query.UserId);

        if (preferences == null)
        {
            // Повертаємо дефолтні налаштування
            return Result<UserPreferencesDto>.Success(new UserPreferencesDto
            {
                SelectedMetrics = new List<string> { "clicks", "impressions" },
                LastRangePreset = "last28days"
            });
        }

        return Result<UserPreferencesDto>.Success(new UserPreferencesDto
        {
            SelectedMetrics = preferences.SelectedMetrics.Split(',').ToList(),
            LastRangePreset = preferences.LastRangePreset,
            LastGroupId = preferences.LastGroupId,
            LastTagId = preferences.LastTagId
        });
    }
}

// ===== UPDATE USER PREFERENCES =====

public class UpdateUserPreferencesCommand
{
    public Guid UserId { get; set; }
    public List<string>? SelectedMetrics { get; set; }
    public string? LastRangePreset { get; set; }
    public Guid? LastGroupId { get; set; }
    public Guid? LastTagId { get; set; }
}

public class UpdateUserPreferencesHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateUserPreferencesHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> HandleAsync(UpdateUserPreferencesCommand command)
    {
        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == command.UserId);

        if (preferences == null)
        {
            // Створюємо нові preferences
            preferences = new Domain.Entities.UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                SelectedMetrics = "clicks,impressions",
                LastRangePreset = "last28days",
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.UserPreferences.Add(preferences);
        }

        // Оновлюємо тільки те, що передано
        if (command.SelectedMetrics != null)
        {
            preferences.SelectedMetrics = string.Join(",", command.SelectedMetrics);
        }

        if (command.LastRangePreset != null)
        {
            preferences.LastRangePreset = command.LastRangePreset;
        }

        if (command.LastGroupId.HasValue)
        {
            preferences.LastGroupId = command.LastGroupId.Value;
        }

        if (command.LastTagId.HasValue)
        {
            preferences.LastTagId = command.LastTagId.Value;
        }

        preferences.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
