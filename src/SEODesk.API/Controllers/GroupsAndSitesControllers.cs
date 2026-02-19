using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEODesk.Application.Features.Groups;
using SEODesk.Application.Features.Groups.Commands;
using SEODesk.Application.Features.Groups.Handlers;
using SEODesk.Application.Features.Groups.Queries;
using SEODesk.Application.Features.Sites;
using SEODesk.Application.Features.Sites.Commands;
using SEODesk.Application.Features.Sites.Handlers;
using SEODesk.Application.Features.Sites.Queries;
using System.Security.Claims;
using System.Text;

namespace SEODesk.API.Controllers;

// ===== GROUPS CONTROLLER =====

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly GetGroupsHandler _getGroupsHandler;
    private readonly UpdateGroupHandler _updateGroupHandler;

    public GroupsController(
        GetGroupsHandler getGroupsHandler,
        UpdateGroupHandler updateGroupHandler)
    {
        _getGroupsHandler = getGroupsHandler;
        _updateGroupHandler = updateGroupHandler;
    }

    /// <summary>
    /// Отримати всі групи користувача
    /// GET /api/groups
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var userId = GetUserId();
        var query = new GetGroupsQuery { UserId = userId };
        var result = await _getGroupsHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Оновити групу
    /// PATCH /api/groups/{id}
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateGroupRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateGroupCommand
        {
            UserId = userId,
            GroupId = id,
            DisplayName = request.DisplayName
        };

        var result = await _updateGroupHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

public record UpdateGroupRequest(string DisplayName);

// ===== SITES CONTROLLER =====

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SitesController : ControllerBase
{
    private readonly UpdateSiteTagsHandler _updateSiteTagsHandler;
    private readonly ToggleFavoriteHandler _toggleFavoriteHandler;
    private readonly SyncSiteDataHandler _syncSiteDataHandler;
    private readonly ExportSiteDataHandler _exportSiteDataHandler;
    private readonly DiscoverSitesHandler _discoverSitesHandler;
    private readonly IConfiguration _configuration;

    public SitesController(
        UpdateSiteTagsHandler updateSiteTagsHandler,
        ToggleFavoriteHandler toggleFavoriteHandler,
        SyncSiteDataHandler syncSiteDataHandler,
        ExportSiteDataHandler exportSiteDataHandler,
        DiscoverSitesHandler discoverSitesHandler,
        IConfiguration configuration)
    {
        _updateSiteTagsHandler = updateSiteTagsHandler;
        _toggleFavoriteHandler = toggleFavoriteHandler;
        _syncSiteDataHandler = syncSiteDataHandler;
        _exportSiteDataHandler = exportSiteDataHandler;
        _discoverSitesHandler = discoverSitesHandler;
        _configuration = configuration;
    }

    /// <summary>
    /// Знайти нові сайти в GSC
    /// POST /api/sites/discover
    /// </summary>
    [HttpPost("discover")]
    public async Task<IActionResult> DiscoverSites()
    {
        var userId = GetUserId();
        var command = new DiscoverSitesCommand { UserId = userId };

        var result = await _discoverSitesHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { newlyAdded = result.Value });
    }

    private async Task<Guid?> ResolveSiteIdAsync(Guid userId, string id)
    {
        if (Guid.TryParse(id, out var siteGuid))
        {
            return siteGuid;
        }

        // Fallback: search by PropertyId
        var site = await _syncSiteDataHandler.GetSiteByPropertyIdAsync(userId, id);
        return site?.Id;
    }

    /// <summary>
    /// Оновити теги сайту
    /// PUT /api/sites/{id}/tags
    /// </summary>
    [HttpPut("{id}/tags")]
    public async Task<IActionResult> UpdateSiteTags(string id, [FromBody] UpdateSiteTagsRequest request)
    {
        var userId = GetUserId();
        var siteId = await ResolveSiteIdAsync(userId, id);
        
        if (siteId == null)
        {
            return BadRequest(new { error = "Site not found" });
        }

        var command = new UpdateSiteTagsCommand
        {
            UserId = userId,
            SiteId = siteId.Value,
            TagIds = request.TagIds
        };

        var result = await _updateSiteTagsHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Змінити статус favorite
    /// PUT /api/sites/{id}/favorite
    /// </summary>
    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(string id, [FromBody] ToggleFavoriteRequest request)
    {
        var userId = GetUserId();
        var siteId = await ResolveSiteIdAsync(userId, id);

        if (siteId == null)
        {
            return BadRequest(new { error = "Site not found" });
        }

        var command = new ToggleFavoriteCommand
        {
            UserId = userId,
            SiteId = siteId.Value,
            IsFavorite = request.IsFavorite
        };

        var result = await _toggleFavoriteHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Синхронізувати дані з GSC
    /// POST /api/sites/{id}/sync
    /// </summary>
    [HttpPost("{id}/sync")]
    public async Task<IActionResult> SyncSiteData(string id, [FromBody] SyncSiteDataRequest request)
    {
        var userId = GetUserId();
        var siteId = await ResolveSiteIdAsync(userId, id);

        if (siteId == null)
        {
            return BadRequest(new { error = "Site not found" });
        }

        var command = new SyncSiteDataCommand
        {
            UserId = userId,
            SiteId = siteId.Value,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var clientId = _configuration["GoogleSearchConsole:ClientId"] 
            ?? throw new InvalidOperationException("Google ClientId not configured");
        var clientSecret = _configuration["GoogleSearchConsole:ClientSecret"] 
            ?? throw new InvalidOperationException("Google ClientSecret not configured");

        var result = await _syncSiteDataHandler.HandleAsync(command, clientId, clientSecret);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Експортувати дані сайту
    /// GET /api/sites/{id}/export
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportSiteData(
        string id,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        [FromQuery] string format = "csv")
    {
        var userId = GetUserId();
        var siteId = await ResolveSiteIdAsync(userId, id);

        if (siteId == null)
        {
            return BadRequest(new { error = "Site not found" });
        }

        var query = new ExportSiteDataQuery
        {
            UserId = userId,
            SiteId = siteId.Value,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        var result = await _exportSiteDataHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        if (format.ToLower() == "csv")
        {
            var csv = GenerateCsv(result.Value!);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"site-export-{id}.csv");
        }

        return BadRequest(new { error = "Unsupported format" });
    }

    private string GenerateCsv(List<Application.Common.TimeSeriesPointDto> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Clicks,Impressions,CTR,Avg Position,Keywords Count");

        foreach (var point in data)
        {
            sb.AppendLine($"{point.Date},{point.Clicks},{point.Impressions},{point.Ctr:F4},{point.AvgPosition:F2},{point.KeywordsCount}");
        }

        return sb.ToString();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

public record UpdateSiteTagsRequest(List<Guid> TagIds);
public record ToggleFavoriteRequest(bool IsFavorite);
public record SyncSiteDataRequest(DateOnly StartDate, DateOnly EndDate);
