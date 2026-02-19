using SEODesk.Application.Common;
using SEODesk.Application.Features.Users.Commands;
using SEODesk.Application.Features.Users.Queries;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Users.Handlers;

public sealed class GetUserPreferencesHandler(ApplicationDbContext _dbContext)
{
    public async Task<Result<UserPreferencesCommand>> HandleAsync(GetUserPreferencesQuery query)
    {
        var preferences = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == query.UserId);

        if (preferences == null)
        {
            // Повертаємо дефолтні налаштування
            return Result<UserPreferencesCommand>.Success(new UserPreferencesCommand
            {
                SelectedMetrics = new List<string> { "clicks", "impressions" },
                LastRangePreset = "last28days"
            });
        }

        return Result<UserPreferencesCommand>.Success(new UserPreferencesCommand
        {
            SelectedMetrics = preferences.SelectedMetrics.Split(',').ToList(),
            LastRangePreset = preferences.LastRangePreset,
            LastGroupId = preferences.LastGroupId,
            LastTagId = preferences.LastTagId
        });
    }
}
