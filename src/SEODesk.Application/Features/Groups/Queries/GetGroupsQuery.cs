namespace SEODesk.Application.Features.Groups.Queries;

public sealed record GetGroupsQuery
{
    public Guid UserId { get; set; }
}
