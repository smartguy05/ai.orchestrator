using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Authentication;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ai.Orchestrator.Tests.Services;

/// <summary>
/// TDD Tests for JwtService
/// Tests JWT token generation, validation, and claims management
/// </summary>
public class JwtServiceTests
{
    private readonly Mock<IConfig> _mockConfig;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _mockConfig = new Mock<IConfig>();
        _mockConfig.Setup(c => c.JwtSecret).Returns("this-is-a-very-secure-secret-key-that-is-at-least-32-characters-long");
        _mockConfig.Setup(c => c.JwtIssuer).Returns("ai.orchestrator");
        _mockConfig.Setup(c => c.JwtAudience).Returns("ai.orchestrator.api");
        _mockConfig.Setup(c => c.JwtExpirationMinutes).Returns(60);

        _jwtService = new JwtService(_mockConfig.Object);
    }

    [Fact]
    public void GenerateToken_ShouldCreateValidToken_WithUserIdAndUsername()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUserIdClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var claims = DecodeToken(token);

        // Assert
        var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal(userId.ToString(), userIdClaim.Value);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUsernameClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var claims = DecodeToken(token);

        // Assert
        var usernameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(usernameClaim);
        Assert.Equal(username, usernameClaim.Value);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeRoleClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var claims = DecodeToken(token);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains(roleClaims, c => c.Value == "Admin");
        Assert.Contains(roleClaims, c => c.Value == "User");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeIssuerClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("ai.orchestrator", jwtToken.Issuer);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeAudienceClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Contains("ai.orchestrator.api", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_ShouldSetExpirationTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var beforeGeneration = DateTime.UtcNow.AddMinutes(59);
        var afterGeneration = DateTime.UtcNow.AddMinutes(61);

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.True(jwtToken.ValidTo > beforeGeneration);
        Assert.True(jwtToken.ValidTo < afterGeneration);
    }

    [Fact]
    public void GenerateToken_WithEmptyRoles_ShouldCreateTokenWithoutRoleClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string>();

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);
        var claims = DecodeToken(token);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Empty(roleClaims);
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_ForValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForExpiredToken()
    {
        // Arrange - Create a service with 0 minute expiration
        _mockConfig.Setup(c => c.JwtExpirationMinutes).Returns(0);
        var shortLivedService = new JwtService(_mockConfig.Object);

        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = shortLivedService.GenerateToken(userId, username, roles);

        // Wait a moment to ensure expiration
        System.Threading.Thread.Sleep(1000);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForNullToken()
    {
        // Act
        var isValid = _jwtService.ValidateToken(null);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForEmptyToken()
    {
        // Act
        var isValid = _jwtService.ValidateToken("");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GetClaims_ShouldReturnClaims_FromValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        var claims = _jwtService.GetClaims(token);

        // Assert
        Assert.NotNull(claims);
        Assert.NotEmpty(claims);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == username);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GetClaims_ShouldReturnEmpty_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var claims = _jwtService.GetClaims(invalidToken);

        // Assert
        Assert.NotNull(claims);
        Assert.Empty(claims);
    }

    [Fact]
    public void GetUserIdFromToken_ShouldReturnUserId_FromValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        var extractedUserId = _jwtService.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(extractedUserId);
        Assert.Equal(userId, extractedUserId.Value);
    }

    [Fact]
    public void GetUserIdFromToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var userId = _jwtService.GetUserIdFromToken(invalidToken);

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void GetUsernameFromToken_ShouldReturnUsername_FromValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        var extractedUsername = _jwtService.GetUsernameFromToken(token);

        // Assert
        Assert.Equal(username, extractedUsername);
    }

    [Fact]
    public void GetUsernameFromToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var username = _jwtService.GetUsernameFromToken(invalidToken);

        // Assert
        Assert.Null(username);
    }

    [Fact]
    public void Constructor_ShouldThrowException_ForNullConfig()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JwtService(null));
    }

    [Fact]
    public void Constructor_ShouldThrowException_ForShortSecret()
    {
        // Arrange
        _mockConfig.Setup(c => c.JwtSecret).Returns("tooshort");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new JwtService(_mockConfig.Object));
        Assert.Contains("at least 32 characters", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowException_ForNullOrEmptySecret()
    {
        // Arrange
        _mockConfig.Setup(c => c.JwtSecret).Returns("");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new JwtService(_mockConfig.Object));
    }

    // Helper method to decode token for testing
    private List<Claim> DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.ToList();
    }
}
