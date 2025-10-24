using System.Reflection;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Services;

/// <summary>
/// Per-agent logging service
/// Loads logging plugins specific to an agent from Agent.LoggingPlugins
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly Agent _agent;
    private readonly IConfig _config;
    private readonly bool _useConsole;
    private readonly List<ILoggingPlugin> _plugins = new();

    public LoggingService(Agent agent, IConfig config)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Use console if no logging plugins configured or LogToConsole is true
        _useConsole = string.IsNullOrWhiteSpace(_agent.LoggingPlugins) || _config.LogToConsole;

        if (!string.IsNullOrWhiteSpace(_agent.LoggingPlugins))
        {
            LoadPlugins();
        }
    }

    public Task LogInformation(string message)
    {
        return Log(LogLevel.Info, message);
    }

    public Task LogError(string message, Exception exception = null)
    {
        return Log(LogLevel.Error, message, exception);
    }

    public Task LogWarning(string message)
    {
        return Log(LogLevel.Warning, message);
    }

    public async Task Log(LogLevel level, string message, Exception exception = null)
    {
        if (_useConsole)
        {
            Console.WriteLine($"[{_agent.Name}] {message}");
            if (exception is not null)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
                var inner = exception.InnerException;
                if (exception.InnerException is not null)
                {
                    Console.WriteLine("Inner Exception(s)");
                }
                while (inner is not null)
                {
                    Console.WriteLine(inner.Message);
                    inner = inner.InnerException;
                }
            }
        }

        if (_plugins.Count > 0)
        {
            try
            {
                var tasks = _plugins.Select(plugin => plugin.Log(level, message, exception).AsTask()).ToArray();
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{_agent.Name}] Error logging with plugin");
                Console.WriteLine(e);
                Console.WriteLine("---- Original Log message ----");
                Console.WriteLine(message);
            }
        }
    }

    private void LoadPlugins()
    {
        var loggingPluginNames = _agent.LoggingPlugins
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList();

        var pluginLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _config.PluginDirectory.Replace('\\', Path.DirectorySeparatorChar)));
        var configLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _config.ConfigDirectory.Replace('\\', Path.DirectorySeparatorChar)));

        foreach (var loggingPlugin in loggingPluginNames)
        {
            try
            {
                var pluginPath = $"{pluginLocation}/{loggingPlugin}.dll";
                var configPath = $"{configLocation}/{loggingPlugin}.json";

                Log(LogLevel.Trace, $"Loading Logging Config from {pluginPath}").ConfigureAwait(false);
                var config = LoadConfig(configPath);

                Log(LogLevel.Trace, $"Loading Logging Plugin from: {pluginPath}").ConfigureAwait(false);
                var loadContext = new PluginLoadContext(pluginPath);
                var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginPath)));

                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ILoggingPlugin).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                    {
                        if (Activator.CreateInstance(type) is ILoggingPlugin result)
                        {
                            result.ConfigString = config;
                            _plugins.Add(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_agent.Name}] Error loading logging plugin '{loggingPlugin}': {ex.Message}");
            }
        }
    }

    private string LoadConfig(string configPath)
    {
        Log(LogLevel.Info, $"Loading config from: {configPath}").ConfigureAwait(false);
        if (File.Exists(configPath))
        {
            return File.ReadAllText(configPath);
        }

        return null;
    }
}
