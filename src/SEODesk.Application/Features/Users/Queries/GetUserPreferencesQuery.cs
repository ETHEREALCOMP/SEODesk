namespace SEODesk.Application.Features.Users.Queries;

public sealed record GetUserPreferencesQuery
{
    public Guid UserId { get; set; }
}
