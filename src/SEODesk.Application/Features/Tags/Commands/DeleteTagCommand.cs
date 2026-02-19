namespace SEODesk.Application.Features.Tags.Commands;

public sealed record DeleteTagCommand
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
}
