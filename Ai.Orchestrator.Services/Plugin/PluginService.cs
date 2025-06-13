using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Services.Plugin;

public class PluginService : IPluginService
{
    private readonly Config _config;
    private readonly ILogger<PluginService> _logger;

    public PluginService(ILogger<PluginService> logger)
    {
        _logger = logger;
        _config = new Config();
    }

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
            var pluginAssembly = LoadPlugin($"{_config.PluginDirectory}/{plugin}.dll");
            _logger.LogInformation($"-- Plugin {plugin} Loaded --");
            
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (!string.IsNullOrWhiteSpace(config))
            {
                _logger.LogInformation($"-- Plugin {plugin} Config Loaded --");   
            }
            var commands = CreateCommands(pluginAssembly).ToList();

            var tasks = new List<Task<object>>();
            _logger.LogInformation($"-- Total Commands: {commands.Count} --");
            foreach (var command in commands)
            {
                tasks.Add(command?.Execute(request, config, GetTools()));
                _logger.LogInformation($"-- Command {command.Name} Started --");
            }
            var results = (await Task.WhenAll(tasks)).ToList();

            if (results.Any() && results.Count == 1)
            {
                return results.First();
            }

            return results;
        }
        _logger.LogError($"No plugin found with the name {request.Service}");
        throw new Exception("Invalid plugin specified!");
    }
    
    public async Task InitializePlugins()
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find specified plugin");
        }

        foreach (var plugin in plugins)
        {
            var pluginAssembly = LoadPlugin($"{_config.PluginDirectory}/{plugin}.dll");
            _logger.LogInformation($"-- Plugin {plugin} Loaded --");
                
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (!string.IsNullOrWhiteSpace(config))
            {
                _logger.LogInformation($"-- Plugin {plugin} Config Loaded --");   
            }
            var commands = CreateCommands(pluginAssembly).ToList();

            var tasks = new List<Task<object>>();
            _logger.LogInformation($"-- Total Commands: {commands.Count} --");
            foreach (var command in commands)
            {
                tasks.Add(command?.Initialize(config));
                _logger.LogInformation($"-- Command {command.Name} Initialized --");
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public async Task DisposePlugins()
    {
        var plugins = _config.ActivePlugins.Split(",");
        if (!plugins.Any())
        {
            throw new Exception("Unable to find specified plugin");
        }

        foreach (var plugin in plugins)
        {
            var pluginAssembly = LoadPlugin($"{_config.PluginDirectory}/{plugin}.dll");
            _logger.LogInformation($"-- Plugin {plugin} Loaded --");
                
            var config = LoadConfig($"{_config.ConfigDirectory}/{plugin}.json");
            if (!string.IsNullOrWhiteSpace(config))
            {
                _logger.LogInformation($"-- Plugin {plugin} Config Loaded --");   
            }
            var commands = CreateCommands(pluginAssembly).ToList();

            var tasks = new List<Task>();
            _logger.LogInformation($"-- Total Commands: {commands.Count} --");
            foreach (var command in commands)
            {
                tasks.Add(command.Dispose());
                _logger.LogInformation($"-- Disposing of command {command.Name} --");
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
    
    private static Assembly LoadPlugin(string relativePath)
    {
        var pluginLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        Console.WriteLine($"Loading commands from: {pluginLocation}");
        var loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }
    
    private static string LoadConfig(string relativePath)
    {
        var configLocation = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        Console.WriteLine($"Loading config from: {configLocation}");
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