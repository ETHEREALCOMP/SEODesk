namespace SEODesk.Application.Features.Users.Queries;

public sealed record GetUserInfoQuery
{
    public Guid UserId { get; set; }
}
