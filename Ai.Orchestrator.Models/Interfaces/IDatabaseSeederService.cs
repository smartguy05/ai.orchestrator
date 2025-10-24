namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service interface for database initialization and seeding
/// </summary>
public interface IDatabaseSeederService
{
    /// <summary>
    /// Seeds the database with initial data
    /// Creates admin user from configuration if not exists
    /// Idempotent - can be called multiple times safely
    /// </summary>
    Task SeedAsync();

    /// <summary>
    /// Checks if the admin user exists
    /// </summary>
    /// <returns>True if admin user exists, false otherwise</returns>
    Task<bool> AdminUserExistsAsync();
}
