namespace SEODesk.Application.Features.Users.Commands;

public sealed record UpdateUserPreferencesCommand
{
    public Guid UserId { get; set; }
    public List<string>? SelectedMetrics { get; set; }
    public string? LastRangePreset { get; set; }
    public Guid? LastGroupId { get; set; }
    public Guid? LastTagId { get; set; }
}
