using Ai.Orchestrator.Controllers;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Users;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Ai.Orchestrator.Tests.Controllers;

/// <summary>
/// TDD Tests for AuthController
/// Tests authentication endpoints: register, login, reset password
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockAuditService = new Mock<IAuditService>();
        _controller = new AuthController(_mockUserService.Object, _mockJwtService.Object, _mockAuditService.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123"
        };

        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "newuser",
            Email = "newuser@example.com",
            IsActive = true,
            Roles = new List<string> { "User" }
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("newuser", response.Username);
        Assert.Equal("newuser@example.com", response.Email);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenDuplicateUsername()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "new@example.com",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new InvalidOperationException("Username 'existinguser' already exists"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenDuplicateEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new InvalidOperationException("Email 'existing@example.com' already exists"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenInvalidEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "invalid-email",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new ArgumentException("Invalid email format"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid email", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordTooShort()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "short"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new ArgumentException("Password must be at least 8 characters long"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("at least 8 characters", badRequestResult.Value.ToString());
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123"
        };

        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "testuser@example.com",
            IsActive = true,
            Roles = new List<string> { "User" }
        };

        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
        var expiresAt = DateTime.UtcNow.AddMinutes(1440);
        var jwtId = Guid.NewGuid().ToString();
        var refreshToken = "refresh_token_string";

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync(userDto);

        _mockJwtService
            .Setup(s => s.GenerateToken(userDto.Id, userDto.Username, userDto.Roles))
            .Returns(token);

        _mockJwtService
            .Setup(s => s.GetTokenExpiration(token))
            .Returns(expiresAt);

        _mockJwtService
            .Setup(s => s.GetJwtId(token))
            .Returns(jwtId);

        _mockJwtService
            .Setup(s => s.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtService
            .Setup(s => s.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Ai.Orchestrator.Models.Entities.RefreshToken { Id = Guid.NewGuid() });

        _mockAuditService
            .Setup(s => s.LogSecurityEventAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(token, response.Token);
        Assert.Equal("testuser", response.Username);
        Assert.Equal("testuser@example.com", response.Email);
        Assert.Contains("User", response.Roles);
        Assert.Equal(expiresAt, response.ExpiresAt);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("Invalid credentials", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("Invalid credentials", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserInactive()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "inactiveuser",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((UserDto)null); // UserService returns null for inactive users

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldGenerateJwtToken_WithUserRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LoginRequest
        {
            Username = "adminuser",
            Password = "Admin@123"
        };

        var userDto = new UserDto
        {
            Id = userId,
            Username = "adminuser",
            Email = "admin@example.com",
            IsActive = true,
            Roles = new List<string> { "Admin", "User" }
        };

        var token = "jwt.token.here";
        var expiresAt = DateTime.UtcNow.AddMinutes(1440);
        var jwtId = Guid.NewGuid().ToString();
        var refreshToken = "refresh_token_string";

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync(userDto);

        _mockJwtService
            .Setup(s => s.GenerateToken(userId, "adminuser", It.Is<List<string>>(roles =>
                roles.Contains("Admin") && roles.Contains("User"))))
            .Returns(token);

        _mockJwtService
            .Setup(s => s.GetTokenExpiration(token))
            .Returns(expiresAt);

        _mockJwtService
            .Setup(s => s.GetJwtId(token))
            .Returns(jwtId);

        _mockJwtService
            .Setup(s => s.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtService
            .Setup(s => s.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Ai.Orchestrator.Models.Entities.RefreshToken { Id = Guid.NewGuid() });

        _mockAuditService
            .Setup(s => s.LogSecurityEventAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(2, response.Roles.Count);
        Assert.Contains("Admin", response.Roles);
        Assert.Contains("User", response.Roles);
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_ShouldReturnOk_WhenResetSucceeds()
    {
        // Arrange
        SetupAuthenticatedUser("admin", Guid.NewGuid(), new List<string> { "Admin" });

        var userId = Guid.NewGuid();
        var request = new ResetPasswordRequest
        {
            UserId = userId,
            NewPassword = "NewPassword123"
        };

        _mockUserService
            .Setup(s => s.ResetPasswordAsync(request))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Password reset successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnNotFound_WhenUserNotExists()
    {
        // Arrange
        SetupAuthenticatedUser("admin", Guid.NewGuid(), new List<string> { "Admin" });

        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid(),
            NewPassword = "NewPassword123"
        };

        _mockUserService
            .Setup(s => s.ResetPasswordAsync(request))
            .ThrowsAsync(new InvalidOperationException($"User with ID '{request.UserId}' not found"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenPasswordInvalid()
    {
        // Arrange
        SetupAuthenticatedUser("admin", Guid.NewGuid(), new List<string> { "Admin" });

        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid(),
            NewPassword = "short"
        };

        _mockUserService
            .Setup(s => s.ResetPasswordAsync(request))
            .ThrowsAsync(new ArgumentException("Password must be at least 8 characters long"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("at least 8 characters", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task ResetPassword_ShouldCallUserService_WithCorrectParameters()
    {
        // Arrange
        SetupAuthenticatedUser("admin", Guid.NewGuid(), new List<string> { "Admin" });

        var userId = Guid.NewGuid();
        var request = new ResetPasswordRequest
        {
            UserId = userId,
            NewPassword = "NewPassword123"
        };

        _mockUserService
            .Setup(s => s.ResetPasswordAsync(It.Is<ResetPasswordRequest>(r =>
                r.UserId == userId && r.NewPassword == "NewPassword123")))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _controller.ResetPassword(request);

        // Assert
        _mockUserService.Verify();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameEmpty()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "",
            Email = "test@example.com",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new ArgumentException("Username cannot be empty"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUsernameEmpty()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordEmpty()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = ""
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Register_ShouldReturnInternalServerError_WhenUnexpectedExceptionThrown()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123"
        };

        _mockUserService
            .Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new Exception("Unexpected database error"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnInternalServerError_WhenJwtServiceFails()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123"
        };

        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "testuser@example.com",
            IsActive = true,
            Roles = new List<string> { "User" }
        };

        _mockUserService
            .Setup(s => s.AuthenticateAsync(request))
            .ReturnsAsync(userDto);

        _mockJwtService
            .Setup(s => s.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .Throws(new Exception("JWT generation failed"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnInternalServerError_WhenUnexpectedExceptionThrown()
    {
        // Arrange
        SetupAuthenticatedUser("admin", Guid.NewGuid(), new List<string> { "Admin" });

        var request = new ResetPasswordRequest
        {
            UserId = Guid.NewGuid(),
            NewPassword = "NewPassword123"
        };

        _mockUserService
            .Setup(s => s.ResetPasswordAsync(request))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string username, Guid userId, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
