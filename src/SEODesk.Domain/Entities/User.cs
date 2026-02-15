namespace SEODesk.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    // Google OAuth
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public string? Avatar { get; set; }

    public PlanType Plan { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public List<Group> Groups { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public List<Site> Sites { get; set; } = new();
    public UserPreference? Preferences { get; set; }
}

public enum PlanType
{
    FREE = 0,
    TRIAL = 1,
    PRO = 2
}