using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SEODesk.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // This helper class must NOT use a private _configuration field.
        // It needs to build its own configuration builder to access extension methods like SetBasePath.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Try to get connection string from env or config
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        string connectionString;

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = Npgsql.SslMode.Require,
                Pooling = true
            };
            connectionString = npgsqlBuilder.ConnectionString;
        }
        else
        {
            // Fallback for design time - use a dummy or read from appsettings if available
            connectionString = configuration.GetConnectionString("DefaultConnection") 
                             ?? "Host=localhost;Database=seodesk;Username=postgres;Password=postgres";
        }

        builder.UseNpgsql(connectionString);

        return new ApplicationDbContext(builder.Options);
    }
}
