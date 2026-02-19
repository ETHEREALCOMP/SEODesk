using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEODesk.Application.Features.Users;
using SEODesk.Application.Features.Users.Commands;
using SEODesk.Application.Features.Users.Handlers;
using SEODesk.Application.Features.Users.Queries;
using System.Security.Claims;

namespace SEODesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly GetUserInfoHandler _getUserInfoHandler;
    private readonly GetUserPreferencesHandler _getUserPreferencesHandler;
    private readonly UpdateUserPreferencesHandler _updateUserPreferencesHandler;

    public UserController(
        GetUserInfoHandler getUserInfoHandler,
        GetUserPreferencesHandler getUserPreferencesHandler,
        UpdateUserPreferencesHandler updateUserPreferencesHandler)
    {
        _getUserInfoHandler = getUserInfoHandler;
        _getUserPreferencesHandler = getUserPreferencesHandler;
        _updateUserPreferencesHandler = updateUserPreferencesHandler;
    }

    /// <summary>
    /// Отримати інформацію про користувача
    /// GET /api/user/me
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = GetUserId();
        var query = new GetUserInfoQuery { UserId = userId };
        var result = await _getUserInfoHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Отримати налаштування користувача
    /// GET /api/user/preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var query = new GetUserPreferencesQuery { UserId = userId };
        var result = await _getUserPreferencesHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Оновити налаштування користувача
    /// PATCH /api/user/preferences
    /// </summary>
    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateUserPreferencesCommand
        {
            UserId = userId,
            SelectedMetrics = request.SelectedMetrics,
            LastRangePreset = request.LastRangePreset,
            LastGroupId = request.LastGroupId,
            LastTagId = request.LastTagId
        };

        var result = await _updateUserPreferencesHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
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

public record UpdatePreferencesRequest(
    List<string>? SelectedMetrics,
    string? LastRangePreset,
    Guid? LastGroupId,
    Guid? LastTagId
);
