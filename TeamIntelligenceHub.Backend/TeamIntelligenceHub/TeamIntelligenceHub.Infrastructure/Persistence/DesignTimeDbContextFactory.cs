using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TeamIntelligenceHub.Infrastructure.Persistence;

/// <summary>
/// Builds the context for `dotnet ef` / Package Manager Console. It reads only the
/// connection string, so migrations do not depend on the API's Entra settings.
/// </summary>
public class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<TeamIntelligenceHubDbContext>
{
    /// <summary>
    /// Creates a context for the design-time tools, reading the connection string from the API
    /// project's configuration, user secrets, and environment variables.
    /// </summary>
    /// <param name="args">The arguments passed by the design-time tools; not used.</param>
    /// <returns>A <see cref="TeamIntelligenceHubDbContext"/> connected to the configured SQL Server database.</returns>
    /// <exception cref="InvalidOperationException">Thrown when ConnectionStrings:DefaultConnection is not set.</exception>
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
