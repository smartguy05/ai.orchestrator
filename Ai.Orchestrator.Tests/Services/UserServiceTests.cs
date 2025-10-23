using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Users;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Tests.Services;

/// <summary>
/// TDD Tests for UserService
/// Tests user CRUD operations, authentication, and authorization
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly OrchestratorDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);

        // Seed roles for testing
        SeedRoles();

        _userService = new UserService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedRoles()
    {
        if (!_context.Roles.Any())
        {
            _context.Roles.AddRange(
                new Role { Id = 1, Name = Roles.Admin, Description = "Administrator" },
                new Role { Id = 2, Name = Roles.AgentManager, Description = "Agent Manager" },
                new Role { Id = 3, Name = Roles.User, Description = "User" },
                new Role { Id = 4, Name = Roles.ReadOnly, Description = "Read Only" }
            );
            _context.SaveChanges();
        }
    }

    #region User Registration Tests

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateUser_WithHashedPassword()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecureP@ssw0rd123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("newuser", result.Username);
        Assert.Equal("newuser@example.com", result.Email);
        Assert.True(result.IsActive);

        // Verify user was added to database
        var userInDb = await _context.Users.FindAsync(result.Id);
        Assert.NotNull(userInDb);
        Assert.NotNull(userInDb.PasswordHash);
        Assert.NotEqual("SecureP@ssw0rd123", userInDb.PasswordHash);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldAssignUserRole_ByDefault()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecureP@ssw0rd123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.Single(result.Roles);
        Assert.Contains("User", result.Roles);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldThrowException_ForDuplicateUsername()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Username = "duplicateuser",
            Email = "user1@example.com",
            Password = "Password123"
        };

        var request2 = new RegisterRequest
        {
            Username = "duplicateuser",
            Email = "user2@example.com",
            Password = "Password456"
        };

        // Act
        await _userService.RegisterUserAsync(request1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.RegisterUserAsync(request2));
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldThrowException_ForDuplicateEmail()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Username = "user1",
            Email = "duplicate@example.com",
            Password = "Password123"
        };

        var request2 = new RegisterRequest
        {
            Username = "user2",
            Email = "duplicate@example.com",
            Password = "Password456"
        };

        // Act
        await _userService.RegisterUserAsync(request1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.RegisterUserAsync(request2));
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldThrowException_ForInvalidEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "user",
            Email = "invalid-email",
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _userService.RegisterUserAsync(request));
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_ForValidCredentials()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecureP@ssw0rd123"
        };
        await _userService.RegisterUserAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "SecureP@ssw0rd123"
        };

        // Act
        var result = await _userService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_ForInvalidPassword()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecureP@ssw0rd123"
        };
        await _userService.RegisterUserAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        // Act
        var result = await _userService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_ForNonExistentUser()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent",
            Password = "Password123"
        };

        // Act
        var result = await _userService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_ForInactiveUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "inactiveuser",
            Email = "inactive@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Username = "inactiveuser",
            Password = "Password123"
        };

        // Act
        var result = await _userService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Get User Tests

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };
        var createdUser = await _userService.RegisterUserAsync(registerRequest);

        // Act
        var result = await _userService.GetUserByIdAsync(createdUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdUser.Id, result.Id);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.GetUserByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };
        await _userService.RegisterUserAsync(registerRequest);

        // Act
        var result = await _userService.GetUserByUsernameAsync("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _userService.GetUserByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };
        await _userService.RegisterUserAsync(registerRequest);

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllActiveUsers()
    {
        // Arrange
        await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "user1",
            Email = "user1@example.com",
            Password = "Password123"
        });

        await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "user2",
            Email = "user2@example.com",
            Password = "Password123"
        });

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldExcludeInactiveUsers()
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
        inactiveUser.SetPassword("Password123");

        _context.Users.AddRange(activeUser, inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("activeuser", result.First().Username);
    }

    #endregion

    #region Update User Tests

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateEmail()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "old@example.com",
            Password = "Password123"
        });

        // Act
        var updated = await _userService.UpdateUserEmailAsync(user.Id, "new@example.com");

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("new@example.com", updated.Email);

        var userInDb = await _context.Users.FindAsync(user.Id);
        Assert.Equal("new@example.com", userInDb.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldThrowException_ForDuplicateEmail()
    {
        // Arrange
        await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "user1",
            Email = "user1@example.com",
            Password = "Password123"
        });

        var user2 = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "user2",
            Email = "user2@example.com",
            Password = "Password123"
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.UpdateUserEmailAsync(user2.Id, "user1@example.com"));
    }

    [Fact]
    public async Task DeactivateUserAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        await _userService.DeactivateUserAsync(user.Id);

        // Assert
        var userInDb = await _context.Users.FindAsync(user.Id);
        Assert.False(userInDb.IsActive);
    }

    [Fact]
    public async Task ActivateUserAsync_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.SetPassword("Password123");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.ActivateUserAsync(user.Id);

        // Assert
        var userInDb = await _context.Users.FindAsync(user.Id);
        Assert.True(userInDb.IsActive);
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task ResetPasswordAsync_ShouldUpdatePassword()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "OldPassword123"
        });

        var resetRequest = new ResetPasswordRequest
        {
            UserId = user.Id,
            NewPassword = "NewPassword456"
        };

        // Act
        await _userService.ResetPasswordAsync(resetRequest);

        // Assert - Try to authenticate with new password
        var loginResult = await _userService.AuthenticateAsync(new LoginRequest
        {
            Username = "testuser",
            Password = "NewPassword456"
        });

        Assert.NotNull(loginResult);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldFailWithOldPassword()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "OldPassword123"
        });

        var resetRequest = new ResetPasswordRequest
        {
            UserId = user.Id,
            NewPassword = "NewPassword456"
        };

        await _userService.ResetPasswordAsync(resetRequest);

        // Act - Try to authenticate with old password
        var loginResult = await _userService.AuthenticateAsync(new LoginRequest
        {
            Username = "testuser",
            Password = "OldPassword123"
        });

        // Assert
        Assert.Null(loginResult);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrowException_ForNonExistentUser()
    {
        // Arrange
        var resetRequest = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid(),
            NewPassword = "NewPassword456"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.ResetPasswordAsync(resetRequest));
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task AssignRoleAsync_ShouldAddRoleToUser()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        await _userService.AssignRoleAsync(user.Id, Roles.Admin);

        // Assert
        var userWithRoles = await _userService.GetUserByIdAsync(user.Id);
        Assert.Contains("Admin", userWithRoles.Roles);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldNotDuplicateRole()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        await _userService.AssignRoleAsync(user.Id, Roles.User);
        await _userService.AssignRoleAsync(user.Id, Roles.User);

        // Assert
        var userWithRoles = await _userService.GetUserByIdAsync(user.Id);
        Assert.Single(userWithRoles.Roles.Where(r => r == "User"));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldRemoveRoleFromUser()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        await _userService.AssignRoleAsync(user.Id, Roles.Admin);

        // Act
        await _userService.RemoveRoleAsync(user.Id, Roles.Admin);

        // Assert
        var userWithRoles = await _userService.GetUserByIdAsync(user.Id);
        Assert.DoesNotContain("Admin", userWithRoles.Roles);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnAllUserRoles()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        await _userService.AssignRoleAsync(user.Id, Roles.Admin);
        await _userService.AssignRoleAsync(user.Id, Roles.AgentManager);

        // Act
        var roles = await _userService.GetUserRolesAsync(user.Id);

        // Assert
        Assert.Contains("User", roles);
        Assert.Contains("Admin", roles);
        Assert.Contains("AgentManager", roles);
        Assert.Equal(3, roles.Count);
    }

    #endregion

    #region Delete User Tests

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        await _userService.DeleteUserAsync(user.Id);

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowException_ForNonExistentUser()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.DeleteUserAsync(nonExistentId));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task UserExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var user = await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        var exists = await _userService.UserExistsAsync(user.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task UserExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _userService.UserExistsAsync(nonExistentId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task IsEmailAvailableAsync_ShouldReturnTrue_WhenEmailNotUsed()
    {
        // Act
        var available = await _userService.IsEmailAvailableAsync("available@example.com");

        // Assert
        Assert.True(available);
    }

    [Fact]
    public async Task IsEmailAvailableAsync_ShouldReturnFalse_WhenEmailUsed()
    {
        // Arrange
        await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "taken@example.com",
            Password = "Password123"
        });

        // Act
        var available = await _userService.IsEmailAvailableAsync("taken@example.com");

        // Assert
        Assert.False(available);
    }

    [Fact]
    public async Task IsUsernameAvailableAsync_ShouldReturnTrue_WhenUsernameNotUsed()
    {
        // Act
        var available = await _userService.IsUsernameAvailableAsync("availableuser");

        // Assert
        Assert.True(available);
    }

    [Fact]
    public async Task IsUsernameAvailableAsync_ShouldReturnFalse_WhenUsernameUsed()
    {
        // Arrange
        await _userService.RegisterUserAsync(new RegisterRequest
        {
            Username = "takenuser",
            Email = "test@example.com",
            Password = "Password123"
        });

        // Act
        var available = await _userService.IsUsernameAvailableAsync("takenuser");

        // Assert
        Assert.False(available);
    }

    #endregion
}
