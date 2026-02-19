namespace SEODesk.Application.Features.Sites.Commands;

public sealed record DiscoverSitesCommand
{
    public Guid UserId { get; set; }
}
