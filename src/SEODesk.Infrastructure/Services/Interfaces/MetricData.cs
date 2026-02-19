namespace SEODesk.Infrastructure.Services.Interfaces;

public class MetricData
{
    public DateOnly Date { get; set; }
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
}