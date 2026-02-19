namespace SEODesk.Application.Features.Dashboard.Queries;

public sealed record GetDashboardQuery
{
    public Guid UserId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? TagId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public DateOnly? CompareFrom { get; set; }
    public DateOnly? CompareTo { get; set; }
    public string SortBy { get; set; } = "clicks";
    public string SortDir { get; set; } = "desc";
}
