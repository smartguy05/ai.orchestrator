namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Junction table for Role-Permission many-to-many relationship
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Role identifier
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Permission identifier
    /// </summary>
    public int PermissionId { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Associated permission
    /// </summary>
    public Permission Permission { get; set; }
}
