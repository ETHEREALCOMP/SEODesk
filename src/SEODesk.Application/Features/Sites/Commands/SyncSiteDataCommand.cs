namespace SEODesk.Application.Features.Sites.Commands;

public sealed record SyncSiteDataCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
