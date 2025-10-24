using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Seeding;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ai.Orchestrator.Tests.Services;

/// <summary>
/// TDD Tests for DatabaseSeederService
/// Tests admin user seeding and database initialization
/// </summary>
public class DatabaseSeederServiceTests : IDisposable
{
    private readonly OrchestratorDbContext _context;
    private readonly Mock<IConfig> _mockConfig;
    private readonly DatabaseSeederService _seederService;

    public DatabaseSeederServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);

        // Ensure database is created with seed data from OnModelCreating
        _context.Database.EnsureCreated();

        _mockConfig = new Mock<IConfig>();
        _mockConfig.Setup(c => c.AdminUsername).Returns("admin");
        _mockConfig.Setup(c => c.AdminPassword).Returns("Admin@123456");

        _seederService = new DatabaseSeederService(_context, _mockConfig.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Admin User Seeding Tests

    [Fact]
    public async Task SeedAsync_ShouldCreateAdminUser_WhenNotExists()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(adminUser);
        Assert.Equal("admin", adminUser.Username);
        Assert.True(adminUser.IsActive);
    }

    [Fact]
    public async Task SeedAsync_ShouldHashAdminPassword()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(adminUser.PasswordHash);
        Assert.NotEqual("Admin@123456", adminUser.PasswordHash);
        Assert.True(adminUser.PasswordHash.StartsWith("$2")); // BCrypt hash
    }

    [Fact]
    public async Task SeedAsync_ShouldAssignAdminRole_ToAdminUser()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == "admin");

        Assert.NotNull(adminUser);
        Assert.NotEmpty(adminUser.UserRoles);
        Assert.Contains(adminUser.UserRoles, ur => ur.Role.Name == Roles.Admin);
    }

    [Fact]
    public async Task SeedAsync_ShouldNotCreateDuplicateAdminUser()
    {
        // Act
        await _seederService.SeedAsync();
        await _seederService.SeedAsync(); // Call twice

        // Assert
        var adminUsers = await _context.Users.Where(u => u.Username == "admin").ToListAsync();
        Assert.Single(adminUsers);
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        // Act - Call multiple times
        await _seederService.SeedAsync();
        await _seederService.SeedAsync();
        await _seederService.SeedAsync();

        // Assert - Should only have one admin user
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldUseConfiguredAdminUsername()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminUsername).Returns("customadmin");

        var customSeeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await customSeeder.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "customadmin");
        Assert.NotNull(adminUser);
    }

    [Fact]
    public async Task SeedAsync_ShouldSetAdminEmail()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(adminUser.Email);
        Assert.Contains("admin", adminUser.Email);
    }

    [Fact]
    public async Task SeedAsync_ShouldSkip_WhenAdminUsernameNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminUsername).Returns((string)null);

        var seeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldSkip_WhenAdminPasswordNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminPassword).Returns((string)null);

        var seeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateActiveUser()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.True(adminUser.IsActive);
    }

    [Fact]
    public async Task SeedAsync_ShouldSetCreatedAndUpdatedTimes()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotEqual(default(DateTime), adminUser.CreatedAt);
        Assert.NotEqual(default(DateTime), adminUser.UpdatedAt);
    }

    #endregion

    #region Role Seeding Tests

    [Fact]
    public async Task SeedAsync_ShouldEnsureAdminRoleExists()
    {
        // Act
        await _seederService.SeedAsync();

        // Assert
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Admin);
        Assert.NotNull(adminRole);
    }

    [Fact]
    public async Task SeedAsync_ShouldNotDuplicateRoles()
    {
        // Act
        await _seederService.SeedAsync();
        await _seederService.SeedAsync();

        // Assert
        var adminRoles = await _context.Roles.Where(r => r.Name == Roles.Admin).ToListAsync();
        Assert.Single(adminRoles);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task SeedAsync_ShouldNotThrowException_WhenCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        await _seederService.SeedAsync();
        await _seederService.SeedAsync();
        await _seederService.SeedAsync();
    }

    [Fact]
    public async Task SeedAsync_ShouldWorkWithExistingUsers()
    {
        // Arrange - Create a non-admin user first
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "existinguser",
            Email = "existing@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        existingUser.SetPassword("Password123");

        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        // Act
        await _seederService.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(2, userCount); // Existing + Admin
    }

    [Fact]
    public async Task AdminUserExists_ShouldReturnTrue_AfterSeeding()
    {
        // Act
        await _seederService.SeedAsync();
        var exists = await _seederService.AdminUserExistsAsync();

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AdminUserExists_ShouldReturnFalse_BeforeSeeding()
    {
        // Act
        var exists = await _seederService.AdminUserExistsAsync();

        // Assert
        Assert.False(exists);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SeedAsync_ShouldHandleEmptyAdminUsername()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminUsername).Returns("");

        var seeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldHandleEmptyAdminPassword()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminPassword).Returns("");

        var seeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldHandleWhitespaceAdminUsername()
    {
        // Arrange
        _mockConfig.Setup(c => c.AdminUsername).Returns("   ");

        var seeder = new DatabaseSeederService(_context, _mockConfig.Object);

        // Act
        await seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(0, userCount);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SeedAsync_AdminUser_ShouldBeAbleToLogin()
    {
        // Arrange
        await _seederService.SeedAsync();

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

        // Act
        var canLogin = adminUser.VerifyPassword("Admin@123456");

        // Assert
        Assert.True(canLogin);
    }

    [Fact]
    public async Task SeedAsync_AdminUser_ShouldNotLoginWithWrongPassword()
    {
        // Arrange
        await _seederService.SeedAsync();

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

        // Act
        var canLogin = adminUser.VerifyPassword("WrongPassword");

        // Assert
        Assert.False(canLogin);
    }

    #endregion
}
