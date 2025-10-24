using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
/// <example>
/// {
///   "Username": "john.doe",
///   "Email": "john.doe@example.com",
///   "Password": "SecurePassword123!"
/// }
/// </example>
public class RegisterRequest
{
    /// <summary>
    /// Username for the new account (3-100 characters)
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters")]
    public string Username { get; set; }

    /// <summary>
    /// Email address for the new account (must be valid email format)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// Password for the new account (minimum 8 characters, will be hashed with BCrypt)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; }
}
