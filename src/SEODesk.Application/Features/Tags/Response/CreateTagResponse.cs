namespace SEODesk.Application.Features.Tags.Response;

public sealed record CreateTagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeletable { get; set; }
}