using Ai.Orchestrator.Controllers;
using Ai.Orchestrator.Models.DTOs.Users;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ai.Orchestrator.Tests.Controllers;

/// <summary>
/// TDD Tests for UserController
/// Tests user management endpoints with authorization
/// </summary>
public class UserControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserController _controller;
    private readonly Guid _currentUserId;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UserController(_mockUserService.Object);
        _currentUserId = Guid.NewGuid();

        // Setup controller context with authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WithUserList()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), Username = "user1", Email = "user1@example.com" },
            new UserDto { Id = Guid.NewGuid(), Username = "user2", Email = "user2@example.com" }
        };

        _mockUserService
            .Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsAssignableFrom<List<UserDto>>(okResult.Value);
        Assert.Equal(2, returnedUsers.Count);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnEmptyList_WhenNoUsers()
    {
        // Arrange
        _mockUserService
            .Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(new List<UserDto>());

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsAssignableFrom<List<UserDto>>(okResult.Value);
        Assert.Empty(returnedUsers);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        _mockUserService
            .Setup(s => s.GetAllUsersAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Roles = new List<string> { "User" }
        };

        _mockUserService
            .Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
        Assert.Equal("testuser", returnedUser.Username);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task GetUserById_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetUserByIdAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest
        {
            Email = "newemail@example.com",
            IsActive = true
        };

        var updatedUser = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "newemail@example.com",
            IsActive = true
        };

        _mockUserService
            .Setup(s => s.UpdateUserAsync(userId, request))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("newemail@example.com", returnedUser.Email);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest { Email = "new@example.com" };

        _mockUserService
            .Setup(s => s.UpdateUserAsync(userId, request))
            .ThrowsAsync(new InvalidOperationException($"User with ID '{userId}' not found"));

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequest_WhenEmailInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest { Email = "invalid-email" };

        _mockUserService
            .Setup(s => s.UpdateUserAsync(userId, request))
            .ThrowsAsync(new ArgumentException("Invalid email format"));

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid email", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequest_WhenDuplicateEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest { Email = "existing@example.com" };

        _mockUserService
            .Setup(s => s.UpdateUserAsync(userId, request))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest { Email = "new@example.com" };

        _mockUserService
            .Setup(s => s.UpdateUserAsync(userId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("deleted successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ThrowsAsync(new InvalidOperationException($"User with ID '{userId}' not found"));

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.DeleteUserAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region AddRoleToUser Tests

    [Fact]
    public async Task AddRoleToUser_ShouldReturnOk_WhenRoleAdded()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddRoleToUserAsync(userId, roleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddRoleToUser(userId, roleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("added successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task AddRoleToUser_ShouldReturnNotFound_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddRoleToUserAsync(userId, roleId))
            .ThrowsAsync(new InvalidOperationException($"User with ID '{userId}' not found"));

        // Act
        var result = await _controller.AddRoleToUser(userId, roleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task AddRoleToUser_ShouldReturnNotFound_WhenRoleNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddRoleToUserAsync(userId, roleId))
            .ThrowsAsync(new InvalidOperationException($"Role with ID '{roleId}' not found"));

        // Act
        var result = await _controller.AddRoleToUser(userId, roleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task AddRoleToUser_ShouldReturnBadRequest_WhenRoleAlreadyAssigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddRoleToUserAsync(userId, roleId))
            .ThrowsAsync(new InvalidOperationException("User already has this role"));

        // Act
        var result = await _controller.AddRoleToUser(userId, roleId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already has", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task AddRoleToUser_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.AddRoleToUserAsync(userId, roleId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AddRoleToUser(userId, roleId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region RemoveRoleFromUser Tests

    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnOk_WhenRoleRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.RemoveRoleFromUserAsync(userId, roleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveRoleFromUser(userId, roleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("removed successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnNotFound_WhenUserNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.RemoveRoleFromUserAsync(userId, roleId))
            .ThrowsAsync(new InvalidOperationException($"User with ID '{userId}' not found"));

        // Act
        var result = await _controller.RemoveRoleFromUser(userId, roleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnNotFound_WhenRoleNotAssigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.RemoveRoleFromUserAsync(userId, roleId))
            .ThrowsAsync(new InvalidOperationException("User does not have this role"));

        // Act
        var result = await _controller.RemoveRoleFromUser(userId, roleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("does not have", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.RemoveRoleFromUserAsync(userId, roleId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.RemoveRoleFromUser(userId, roleId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetUsersByRole Tests

    [Fact]
    public async Task GetUsersByRole_ShouldReturnOk_WithUserList()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), Username = "admin1", Roles = new List<string> { "Admin" } },
            new UserDto { Id = Guid.NewGuid(), Username = "admin2", Roles = new List<string> { "Admin" } }
        };

        _mockUserService
            .Setup(s => s.GetUsersByRoleAsync(roleId))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsersByRole(roleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsAssignableFrom<List<UserDto>>(okResult.Value);
        Assert.Equal(2, returnedUsers.Count);
    }

    [Fact]
    public async Task GetUsersByRole_ShouldReturnEmptyList_WhenNoUsersWithRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetUsersByRoleAsync(roleId))
            .ReturnsAsync(new List<UserDto>());

        // Act
        var result = await _controller.GetUsersByRole(roleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsAssignableFrom<List<UserDto>>(okResult.Value);
        Assert.Empty(returnedUsers);
    }

    [Fact]
    public async Task GetUsersByRole_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.GetUsersByRoleAsync(roleId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUsersByRole(roleId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion
}
