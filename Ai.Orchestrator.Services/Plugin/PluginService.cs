using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Services.Plugin;

/// <summary>
/// Per-agent plugin service
/// Loads and manages plugins specific to an agent from database configuration
/// </summary>
public class PluginService_New : IPluginService
{
    private readonly Agent _agent;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfig _config;
    private LogDelegate _logger;
    private INotificationService _notificationService;

    // Per-agent caches (non-static)
    private readonly Dictionary<string, Assembly> _assemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginLoadContext> _contextCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<object>> _pluginInstanceCache = new(StringComparer.OrdinalIgnoreCase);

    public PluginService_New(Agent agent, IServiceProvider serviceProvider)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = serviceProvider.GetRequiredService<IConfig>();
    }

    public List<ToolCall> GetTools()
    {
        var pluginConfigs = GetAgentPluginConfigurationsFromDb();

        if (!pluginConfigs.Any())
        {
            return new List<ToolCall>();
        }

        var configs = new List<ToolCall>();

        foreach (var pluginConfig in pluginConfigs)
        {
            if (!string.IsNullOrWhiteSpace(pluginConfig.ConfigurationJson))
            {
                using JsonDocument doc = JsonDocument.Parse(pluginConfig.ConfigurationJson);
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

        // Add schedule_task tool
        configs.Add(CreateScheduleTaskTool());

        return configs;
    }

    public Dictionary<string, IEnumerable<string>> GetPluginContracts()
    {
        var pluginConfigs = GetAgentPluginConfigurationsFromDb();

        if (!pluginConfigs.Any())
        {
            return new Dictionary<string, IEnumerable<string>>();
        }

        var contracts = new Dictionary<string, IEnumerable<string>>();

        foreach (var pluginConfig in pluginConfigs)
        {
            if (!string.IsNullOrWhiteSpace(pluginConfig.ConfigurationJson))
            {
                using JsonDocument doc = JsonDocument.Parse(pluginConfig.ConfigurationJson);
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
                    contracts.Add(pluginConfig.PluginName, functions);
                }
            }
        }

        return contracts;
    }

    public async Task<object> RunPlugin(OrchestratorRequest request)
    {
        var pluginConfigs = GetAgentPluginConfigurationsFromDb();

        if (!pluginConfigs.Any())
        {
            throw new Exception("No plugins configured for this agent");
        }

        // Find plugin configuration by service name
        var pluginConfig = pluginConfigs.FirstOrDefault(pc =>
            string.Equals(pc.PluginName, request.Service, StringComparison.InvariantCultureIgnoreCase));

        if (pluginConfig == null)
        {
            await _logger(LogLevel.Warning, $"No plugin found with the name {request.Service}");
            throw new Exception($"Invalid plugin specified: {request.Service}");
        }

        var command = GetPlugin<ICommand>(pluginConfig.PluginName);
        if (command != null)
        {
            return await command.Execute(request, pluginConfig.ConfigurationJson, GetTools(), _logger, _notificationService);
        }

        await _logger(LogLevel.Warning, $"No command implementation found for plugin {request.Service}");
        throw new Exception("Invalid plugin specified!");
    }

    public async Task InitializePlugins(LogDelegate logger, INotificationService notificationService)
    {
        _notificationService = notificationService;
        _logger = logger;

        var pluginConfigs = GetAgentPluginConfigurationsFromDb();

        if (!pluginConfigs.Any())
        {
            await _logger(LogLevel.Info, $"No plugins configured for agent '{_agent.Name}'");
            return;
        }

        foreach (var pluginConfig in pluginConfigs)
        {
            var pluginAssembly = LoadPlugin($"{_config.PluginDirectory}/{pluginConfig.PluginName}.dll");
            await logger(LogLevel.Trace, $"-- Plugin {pluginConfig.PluginName} loaded for agent '{_agent.Name}' --");

            if (!string.IsNullOrWhiteSpace(pluginConfig.ConfigurationJson))
            {
                await logger(LogLevel.Trace, $"-- Plugin {pluginConfig.PluginName} config loaded --");
            }

            var instances = CreatePluginInstances(pluginAssembly).ToList();

            if (!_pluginInstanceCache.ContainsKey(pluginConfig.PluginName))
            {
                _pluginInstanceCache.Add(pluginConfig.PluginName, instances);
            }

            var tasks = new List<Task<object>>();
            foreach (var instance in instances)
            {
                if (instance is ICommand command)
                {
                    tasks.Add(command.Initialize(pluginConfig.ConfigurationJson, logger, notificationService));
                    await logger(LogLevel.Trace, $"-- Command {command.Name} initialized for agent '{_agent.Name}' --");
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                await logger(LogLevel.Error, $"Error initializing plugin {pluginConfig.PluginName}: {e.Message}");
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

    // Private helper methods

    private List<PluginConfiguration> GetAgentPluginConfigurationsFromDb()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        return context.PluginConfigurations
            .Where(pc => pc.AgentId == _agent.Id && pc.IsActive)
            .ToList();
    }

    private IEnumerable<object> CreatePluginInstances(Assembly assembly)
    {
        var instances = new List<object>();
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsClass && !type.IsAbstract && (typeof(ICommand).IsAssignableFrom(type) || typeof(INotificationPlugin).IsAssignableFrom(type)))
            {
                if (!instances.Any(i => i.GetType() == type))
                {
                    instances.Add(Activator.CreateInstance(type));
                }
            }
        }
        return instances;
    }

    private Assembly LoadPlugin(string relativePath)
    {
        var pluginName = Path.GetFileNameWithoutExtension(relativePath);
        if (_assemblyCache.TryGetValue(pluginName, out var cachedAssembly))
        {
            return cachedAssembly;
        }

        var pluginLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        _logger(LogLevel.Trace, $"Loading commands from: {pluginLocation}").ConfigureAwait(false);
        var loadContext = new PluginLoadContext(pluginLocation);
        var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));

        _contextCache[pluginName] = loadContext;
        _assemblyCache[pluginName] = assembly;

        return assembly;
    }

    private ToolCall CreateScheduleTaskTool()
    {
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

        return new ToolCall
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
        };
    }
}
