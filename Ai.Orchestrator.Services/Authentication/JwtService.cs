using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Ai.Orchestrator.Services.Authentication;

/// <summary>
/// Service for JWT token generation and validation
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfig _config;
    private readonly OrchestratorDbContext _context;
    private readonly byte[] _key;

    public JwtService(IConfig config, OrchestratorDbContext context)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _context = context ?? throw new ArgumentNullException(nameof(context));

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

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<RefreshToken> StoreRefreshTokenAsync(Guid userId, string jwtId, string token, string ipAddress, string userAgent)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            JwtId = jwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_config.RefreshTokenExpirationDays),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken> ValidateRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null || !refreshToken.IsValid())
        {
            return null;
        }

        return refreshToken;
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, string newJwtId, string ipAddress, string userAgent)
    {
        // Mark old token as used
        oldToken.IsUsed = true;
        oldToken.UsedAt = DateTime.UtcNow;
        _context.RefreshTokens.Update(oldToken);

        // Generate new refresh token
        var newTokenString = GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = oldToken.UserId,
            Token = newTokenString,
            JwtId = newJwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_config.RefreshTokenExpirationDays),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        // Link the chain
        oldToken.ReplacedByTokenId = newRefreshToken.Id;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return newRefreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token, string reason)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null && !refreshToken.IsRevoked)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = reason;

            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }

    public string GetJwtId(string token)
    {
        var claims = GetClaims(token);
        var jtiClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        return jtiClaim?.Value;
    }

    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
}
