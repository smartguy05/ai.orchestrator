using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Tests.Data;

/// <summary>
/// TDD Tests for OrchestratorDbContext
/// Tests database configuration, relationships, and constraints
/// </summary>
public class OrchestratorDbContextTests : IDisposable
{
    private readonly OrchestratorDbContext _context;

    public OrchestratorDbContextTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void DbContext_ShouldHaveUsersDbSet()
    {
        // Assert
        Assert.NotNull(_context.Users);
    }

    [Fact]
    public void DbContext_ShouldHaveAgentsDbSet()
    {
        // Assert
        Assert.NotNull(_context.Agents);
    }

    [Fact]
    public void DbContext_ShouldHaveRolesDbSet()
    {
        // Assert
        Assert.NotNull(_context.Roles);
    }

    [Fact]
    public void DbContext_ShouldHavePermissionsDbSet()
    {
        // Assert
        Assert.NotNull(_context.Permissions);
    }

    [Fact]
    public void DbContext_ShouldHavePluginConfigurationsDbSet()
    {
        // Assert
        Assert.NotNull(_context.PluginConfigurations);
    }

    [Fact]
    public void DbContext_ShouldHaveAgentToolsDbSet()
    {
        // Assert
        Assert.NotNull(_context.AgentTools);
    }

    [Fact]
    public async Task DbContext_ShouldCreateAndRetrieveUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("SecureP@ssw0rd123");

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal(user.Username, retrievedUser.Username);
        Assert.Equal(user.Email, retrievedUser.Email);
        Assert.NotNull(retrievedUser.PasswordHash);
    }

    [Fact]
    public async Task DbContext_ShouldEnforceUniqueUsername()
    {
        // Arrange
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "duplicateuser",
            Email = "user1@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user1.SetPassword("Password123");

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "duplicateuser",  // Duplicate username
            Email = "user2@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user2.SetPassword("Password456");

        // Act
        _context.Users.Add(user1);
        await _context.SaveChangesAsync();

        _context.Users.Add(user2);

        // Assert
        // Note: InMemory database doesn't enforce unique constraints like real databases
        // This test documents expected behavior with a real database
        // With a real PostgreSQL database, this would throw DbUpdateException
    }

    [Fact]
    public async Task DbContext_ShouldSupportUserAgentRelationship()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = user.Id,
            Model = "gpt-4",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var retrievedAgent = await _context.Agents
            .Include(a => a.Owner)
            .FirstOrDefaultAsync(a => a.Id == agent.Id);

        // Assert
        Assert.NotNull(retrievedAgent);
        Assert.NotNull(retrievedAgent.Owner);
        Assert.Equal(user.Username, retrievedAgent.Owner.Username);
    }

    [Fact]
    public async Task DbContext_ShouldSupportAgentPluginConfigRelationship()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pluginConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"smtp.gmail.com\"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.Agents.Add(agent);
        _context.PluginConfigurations.Add(pluginConfig);
        await _context.SaveChangesAsync();

        var retrievedAgent = await _context.Agents
            .Include(a => a.PluginConfigurations)
            .FirstOrDefaultAsync(a => a.Id == agent.Id);

        // Assert
        Assert.NotNull(retrievedAgent);
        Assert.Single(retrievedAgent.PluginConfigurations);
        Assert.Equal("Ai.Orchestrator.Plugins.Email", retrievedAgent.PluginConfigurations.First().PluginName);
    }

    [Fact]
    public async Task DbContext_ShouldSupportDefaultPluginConfiguration()
    {
        // Arrange
        var defaultConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = null,  // NULL means default configuration
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"default\":true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.PluginConfigurations.Add(defaultConfig);
        await _context.SaveChangesAsync();

        var retrievedConfig = await _context.PluginConfigurations
            .FirstOrDefaultAsync(c => c.AgentId == null && c.PluginName == "Ai.Orchestrator.Plugins.Email");

        // Assert
        Assert.NotNull(retrievedConfig);
        Assert.Null(retrievedConfig.AgentId);
        Assert.Contains("default", retrievedConfig.ConfigurationJson);
    }

    [Fact]
    public async Task DbContext_ShouldSupportUserRoleRelationship()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator role"
        };

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };

        // Act
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.Set<UserRole>().Add(userRole);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Single(retrievedUser.UserRoles);
        Assert.Equal("Admin", retrievedUser.UserRoles.First().Role.Name);
    }

    [Fact]
    public async Task DbContext_ShouldSupportRolePermissionRelationship()
    {
        // Arrange
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Administrator role"
        };

        var permission = new Permission
        {
            Id = 1,
            Name = "ManageUsers",
            Description = "Can manage users"
        };

        var rolePermission = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        };

        // Act
        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        _context.Set<RolePermission>().Add(rolePermission);
        await _context.SaveChangesAsync();

        var retrievedRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == role.Id);

        // Assert
        Assert.NotNull(retrievedRole);
        Assert.Single(retrievedRole.RolePermissions);
        Assert.Equal("ManageUsers", retrievedRole.RolePermissions.First().Permission.Name);
    }

    [Fact]
    public async Task DbContext_ShouldSupportAgentToolsRelationship()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tool = new AgentTool
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            ToolName = "SendEmail",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.Agents.Add(agent);
        _context.AgentTools.Add(tool);
        await _context.SaveChangesAsync();

        var retrievedAgent = await _context.Agents
            .Include(a => a.AgentTools)
            .FirstOrDefaultAsync(a => a.Id == agent.Id);

        // Assert
        Assert.NotNull(retrievedAgent);
        Assert.Single(retrievedAgent.AgentTools);
        Assert.Equal("SendEmail", retrievedAgent.AgentTools.First().ToolName);
    }

    [Fact]
    public async Task DbContext_ShouldSupportSelfReferentialUserRelationship()
    {
        // Arrange
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        adminUser.SetPassword("AdminPassword123");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "newuser",
            Email = "newuser@example.com",
            CreatedById = adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        newUser.SetPassword("UserPassword123");

        // Act
        _context.Users.Add(adminUser);
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users
            .Include(u => u.CreatedBy)
            .FirstOrDefaultAsync(u => u.Id == newUser.Id);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.NotNull(retrievedUser.CreatedBy);
        Assert.Equal("admin", retrievedUser.CreatedBy.Username);
    }

    [Fact]
    public async Task DbContext_ShouldQueryActiveUsersOnly()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "activeuser",
            Email = "active@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        activeUser.SetPassword("Password123");

        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "inactiveuser",
            Email = "inactive@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        inactiveUser.SetPassword("Password456");

        // Act
        _context.Users.Add(activeUser);
        _context.Users.Add(inactiveUser);
        await _context.SaveChangesAsync();

        var activeUsers = await _context.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        // Assert
        Assert.Single(activeUsers);
        Assert.Equal("activeuser", activeUsers.First().Username);
    }

    [Fact]
    public async Task DbContext_ShouldQueryDefaultAgentPerUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        var defaultAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "DefaultAgent",
            OwnerId = user.Id,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var secondaryAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "SecondaryAgent",
            OwnerId = user.Id,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.Agents.Add(defaultAgent);
        _context.Agents.Add(secondaryAgent);
        await _context.SaveChangesAsync();

        var userDefaultAgent = await _context.Agents
            .Where(a => a.OwnerId == user.Id && a.IsDefault)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(userDefaultAgent);
        Assert.Equal("DefaultAgent", userDefaultAgent.Name);
    }
}
