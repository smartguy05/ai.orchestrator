namespace Ai.Orchestrator.Models.DTOs.Users;

/// <summary>
/// Request model for updating user information
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// New email address (optional)
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Active status (optional)
    /// </summary>
    public bool? IsActive { get; set; }
}
