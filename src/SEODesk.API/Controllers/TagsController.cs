using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEODesk.Application.Features.Tags;
using SEODesk.Application.Features.Tags.Commands;
using SEODesk.Application.Features.Tags.Handlers;
using SEODesk.Application.Features.Tags.Queries;
using System.Security.Claims;

namespace SEODesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly GetTagsHandler _getTagsHandler;
    private readonly CreateTagHandler _createTagHandler;
    private readonly UpdateTagHandler _updateTagHandler;
    private readonly DeleteTagHandler _deleteTagHandler;

    public TagsController(
        GetTagsHandler getTagsHandler,
        CreateTagHandler createTagHandler,
        UpdateTagHandler updateTagHandler,
        DeleteTagHandler deleteTagHandler)
    {
        _getTagsHandler = getTagsHandler;
        _createTagHandler = createTagHandler;
        _updateTagHandler = updateTagHandler;
        _deleteTagHandler = deleteTagHandler;
    }

    /// <summary>
    /// Отримати всі теги користувача
    /// GET /api/tags
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var userId = GetUserId();
        var query = new GetTagsQuery { UserId = userId };
        var result = await _getTagsHandler.HandleAsync(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Створити новий тег
    /// POST /api/tags
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
    {
        var userId = GetUserId();
        var command = new CreateTagCommand
        {
            UserId = userId,
            Name = request.Name
        };

        var result = await _createTagHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Оновити тег
    /// PATCH /api/tags/{id}
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateTagCommand
        {
            UserId = userId,
            TagId = id,
            Name = request.Name
        };

        var result = await _updateTagHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Видалити тег
    /// DELETE /api/tags/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        var userId = GetUserId();
        var command = new DeleteTagCommand
        {
            UserId = userId,
            TagId = id
        };

        var result = await _deleteTagHandler.HandleAsync(command);

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

// Request DTOs
public record CreateTagRequest(string Name);
public record UpdateTagRequest(string Name);
