using System.Reflection;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Services;

public class LoggingService : ILoggingService
{
    private readonly Config _config = new();
    private bool _useConsole;
    private static List<ILoggingPlugin> _plugins = new();

    public LoggingService()
    {
        if (_config.LoggingPlugins == null || _config.LogToConsole)
        {
            _useConsole = true;
        }
        else
        {
            GetPlugins();   
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
            Console.WriteLine(message);
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
                var tasks = _plugins.Select(plugin => plugin.Log( level, message, exception).AsTask()).ToArray();

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error logging with plugin");
                Console.WriteLine(e);
                Console.WriteLine("---- Original Log message ----");
                Console.WriteLine(message);
                _useConsole = true;
            }
        }
    }

    private void GetPlugins()
    {
        if (_plugins.Count > 0 || _useConsole)
        {
            return;
        }
        
        _plugins = [];
        
        _plugins.AddRange(LoadPlugins());
    }
    
    private List<ILoggingPlugin> LoadPlugins()
    {
        var pluginLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _config.PluginDirectory.Replace('\\', Path.DirectorySeparatorChar)));
        var configLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _config.ConfigDirectory.Replace('\\', Path.DirectorySeparatorChar)));
        List<ILoggingPlugin> plugins = new ();
        
        foreach (var loggingPlugin in _config.LoggingPlugins)
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
                if (typeof(ILoggingPlugin).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is ILoggingPlugin result)
                    {
                        result.ConfigString = config;
                        plugins.Add(result);
                    }
                }
            }
            
        }
        
        return plugins;
    }
    
    private string LoadConfig(string configPath)
    {
        var configLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configPath.Replace('\\', Path.DirectorySeparatorChar)));
        Log( LogLevel.Info,$"Loading config from: {configLocation}").ConfigureAwait(false);
        if (File.Exists(configLocation))
        {
            return File.ReadAllText(configLocation);
        }

        return null;
    }
}