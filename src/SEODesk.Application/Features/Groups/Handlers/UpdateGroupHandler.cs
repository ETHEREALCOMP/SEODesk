using SEODesk.Application.Common;
using SEODesk.Application.Features.Groups.Commands;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SEODesk.Application.Features.Groups.Handlers
{
    public class UpdateGroupHandler(ApplicationDbContext _dbContext)
    {
        public async Task<Result<GroupCommand>> HandleAsync(UpdateGroupCommand command)
        {
            // Валідація
            if (string.IsNullOrWhiteSpace(command.DisplayName))
            {
                return Result<GroupCommand>.Failure("Group name is required");
            }

            if (command.DisplayName.Length > 40)
            {
                return Result<GroupCommand>.Failure("Group name must be 40 characters or less");
            }

            // Знаходимо групу
            var group = await _dbContext.Groups
                .FirstOrDefaultAsync(g => g.Id == command.GroupId && g.UserId == command.UserId);

            if (group == null)
            {
                return Result<GroupCommand>.Failure("Group not found");
            }

            if (group.IsDefault)
            {
                return Result<GroupCommand>.Failure("Cannot rename default group");
            }

            // Оновлення
            group.DisplayName = command.DisplayName.Trim();
            await _dbContext.SaveChangesAsync();

            return Result<GroupCommand>.Success(new GroupCommand
            {
                Id = group.Id,
                DisplayName = group.DisplayName,
                EmailOwner = group.EmailOwner,
                IsDefault = group.IsDefault
            });
        }
    }
}