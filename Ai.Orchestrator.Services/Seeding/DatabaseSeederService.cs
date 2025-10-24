using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Services.Seeding;

/// <summary>
/// Service for database initialization and seeding
/// Creates admin user from environment configuration
/// </summary>
public class DatabaseSeederService : IDatabaseSeederService
{
    private readonly OrchestratorDbContext _context;
    private readonly IConfig _config;

    public DatabaseSeederService(OrchestratorDbContext context, IConfig config)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SeedAsync()
    {
        // Ensure roles exist (DbContext OnModelCreating should seed these, but double-check)
        await EnsureRolesExistAsync();

        // Create admin user if configured and not exists
        await SeedAdminUserAsync();
    }

    public async Task<bool> AdminUserExistsAsync()
    {
        var adminUsername = _config.AdminUsername;

        if (string.IsNullOrWhiteSpace(adminUsername))
        {
            return false;
        }

        return await _context.Users.AnyAsync(u => u.Username == adminUsername);
    }

    private async Task SeedAdminUserAsync()
    {
        var adminUsername = _config.AdminUsername;
        var adminPassword = _config.AdminPassword;

        // Skip if admin credentials not configured
        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        // Check if admin user already exists
        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == adminUsername);

        if (existingAdmin != null)
        {
            // Admin user already exists, skip creation
            return;
        }

        // Create admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = adminUsername,
            Email = $"{adminUsername}@orchestrator.local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        adminUser.SetPassword(adminPassword);

        _context.Users.Add(adminUser);

        // Assign Admin role
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.Admin);

        if (adminRole != null)
        {
            _context.Set<UserRole>().Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task EnsureRolesExistAsync()
    {
        // Check if roles are already seeded
        var rolesExist = await _context.Roles.AnyAsync();

        if (rolesExist)
        {
            // Roles already seeded by DbContext or previous run
            return;
        }

        // If roles don't exist, they should be created by DbContext.OnModelCreating
        // This is a fallback in case they're not there
        // In production, migrations should handle this

        // Note: The roles are seeded in OrchestratorDbContext.OnModelCreating
        // This method is just a safety check
    }
}
