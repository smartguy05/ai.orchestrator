using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for user login
/// </summary>
/// <example>
/// {
///   "Username": "john.doe",
///   "Password": "SecurePassword123!"
/// }
/// </example>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }

    /// <summary>
    /// Password for authentication (will be verified against BCrypt hash)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}
