namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Junction table for User-Role many-to-many relationship
/// </summary>
public class UserRole
{
    /// <summary>
    /// User identifier
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role identifier
    /// </summary>
    public int RoleId { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated user
    /// </summary>
    public User User { get; set; }

    /// <summary>
    /// Associated role
    /// </summary>
    public Role Role { get; set; }
}
