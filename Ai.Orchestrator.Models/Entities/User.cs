using BCrypt.Net;

namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// User entity for authentication and authorization
/// Stores user accounts with hashed passwords (never plain text!)
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Login username (unique)
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// User email address (unique)
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// BCrypt hashed password
    /// NEVER store plain text passwords!
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// Account active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created this account (for audit trail)
    /// </summary>
    public Guid? CreatedById { get; set; }

    // Navigation properties

    /// <summary>
    /// Self-referential relationship: user who created this account
    /// </summary>
    public User CreatedBy { get; set; }

    /// <summary>
    /// Agents owned by this user
    /// </summary>
    public List<Agent> Agents { get; set; } = new();

    /// <summary>
    /// User's role assignments
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = new();

    /// <summary>
    /// Sets the user's password with BCrypt hashing
    /// </summary>
    /// <param name="plainPassword">Plain text password to hash</param>
    /// <exception cref="ArgumentException">Thrown when password is invalid</exception>
    public void SetPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(plainPassword));
        }

        // Enforce minimum password requirements
        if (plainPassword.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long", nameof(plainPassword));
        }

        // Hash password with BCrypt (work factor: 12)
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
    }

    /// <summary>
    /// Verifies if the provided password matches the stored hash
    /// </summary>
    /// <param name="plainPassword">Plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(PasswordHash))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);
        }
        catch
        {
            return false;
        }
    }
}
