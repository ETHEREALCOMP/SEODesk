namespace SEODesk.Application.Features.Groups.Commands;

public sealed record GroupCommand
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EmailOwner { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
