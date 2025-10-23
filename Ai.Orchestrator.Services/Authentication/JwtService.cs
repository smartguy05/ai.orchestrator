using Ai.Orchestrator.Models.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ai.Orchestrator.Services.Authentication;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfig _config;
    private readonly byte[] _key;

    public JwtService(IConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(_config.JwtSecret))
        {
            throw new ArgumentException("JWT secret cannot be null or empty", nameof(config));
        }

        if (_config.JwtSecret.Length < 32)
        {
            throw new ArgumentException("JWT secret must be at least 32 characters long", nameof(config));
        }

        _key = Encoding.UTF8.GetBytes(_config.JwtSecret);
    }

    public string GenerateToken(Guid userId, string username, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_config.JwtExpirationMinutes),
            Issuer = _config.JwtIssuer,
            Audience = _config.JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateIssuer = true,
                ValidIssuer = _config.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _config.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expiration
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<Claim> GetClaims(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new List<Claim>();
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.ToList();
        }
        catch
        {
            return new List<Claim>();
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        var claims = GetClaims(token);
        var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public string GetUsernameFromToken(string token)
    {
        var claims = GetClaims(token);
        var usernameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        return usernameClaim?.Value;
    }
}
