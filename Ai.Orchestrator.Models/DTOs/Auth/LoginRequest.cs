using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}
