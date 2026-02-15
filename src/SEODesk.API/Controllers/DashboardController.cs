using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEODesk.Application.Features.Dashboard;
using System.Security.Claims;

namespace SEODesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GetDashboardHandler _getDashboardHandler;

    public DashboardController(GetDashboardHandler getDashboardHandler)
    {
        _getDashboardHandler = getDashboardHandler;
    }

    /// <summary>
    /// Отримати дані для dashboard (може приймати редірект з логіну)
    /// GET /api/dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? tagId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] DateOnly? compareFrom,
        [FromQuery] DateOnly? compareTo,
        [FromQuery] string sortBy = "clicks",
        [FromQuery] string sortDir = "desc")
    {
        var userId = GetUserId();

        // Якщо немає дат - використати дефолтні (останні 28 днів)
        var endDate = dateTo ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = dateFrom ?? endDate.AddDays(-28);

        var query = new GetDashboardQuery
        {
            UserId = userId,
            GroupId = groupId,
            TagId = tagId,
            DateFrom = startDate,
            DateTo = endDate,
            CompareFrom = compareFrom,
            CompareTo = compareTo,
            SortBy = sortBy,
            SortDir = sortDir
        };

        var result = await _getDashboardHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    private Guid GetUserId()
    {
        // Use the internal Guid ID we put in the "userId" claim
        var userIdClaim = User.FindFirst("userId")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID claim not found");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // If it's not a Guid, it might be the GoogleId (sub). 
            // We should try to look up the user in that case, but the claim "userId" 
            // should always be provided by our AuthController.
            throw new UnauthorizedAccessException("Invalid User ID format");
        }

        return userId;
    }
}