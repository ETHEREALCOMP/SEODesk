using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;
using SEODesk.Infrastructure.Services;

namespace SEODesk.Application.Features.Sites;

public class DiscoverSitesCommand
{
    public Guid UserId { get; set; }
}

public class DiscoverSitesHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GoogleSearchConsoleService _gscService;

    public DiscoverSitesHandler(
        ApplicationDbContext dbContext,
        GoogleSearchConsoleService gscService)
    {
        _dbContext = dbContext;
        _gscService = gscService;
    }

    public async Task<Result<int>> HandleAsync(DiscoverSitesCommand command, string clientId, string clientSecret)
    {
        var user = await _dbContext.Users
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.Id == command.UserId);

        if (user == null || string.IsNullOrEmpty(user.GoogleRefreshToken))
        {
            return Result<int>.Failure("User or refresh token not found");
        }

        try
        {
            var gscService = _gscService.CreateService(user.GoogleRefreshToken, clientId, clientSecret);
            var remoteSites = await _gscService.GetUserSitesAsync(gscService);

            if (remoteSites.Count == 0)
            {
                return Result<int>.Success(0);
            }

            var localSites = await _dbContext.Sites
                .Where(s => s.UserId == user.Id)
                .Select(s => s.PropertyId)
                .ToListAsync();

            var defaultGroup = user.Groups.FirstOrDefault(g => g.IsDefault) 
                             ?? user.Groups.First();

            int newlyAdded = 0;

            foreach (var siteUrl in remoteSites)
            {
                if (!localSites.Contains(siteUrl))
                {
                    var domain = new Uri(siteUrl).Host;
                    
                    _dbContext.Sites.Add(new Site
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        GroupId = defaultGroup.Id,
                        PropertyId = siteUrl,
                        Domain = domain,
                        CreatedAt = DateTime.UtcNow,
                    });
                    newlyAdded++;
                }
            }

            if (newlyAdded > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            return Result<int>.Success(newlyAdded);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Discovery failed: {ex.Message}");
        }
    }
}
