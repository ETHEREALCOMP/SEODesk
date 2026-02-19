using SEODesk.Application.Common;
using SEODesk.Application.Features.Tags.Commands;
using SEODesk.Application.Features.Tags.Response;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace SEODesk.Application.Features.Tags.Handlers;

public sealed class UpdateTagHandler(ApplicationDbContext _dbContext)
{
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
