using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for password reset (admin only)
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; }
}
