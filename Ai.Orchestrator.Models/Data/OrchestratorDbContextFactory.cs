using Ai.Orchestrator.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ai.Orchestrator.Models.Data;

/// <summary>
/// Design-time DbContext factory for EF Core migrations
/// This is used by migration tools to create DbContext instances
/// </summary>
public class OrchestratorDbContextFactory : IDesignTimeDbContextFactory<OrchestratorDbContext>
{
    public OrchestratorDbContext CreateDbContext(string[] args)
    {
        // Get connection string from environment variable or use default
        var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString")
            ?? "Host=localhost;Port=5432;Database=orchestrator;Username=orchestrator;Password=orchestrator_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<OrchestratorDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new OrchestratorDbContext(optionsBuilder.Options);
    }
}
