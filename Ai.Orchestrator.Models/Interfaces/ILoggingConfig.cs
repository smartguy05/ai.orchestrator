using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ILoggingConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public LogLevel MinimumLogLevel { get; set; } 
}