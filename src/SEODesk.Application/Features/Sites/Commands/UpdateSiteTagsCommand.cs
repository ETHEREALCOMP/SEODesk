namespace SEODesk.Application.Features.Sites.Commands;

public sealed record UpdateSiteTagsCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
}
