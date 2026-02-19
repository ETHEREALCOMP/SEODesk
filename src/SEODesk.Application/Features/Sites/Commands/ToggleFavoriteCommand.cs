namespace SEODesk.Application.Features.Sites.Commands;

public sealed record ToggleFavoriteCommand
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public bool IsFavorite { get; set; }
}
