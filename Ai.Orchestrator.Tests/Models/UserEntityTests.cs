using Ai.Orchestrator.Models.Entities;
using BCrypt.Net;

namespace Ai.Orchestrator.Tests.Models;

/// <summary>
/// TDD Tests for User entity
/// These tests define the expected behavior of the User entity BEFORE implementation
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void User_ShouldCreateInstanceWithValidProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("test@example.com", user.Email);
        Assert.True(user.IsActive);
        Assert.NotEqual(default(DateTime), user.CreatedAt);
        Assert.NotEqual(default(DateTime), user.UpdatedAt);
    }

    [Fact]
    public void User_SetPassword_ShouldHashPasswordWithBCrypt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };
        var plainPassword = "SecureP@ssw0rd123";

        // Act
        user.SetPassword(plainPassword);

        // Assert
        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual(plainPassword, user.PasswordHash);
        Assert.True(user.PasswordHash.StartsWith("$2"));  // BCrypt hash prefix
        Assert.True(user.PasswordHash.Length >= 60); // BCrypt hash length
    }

    [Fact]
    public void User_VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };
        var plainPassword = "SecureP@ssw0rd123";
        user.SetPassword(plainPassword);

        // Act
        var result = user.VerifyPassword(plainPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void User_VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };
        user.SetPassword("CorrectPassword123");

        // Act
        var result = user.VerifyPassword("WrongPassword456");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void User_VerifyPassword_ShouldReturnFalseWhenPasswordHashIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = null
        };

        // Act
        var result = user.VerifyPassword("AnyPassword");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void User_SetPassword_ShouldThrowExceptionForNullOrEmptyPassword()
    {
        // Arrange
        var user = new User();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.SetPassword(null));
        Assert.Throws<ArgumentException>(() => user.SetPassword(""));
        Assert.Throws<ArgumentException>(() => user.SetPassword("   "));
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("12345")]
    [InlineData("short")]
    public void User_SetPassword_ShouldThrowExceptionForWeakPassword(string weakPassword)
    {
        // Arrange
        var user = new User();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => user.SetPassword(weakPassword));
    }

    [Fact]
    public void User_DefaultConstructor_ShouldInitializeCollections()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.NotNull(user.UserRoles);
        Assert.Empty(user.UserRoles);
        Assert.NotNull(user.Agents);
        Assert.Empty(user.Agents);
    }

    [Fact]
    public void User_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_ShouldSupportNavigationToAgents()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = user.Id,
            Owner = user
        };

        // Act
        user.Agents.Add(agent);

        // Assert
        Assert.Single(user.Agents);
        Assert.Equal(agent.Id, user.Agents.First().Id);
        Assert.Equal(user.Id, agent.OwnerId);
    }

    [Fact]
    public void User_ShouldSupportNavigationToUserRoles()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        };

        // Act
        user.UserRoles.Add(userRole);

        // Assert
        Assert.Single(user.UserRoles);
        Assert.Equal(role.Id, user.UserRoles.First().RoleId);
    }

    [Fact]
    public void User_CreatedBy_ShouldSupportSelfReferentialRelationship()
    {
        // Arrange
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com"
        };

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "newuser",
            Email = "newuser@example.com",
            CreatedById = adminUser.Id,
            CreatedBy = adminUser
        };

        // Assert
        Assert.Equal(adminUser.Id, newUser.CreatedById);
        Assert.Equal(adminUser.Username, newUser.CreatedBy.Username);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("first.last+tag@example.org")]
    public void User_Email_ShouldAcceptValidEmailFormats(string validEmail)
    {
        // Arrange & Act
        var user = new User
        {
            Email = validEmail
        };

        // Assert
        Assert.Equal(validEmail, user.Email);
    }

    [Theory]
    [InlineData("user123")]
    [InlineData("test_user")]
    [InlineData("user-name")]
    [InlineData("TestUser")]
    public void User_Username_ShouldAcceptValidUsernameFormats(string validUsername)
    {
        // Arrange & Act
        var user = new User
        {
            Username = validUsername
        };

        // Assert
        Assert.Equal(validUsername, user.Username);
    }

    [Fact]
    public void User_UpdatedAt_ShouldBeUpdatableIndependently()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var originalUpdatedAt = user.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Ensure time difference

        // Act
        user.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.True(user.UpdatedAt > originalUpdatedAt);
        Assert.True(user.UpdatedAt > user.CreatedAt);
    }
}
