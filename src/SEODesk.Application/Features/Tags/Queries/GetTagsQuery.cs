namespace SEODesk.Application.Features.Tags.Queries;

public sealed record GetTagsQuery
{
    public Guid UserId { get; set; }
}
