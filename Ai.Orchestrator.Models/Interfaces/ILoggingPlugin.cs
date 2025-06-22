using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ILoggingPlugin
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ConfigString { get; set; }
    
    public ValueTask Log(LogLevel logLevel, string message, Exception exception = null);
}