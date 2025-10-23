namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Agent-Tool junction table
/// Defines which tools/functions are enabled for each agent
/// </summary>
public class AgentTool
{
    /// <summary>
    /// Record identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Agent identifier
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// Tool/function name
    /// </summary>
    public string ToolName { get; set; }

    /// <summary>
    /// Tool enabled status
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated agent
    /// </summary>
    public Agent Agent { get; set; }
}
