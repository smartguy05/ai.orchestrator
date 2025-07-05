using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Services.Plugin;

public class PluginService : IPluginService
{
    private readonly Config _config = new();
    private static LogDelegate _logger;
    private static INotificationService _notificationService;
    private static readonly Dictionary<string, Assembly> _assemblyCache = new (StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, PluginLoadContext> _contextCache = new (StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<object>> _pluginInstanceCache = new(StringComparer.OrdinalIgnoreCase);

    public List<ToolCall> GetTools()
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find any plugins");
        }

        var configs = new List<ToolCall>();
        foreach (var plugin in plugins)
        {
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (config is not null)
            {
                using JsonDocument doc = JsonDocument.Parse(config);
                var element = doc.RootElement;
                var expando = element.Deserialize<ExpandoObject>();
                var dictionary = (IDictionary<string, object>)expando;

                if (dictionary.TryGetValue("tools", out var value))
                {
                    if (((JsonElement)value).ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in ((JsonElement)value).EnumerateArray())
                        {
                            configs.Add(JsonSerializer.Deserialize<ToolCall>(item, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true}));
                        }
                    }
                    else
                    {
                        var toolCall = JsonSerializer.Deserialize<ToolCall>((JsonElement)value);
                        configs.Add(toolCall);
                    }
                }
            }
        }

        var properties = new Dictionary<string, ToolProperty>();
        properties.Add("description", new ToolProperty
        {
            Type = "string",
            Description = "A description of the scheduled task."
        });
        properties.Add("name", new ToolProperty
        {
            Type = "string",
            Description = "A name for the scheduled task."
        });
        properties.Add("expiration", new ToolProperty
        {
            Type = "string",
            Description = "The datetime that the scheduled task should be performed at as a string. Supply either expiration or timeout."
        });
        properties.Add("timeout", new ToolProperty
        {
            Type = "number",
            Description = "How many seconds until the scheduled task should be performed. Supply either expiration or timeout."
        });
        properties.Add("isRecurring", new ToolProperty
        {
            Type = "boolean",
            Description = "Should this be a recurring scheduled task. If yes, true, if no, false."
        });
        configs.Add(new ToolCall
        {
            Function = new ToolFunction
            {
                Name = "schedule_task",
                Description = "Use this function to run another tool function at a future date. You can also schedule recurring tasks. Example: 'In 40 minutes send an email to test@test.com with the subject: An Email Subject. In the body of the email write a love song'",
                Parameters = new ToolParameters
                {
                    Type = "object",
                    Properties = properties,
                    Required = new List<string>
                    {
                        "name",
                        "description"
                    }
                }
            }
        });

        return configs;
    }
    
    public Dictionary<string, IEnumerable<string>> GetPluginContracts()
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find any plugins");
        }

        var configs = new Dictionary<string, IEnumerable<string>>();
        foreach (var plugin in plugins)
        {
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (config is not null)
            {
                using JsonDocument doc = JsonDocument.Parse(config);
                var element = doc.RootElement;
                var expando = element.Deserialize<ExpandoObject>();
                var dictionary = (IDictionary<string, object>)expando;

                if (dictionary.TryGetValue("tools", out var tools))
                {
                    var toolCalls = ((JsonElement)tools)
                        .EnumerateArray()
                        .Select(item => JsonSerializer.Deserialize<ToolCall>(item, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        }))
                        .ToList();
                    var functions = toolCalls.Select(s => s.Function.Name).ToList();
                    configs.Add(plugin, functions);
                }
            }
        }

        return configs;
    }

    public async Task<object> RunPlugin(OrchestratorRequest request)
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find specified plugin");
        }
        var plugin = plugins.FirstOrDefault(f =>
            string.Equals(f, request.Service, StringComparison.InvariantCultureIgnoreCase));
        if (plugin != null)
        {
            var command = GetPlugin<ICommand>(plugin);
            if (command != null)
            {
                var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
                return await command.Execute(request, config, GetTools(), _logger, _notificationService);
            }
        }
        await _logger(LogLevel.Warning, $"No plugin found with the name {request.Service}");
        throw new Exception("Invalid plugin specified!");
    }
    
    public async Task InitializePlugins(LogDelegate logger, INotificationService notificationService)
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find specified plugin");
        }
        _notificationService = notificationService;
        _logger = logger;

        foreach (var plugin in plugins)
        {
            var pluginAssembly = LoadPlugin($"{_config.PluginDirectory}/{plugin}.dll");
            await logger(LogLevel.Trace, $"-- Plugin {plugin} Loaded --");
                
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (!string.IsNullOrWhiteSpace(config))
            {
                await logger(LogLevel.Trace, $"-- Plugin {plugin} Config Loaded --");
            }
            var instances = CreatePluginInstances(pluginAssembly).ToList();

            if (!_pluginInstanceCache.ContainsKey(plugin))
            {
                _pluginInstanceCache.Add(plugin, instances);
            }

            var tasks = new List<Task<object>>();
            foreach (var instance in instances)
            {
                // 3. Only call Initialize if the instance is an ICommand.
                if (instance is ICommand command)
                {
                    tasks.Add(command.Initialize(config, logger, notificationService));
                    await logger(LogLevel.Trace, $"-- Command {command.Name} Initialized --");
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                await logger(LogLevel.Error, e.Message);
                throw;
            }
        }
    }
    
    public T GetPlugin<T>(string pluginName) where T : class
    {
        if (_pluginInstanceCache.TryGetValue(pluginName, out var instances))
        {
            return instances.OfType<T>().FirstOrDefault();
        }

        _logger?.Invoke(LogLevel.Error, $"Attempted to get plugin '{pluginName}' before it was initialized.", null).Wait();
        return null;
    }
    
    public Task DisposePlugins()
    {
        foreach (var context in _contextCache.Values)
        {
            context.Unload();
        }
        _assemblyCache.Clear();
        _contextCache.Clear();
        _pluginInstanceCache.Clear();
        return Task.CompletedTask;
    }
    
    private static IEnumerable<object> CreatePluginInstances(Assembly assembly)
    {
        var instances = new List<object>();
        foreach (var type in assembly.GetExportedTypes())
        {
            // This condition finds any concrete class that implements a known plugin interface.
            // You can add more interfaces here like || typeof(ILoggingPlugin).IsAssignableFrom(type)
            if (type.IsClass && !type.IsAbstract && (typeof(ICommand).IsAssignableFrom(type) || typeof(IConfirmationPlugin).IsAssignableFrom(type)))
            {
                // Ensure we only create one instance of a class, even if it implements multiple interfaces.
                if (!instances.Any(i => i.GetType() == type))
                {
                    instances.Add(Activator.CreateInstance(type));
                }
            }
        }
        return instances;
    }
    
    private static Assembly LoadPlugin(string relativePath)
    {
        var pluginName = Path.GetFileNameWithoutExtension(relativePath);
        if (_assemblyCache.TryGetValue(pluginName, out var cachedAssembly))
        {
            return cachedAssembly;
        }
        
        var pluginLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        _logger( LogLevel.Trace,$"Loading commands from: {pluginLocation}").ConfigureAwait(false);
        var loadContext = new PluginLoadContext(pluginLocation);
        var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        
        _contextCache[pluginName] = loadContext;
        _assemblyCache[pluginName] = assembly;

        return assembly;
    }
    
    private static string LoadConfig(string relativePath)
    {
        var configLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        _logger( LogLevel.Trace,$"Loading config from: {configLocation}").ConfigureAwait(false);
        if (File.Exists(configLocation))
        {
            return File.ReadAllText(configLocation);
        }

        return null;
    }
    
    private static IEnumerable<ICommand> CreateCommands(Assembly assembly)
    {
        var count = 0;

        foreach (var type in assembly.GetTypes())
        {
            if (typeof(ICommand).IsAssignableFrom(type))
            {
                if (Activator.CreateInstance(type) is ICommand result)
                {
                    count++;
                    yield return result;
                }
            }
        }

        if (count == 0)
        {
            var availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            throw new ApplicationException(
                $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");
        }
    }
}