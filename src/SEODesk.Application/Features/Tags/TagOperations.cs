using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Common;
using SEODesk.Domain.Entities;
using SEODesk.Infrastructure.Data;

namespace SEODesk.Application.Features.Tags;

// ===== CREATE TAG =====

public class CreateTagCommand
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateTagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeletable { get; set; }
}

public class CreateTagHandler
{
    private readonly ApplicationDbContext _dbContext;

    public CreateTagHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateTagResponse>> HandleAsync(CreateTagCommand command)
    {
        // Валідація
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result<CreateTagResponse>.Failure("Tag name is required");
        }

        if (command.Name.Length > 30)
        {
            return Result<CreateTagResponse>.Failure("Tag name must be 30 characters or less");
        }

        // Перевірка унікальності
        var exists = await _dbContext.Tags
            .AnyAsync(t => t.UserId == command.UserId && t.Name == command.Name);

        if (exists)
        {
            return Result<CreateTagResponse>.Failure("Tag with this name already exists");
        }

        // Створення тегу
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            Name = command.Name.Trim(),
            IsDeletable = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        return Result<CreateTagResponse>.Success(new CreateTagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            IsDeletable = tag.IsDeletable
        });
    }
}

// ===== UPDATE TAG =====

public class UpdateTagCommand
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateTagHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateTagHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateTagResponse>> HandleAsync(UpdateTagCommand command)
    {
        // Валідація
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result<CreateTagResponse>.Failure("Tag name is required");
        }

        if (command.Name.Length > 30)
        {
            return Result<CreateTagResponse>.Failure("Tag name must be 30 characters or less");
        }

        // Знаходимо тег
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == command.TagId && t.UserId == command.UserId);

        if (tag == null)
        {
            return Result<CreateTagResponse>.Failure("Tag not found");
        }

        if (!tag.IsDeletable)
        {
            return Result<CreateTagResponse>.Failure("Cannot rename system tag");
        }

        // Перевірка унікальності нової назви
        var exists = await _dbContext.Tags
            .AnyAsync(t => t.UserId == command.UserId && 
                          t.Name == command.Name && 
                          t.Id != command.TagId);

        if (exists)
        {
            return Result<CreateTagResponse>.Failure("Tag with this name already exists");
        }

        // Оновлення
        tag.Name = command.Name.Trim();
        await _dbContext.SaveChangesAsync();

        return Result<CreateTagResponse>.Success(new CreateTagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            IsDeletable = tag.IsDeletable
        });
    }
}

// ===== DELETE TAG =====

public class DeleteTagCommand
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
}

public class DeleteTagHandler
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteTagHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> HandleAsync(DeleteTagCommand command)
    {
        var tag = await _dbContext.Tags
            .Include(t => t.SiteTags)
            .FirstOrDefaultAsync(t => t.Id == command.TagId && t.UserId == command.UserId);

        if (tag == null)
        {
            return Result<bool>.Failure("Tag not found");
        }

        if (!tag.IsDeletable)
        {
            return Result<bool>.Failure("Cannot delete system tag");
        }

        // Видаляємо всі зв'язки з сайтами
        _dbContext.SiteTags.RemoveRange(tag.SiteTags);
        
        // Видаляємо тег
        _dbContext.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}

// ===== GET TAGS =====

public class GetTagsQuery
{
    public Guid UserId { get; set; }
}

public class GetTagsHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetTagsHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<CreateTagResponse>>> HandleAsync(GetTagsQuery query)
    {
        var tags = await _dbContext.Tags
            .Where(t => t.UserId == query.UserId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new CreateTagResponse
            {
                Id = t.Id,
                Name = t.Name,
                IsDeletable = t.IsDeletable
            })
            .ToListAsync();

        return Result<List<CreateTagResponse>>.Success(tags);
    }
}
