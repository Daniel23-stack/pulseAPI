using Microsoft.EntityFrameworkCore;
using PulseAPI.Core.Entities;

namespace PulseAPI.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Api> Apis { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<HealthCheck> HealthChecks { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AlertHistory> AlertHistories { get; set; }
    public DbSet<ApiCollection> ApiCollections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
        });

        // Api configuration
        modelBuilder.Entity<Api>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(10);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // HealthCheck configuration
        modelBuilder.Entity<HealthCheck>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CheckedAt);
            entity.HasIndex(e => e.ApiId);
            entity.HasOne(e => e.Api)
                .WithMany(a => a.HealthChecks)
                .HasForeignKey(e => e.ApiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Alert configuration
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Api)
                .WithMany()
                .HasForeignKey(e => e.ApiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Collection)
                .WithMany()
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AlertHistory configuration
        modelBuilder.Entity<AlertHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FiredAt);
            entity.HasOne(e => e.Alert)
                .WithMany(a => a.AlertHistories)
                .HasForeignKey(e => e.AlertId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiCollection configuration (many-to-many)
        modelBuilder.Entity<ApiCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApiId, e.CollectionId }).IsUnique();
            entity.HasOne(e => e.Api)
                .WithMany(a => a.ApiCollections)
                .HasForeignKey(e => e.ApiId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Collection)
                .WithMany(c => c.ApiCollections)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}



