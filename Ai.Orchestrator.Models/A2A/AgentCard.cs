namespace Ai.Orchestrator.Models.A2A;

public record AgentCard
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Version { get; init; }
    /// <summary>
    /// The specific endpoint within the agent’s server where A2A communication happens
    /// </summary>
    public string Url { get; init; }
    public AgentCapabilities Capabilities { get; init; }
    public IEnumerable<string> DefaultInputModes { get; init; }
    public IEnumerable<string> DefaultOutputModes { get; init; }
    public IEnumerable<AgentSkill> Skills { get; init; }
    public AgentProvider Provider { get; init; }
}