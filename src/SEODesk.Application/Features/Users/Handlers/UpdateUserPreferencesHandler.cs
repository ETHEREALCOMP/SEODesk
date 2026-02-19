using SEODesk.Application.Common;
using SEODesk.Application.Features.Users.Commands;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Users.Handlers;

public sealed class UpdateUserPreferencesHandler(ApplicationDbContext _dbContext)
{
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
