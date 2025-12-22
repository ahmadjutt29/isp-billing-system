using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IspBackend.Data;

/// <summary>
/// Design-time factory for creating AppDbContext instances.
/// Used by EF Core tools for migrations when the application isn't running.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Use a default connection string for design-time operations
        var connectionString = "Server=localhost;Port=3306;Database=ispdb;User=root;Password=root;";
        
        // Use a specific MySQL version to avoid needing to connect during design time
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
        
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
