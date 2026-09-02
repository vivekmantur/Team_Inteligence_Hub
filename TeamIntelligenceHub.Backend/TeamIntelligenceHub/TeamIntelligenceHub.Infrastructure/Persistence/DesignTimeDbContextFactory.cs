using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TeamIntelligenceHub.Infrastructure.Persistence;

/// <summary>
/// Builds the context for `dotnet ef` / Package Manager Console.
/// </summary>
/// <remarks>
/// Without this, EF constructs the context by running the API's Program.cs, which means
/// migrations fail whenever the Entra settings are absent — unrelated to the database.
/// This reads only what a migration actually needs: the connection string.
/// </remarks>
public class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<TeamIntelligenceHubDbContext>
{
    public TeamIntelligenceHubDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = Path.GetFullPath(
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "TeamIntelligenceHub.API"));

        var basePath = Directory.Exists(apiProjectPath)
            ? apiProjectPath
            : Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection was not found. Set it in " +
                "TeamIntelligenceHub.API/appsettings.json or with dotnet user-secrets.");
        }

        var options = new DbContextOptionsBuilder<TeamIntelligenceHubDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TeamIntelligenceHubDbContext(options);
    }
}
