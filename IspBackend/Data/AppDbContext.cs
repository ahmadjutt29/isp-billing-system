using IspBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace IspBackend.Data;

/// <summary>
/// Application database context for Entity Framework Core.
/// This class serves as the main entry point for database operations.
/// </summary>
/// <remarks>
/// Best Practices:
/// - Use dependency injection to resolve DbContext instances (configured in Program.cs)
/// - Keep DbContext instances short-lived (scoped lifetime is default and recommended)
/// - Use DbSet properties to define entity collections for querying and saving
/// - Override OnModelCreating to configure entity mappings using Fluent API
/// - Consider using IEntityTypeConfiguration for complex entity configurations
/// </remarks>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options configured in dependency injection.</param>
    /// <remarks>
    /// The options parameter is injected by the DI container and includes:
    /// - Database provider configuration (MySQL via Pomelo)
    /// - Connection string settings
    /// - Retry policies and other provider-specific options
    /// </remarks>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSet Properties
    /// <summary>
    /// Gets or sets the Users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the Fees table.
    /// </summary>
    public DbSet<Fee> Fees => Set<Fee>();
    #endregion

    /// <summary>
    /// Configures the model using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    /// <remarks>
    /// Use this method to:
    /// - Configure entity relationships (one-to-many, many-to-many)
    /// - Set up indexes and constraints
    /// - Configure value conversions
    /// - Apply global query filters (e.g., soft delete)
    /// - Seed initial data
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Configure Fee entity and relationship with User
        modelBuilder.Entity<Fee>(entity =>
        {
            // One-to-many: User has many Fees
            entity.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete fees when user is deleted

            // Index for faster queries by UserId
            entity.HasIndex(f => f.UserId);

            // Index for querying unpaid fees
            entity.HasIndex(f => new { f.UserId, f.Paid });
        });

        // Apply all entity configurations from the current assembly
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
