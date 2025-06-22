namespace Ai.Orchestrator.Models.Enums;

public enum LogLevel
{
    /// <summary>
    /// Represents a critical error that has lead to application termination
    /// </summary>
    Fatal,
    /// <summary>
    /// Signals that an error has occurred, but the application can continue running
    /// </summary>
    Error,
    /// <summary>
    /// Indicates potential issues or unexpected events that do not necessarily halt execution
    /// </summary>
    Warning,
    /// <summary>
    /// General information about the application's operation
    /// </summary>
    Info,
    /// <summary>
    /// Provides detailed information, typically useful for debugging purposes
    /// </summary>
    Debug,
    /// <summary>
    /// The most fine-grained level, offering full visibility. Usually not needed unless you require complete detail
    /// </summary>
    Trace
}