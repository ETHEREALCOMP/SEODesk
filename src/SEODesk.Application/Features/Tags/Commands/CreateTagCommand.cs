namespace SEODesk.Application.Features.Tags.Commands;

public sealed record CreateTagCommand
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}
