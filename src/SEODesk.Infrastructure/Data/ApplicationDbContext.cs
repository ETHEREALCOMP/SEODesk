using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SEODesk.Domain.Entities;

namespace SEODesk.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteTag> SiteTags => Set<SiteTag>();
    public DbSet<SiteMetric> SiteMetrics => Set<SiteMetric>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.GoogleRefreshToken).IsRequired();
            entity.HasIndex(e => e.GoogleId).IsUnique();
            entity.Property(e => e.GoogleId).HasMaxLength(256).IsRequired();

            entity.HasMany(e => e.Groups)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Tags)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Sites)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Preferences)
                .WithOne(e => e.User)
                .HasForeignKey<UserPreference>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EmailOwner).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.DisplayName });
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
        });

        // Site configuration
        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Domain).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.PropertyId }).IsUnique();
            
            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SiteTag configuration (many-to-many)
        modelBuilder.Entity<SiteTag>(entity =>
        {
            entity.HasKey(e => new { e.SiteId, e.TagId });
            
            entity.HasOne(e => e.Site)
                .WithMany(e => e.SiteTags)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Tag)
                .WithMany(e => e.SiteTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SiteMetric configuration
        modelBuilder.Entity<SiteMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SiteId, e.Date }).IsUnique();
            
            entity.HasOne(e => e.Site)
                .WithMany(e => e.Metrics)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPreference configuration
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.SelectedMetrics).HasMaxLength(200);
            entity.Property(e => e.LastRangePreset).HasMaxLength(50);
        });

        // Seed default "All" tag for each user (handled in application layer)
    }
}
