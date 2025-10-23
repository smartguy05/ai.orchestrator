namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Role entity for role-based access control
/// </summary>
public class Role
{
    /// <summary>
    /// Role identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role name (unique)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Users assigned to this role
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = new();

    /// <summary>
    /// Permissions granted to this role
    /// </summary>
    public List<RolePermission> RolePermissions { get; set; } = new();
}

/// <summary>
/// Default system roles
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string AgentManager = "AgentManager";
    public const string User = "User";
    public const string ReadOnly = "ReadOnly";
}
