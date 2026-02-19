using SEODesk.Application.Common;
using SEODesk.Application.Features.Users.Queries;
using SEODesk.Application.Features.Users.Response;
using SEODesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SEODesk.Application.Features.Users.Commands;
using SEODesk.Domain.Enums;

namespace SEODesk.Application.Features.Users.Handlers;

public sealed class GetUserInfoHandler(ApplicationDbContext _dbContext)
{
    public async Task<Result<UserInfoResponse>> HandleAsync(GetUserInfoQuery query)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == query.UserId);

        if (user == null)
        {
            return Result<UserInfoResponse>.Failure("User not found");
        }

        var response = new UserInfoResponse
        {
            User = new UserCommand
            {
                Email = user.Email,
                Name = user.Name,
                Avatar = user.Avatar,
                Plan = user.Plan.ToString()
            },
            Promotions = GetActivePromotions(user.Plan)
        };

        return Result<UserInfoResponse>.Success(response);
    }

    private List<string> GetActivePromotions(PlanType plan)
    {
        var promotions = new List<string>();

        // Якщо TRIAL - показуємо знижку на річний план
        if (plan == PlanType.TRIAL)
        {
            promotions.Add("-20% annual");
        }

        return promotions;
    }
}
