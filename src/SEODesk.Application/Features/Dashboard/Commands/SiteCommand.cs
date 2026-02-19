using SEODesk.Application.Common;

namespace SEODesk.Application.Features.Dashboard.Commands;

public sealed record SiteCommand
{
    public Guid Id { get; set; }
    public string PropertyId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public MetricDto Totals { get; set; } = new();
    public List<TimeSeriesPointDto> TimeSeries { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
    public DateTime? LastSynced { get; set; }
    public string? SyncError { get; set; }
}
