using Ai.Orchestrator.Models.Extensions;

namespace Ai.Orchestrator.Models.Interfaces;

public abstract class LoggingBase<T>: ILoggingPlugin where T : ILoggingConfig
{
    public virtual string Name { get; set; }
    public virtual string Description { get; set; }
    public string ConfigString { get; set; }

    public async ValueTask Log(Ai.Orchestrator.Models.Enums.LogLevel logLevel, string message, Exception exception = null)
    {
        var config = ConfigString.ReadLoggingConfig<T>();
        if (logLevel < config.MinimumLogLevel)
        {
            return;
        }
        await DoWork(config, logLevel, message, exception);
    }

    protected abstract Task DoWork(T config, Ai.Orchestrator.Models.Enums.LogLevel logLevel, string message, Exception exception = null);
}