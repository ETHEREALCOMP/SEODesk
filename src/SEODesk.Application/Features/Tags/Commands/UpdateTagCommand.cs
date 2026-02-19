namespace SEODesk.Application.Features.Tags.Commands;

public sealed record UpdateTagCommand
{
    public Guid UserId { get; set; }
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
}
