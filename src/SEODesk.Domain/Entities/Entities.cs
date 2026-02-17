namespace SEODesk.Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EmailOwner { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

public class Tag
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeletable { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public List<SiteTag> SiteTags { get; set; } = new();
}

public class Site
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? GroupId { get; set; }
    public string PropertyId { get; set; } = string.Empty; // GSC property ID
    public string Domain { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? SyncError { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Group? Group { get; set; }
    public List<SiteTag> SiteTags { get; set; } = new();
    public List<SiteMetric> Metrics { get; set; } = new();
}

public class SiteTag
{
    public Guid SiteId { get; set; }
    public Guid TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Site Site { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public class SiteMetric
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public DateTime Date { get; set; }
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
    public int KeywordsCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Site Site { get; set; } = null!;
}

public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SelectedMetrics { get; set; } = "clicks,impressions"; // JSON array
    public string LastRangePreset { get; set; } = "last28days";
    public Guid? LastGroupId { get; set; }
    public Guid? LastTagId { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
