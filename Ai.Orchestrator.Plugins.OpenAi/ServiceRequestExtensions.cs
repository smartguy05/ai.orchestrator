using Ai.Orchestrator.Plugins.OpenAi.Models;

namespace Ai.Orchestrator.Plugins.OpenAi;

public static class ServiceConfigExtensions
{
    public static OpenAiApi GetApiConfig(this ServiceConfig config, string agentName)
    {
        return config.Agents.FirstOrDefault(f => string.Equals(f.Name, agentName, StringComparison.InvariantCultureIgnoreCase)) 
                             ?? config.Agents.FirstOrDefault(f => f.Default) 
                             ?? config.Agents.FirstOrDefault();
    }
}