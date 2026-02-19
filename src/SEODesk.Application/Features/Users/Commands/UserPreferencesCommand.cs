namespace SEODesk.Application.Features.Users.Commands;

public sealed record UserPreferencesCommand
{
    public List<string> SelectedMetrics { get; set; } = new();
    public string LastRangePreset { get; set; } = "last28days";
    public Guid? LastGroupId { get; set; }
    public Guid? LastTagId { get; set; }
}
