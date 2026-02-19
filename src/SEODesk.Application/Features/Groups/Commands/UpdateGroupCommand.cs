namespace SEODesk.Application.Features.Groups.Commands;

public sealed record UpdateGroupCommand
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
