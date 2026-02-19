namespace SEODesk.Application.Features.Sites.Queries;

public sealed record ExportSiteDataQuery
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
}
