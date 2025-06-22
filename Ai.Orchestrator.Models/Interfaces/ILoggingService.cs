namespace Ai.Orchestrator.Models.Interfaces;

public interface ILoggingService
{
    Task LogInformation(string message);
    Task LogError(string message, Exception exception = null);
    Task LogWarning(string message);
    Task Log(Ai.Orchestrator.Models.Enums.LogLevel level, string message, Exception exception = null);
}