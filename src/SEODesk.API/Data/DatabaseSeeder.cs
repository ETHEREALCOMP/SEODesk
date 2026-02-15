using Microsoft.EntityFrameworkCore;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;

namespace SEODesk.API.Data;

/// <summary>
/// Клас для seed даних в базу (для розробки)
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Створюємо базу якщо не існує
        await context.Database.EnsureCreatedAsync();

        // Перевіряємо чи є користувачі
        if (await context.Users.AnyAsync())
        {
            return; // База вже заповнена
        }

        // Створюємо тестового користувача
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Plan = PlanType.TRIAL,
            GoogleRefreshToken = "test_refresh_token",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(testUser);

        // Створюємо дефолтну групу "My sites"
        var defaultGroup = new Group
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            DisplayName = "My sites",
            EmailOwner = testUser.Email,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Groups.Add(defaultGroup);

        // Створюємо дефолтний тег "All"
        var allTag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            Name = "All",
            IsDeletable = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Tags.Add(allTag);

        // Створюємо кілька тестових тегів
        var tags = new[]
        {
            new Tag
            {
                Id = Guid.NewGuid(),
                UserId = testUser.Id,
                Name = "E-commerce",
                IsDeletable = true,
                CreatedAt = DateTime.UtcNow
            },
            new Tag
            {
                Id = Guid.NewGuid(),
                UserId = testUser.Id,
                Name = "Blog",
                IsDeletable = true,
                CreatedAt = DateTime.UtcNow
            },
            new Tag
            {
                Id = Guid.NewGuid(),
                UserId = testUser.Id,
                Name = "SaaS",
                IsDeletable = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Tags.AddRange(tags);

        // Створюємо тестовий сайт
        var testSite = new Site
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            GroupId = defaultGroup.Id,
            PropertyId = "sc-domain:example.com",
            Domain = "example.com",
            IsFavorite = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Sites.Add(testSite);

        // Прив'язуємо тег до сайту
        context.SiteTags.Add(new SiteTag
        {
            SiteId = testSite.Id,
            TagId = tags[0].Id, // E-commerce
            CreatedAt = DateTime.UtcNow
        });

        // Створюємо тестові метрики (останні 7 днів)
        var metrics = new List<SiteMetric>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
            metrics.Add(new SiteMetric
            {
                Id = Guid.NewGuid(),
                SiteId = testSite.Id,
                Date = date,
                Clicks = Random.Shared.Next(100, 500),
                Impressions = Random.Shared.Next(1000, 5000),
                Ctr = Random.Shared.NextDouble() * 0.1, // 0-10%
                AvgPosition = Random.Shared.NextDouble() * 20 + 5, // 5-25
                KeywordsCount = Random.Shared.Next(50, 200),
                CreatedAt = DateTime.UtcNow
            });
        }

        context.SiteMetrics.AddRange(metrics);

        // Створюємо preferences
        var preferences = new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            SelectedMetrics = "clicks,impressions",
            LastRangePreset = "last28days",
            UpdatedAt = DateTime.UtcNow
        };

        context.UserPreferences.Add(preferences);

        // Зберігаємо всі зміни
        await context.SaveChangesAsync();
    }
}

// Додайте в Program.cs перед app.Run():
/*
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}
*/
