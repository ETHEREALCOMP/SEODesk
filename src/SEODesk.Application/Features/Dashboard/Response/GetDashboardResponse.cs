using SEODesk.Application.Common;
using SEODesk.Application.Features.Dashboard.Commands;

namespace SEODesk.Application.Features.Dashboard.Response;

public sealed record GetDashboardResponse
{
    public MetricDto Summary { get; set; } = new();
    public List<TimeSeriesPointDto> TimeSeries { get; set; } = new();
    public List<SiteCommand> Sites { get; set; } = new();
    public int TotalUserSites { get; set; }
}
