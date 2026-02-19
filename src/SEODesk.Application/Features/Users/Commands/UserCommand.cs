namespace SEODesk.Application.Features.Users.Commands;

public sealed record UserCommand
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Plan { get; set; } = string.Empty;
}
