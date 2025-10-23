namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Permission entity for granular access control
/// </summary>
public class Permission
{
    /// <summary>
    /// Permission identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Permission name (unique)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Permission description
    /// </summary>
    public string Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Roles that have this permission
    /// </summary>
    public List<RolePermission> RolePermissions { get; set; } = new();
}

/// <summary>
/// Default system permissions
/// </summary>
public static class Permissions
{
    public const string ManageUsers = "ManageUsers";
    public const string ResetPasswords = "ResetPasswords";
    public const string CreateAgents = "CreateAgents";
    public const string UpdateAgents = "UpdateAgents";
    public const string DeleteAgents = "DeleteAgents";
    public const string ViewAgents = "ViewAgents";
    public const string ManagePluginConfigs = "ManagePluginConfigs";
    public const string ViewPluginConfigs = "ViewPluginConfigs";
}
