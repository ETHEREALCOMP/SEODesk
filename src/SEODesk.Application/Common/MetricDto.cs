namespace SEODesk.Application.Common;

public sealed record MetricDto
{
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
    public int KeywordsCount { get; set; }
}
