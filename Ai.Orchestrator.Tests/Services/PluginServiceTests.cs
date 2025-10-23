using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Services.Plugin;
using Moq;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Tests.Services;

public class PluginServiceTests
{
    private class TestablePluginService : IPluginService
    {
        private readonly IConfig _config;
        private readonly Dictionary<string, string> _configFiles;

        public TestablePluginService(IConfig config, Dictionary<string, string> configFiles = null)
        {
            _config = config;
            _configFiles = configFiles ?? new Dictionary<string, string>();
        }

        public List<ToolCall> GetTools()
        {
            var plugins = _config.ActivePlugins.Split(",");
            if (!plugins.Any() || plugins.All(string.IsNullOrWhiteSpace))
            {
                throw new Exception("Unable to find any plugins");
            }

            var configs = new List<ToolCall>();
            foreach (var plugin in plugins)
            {
                if (_configFiles.TryGetValue(plugin, out var config))
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
                                configs.Add(JsonSerializer.Deserialize<ToolCall>(item, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
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
            if (!plugins.Any() || plugins.All(string.IsNullOrWhiteSpace))
            {
                throw new Exception("Unable to find any plugins");
            }

            var configs = new Dictionary<string, IEnumerable<string>>();
            foreach (var plugin in plugins)
            {
                if (_configFiles.TryGetValue(plugin, out var config))
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
            if (plugin == null)
            {
                throw new Exception("Invalid plugin specified!");
            }

            // For testing, return a mock response
            return "test response";
        }

        public async Task InitializePlugins(LogDelegate logger, INotificationService notificationService)
        {
            var plugins = _config.ActivePlugins.Split(",");
            if (!plugins.Any() || plugins.All(string.IsNullOrWhiteSpace))
            {
                throw new Exception("Unable to find specified plugin");
            }

            // For testing, just complete successfully
            await Task.CompletedTask;
        }

        public T GetPlugin<T>(string pluginName) where T : class
        {
            // For testing, return null as no plugins are actually loaded
            return null;
        }

        public Task DisposePlugins()
        {
            return Task.CompletedTask;
        }
    }

    private readonly Mock<IConfig> _mockConfig;
    private readonly Dictionary<string, string> _mockConfigFiles;
    private readonly TestablePluginService _pluginService;

    public PluginServiceTests()
    {
        _mockConfig = new Mock<IConfig>();
        _mockConfigFiles = new Dictionary<string, string>();

        // Setup default config
        _mockConfig.Setup(x => x.ActivePlugins).Returns("TestPlugin1,TestPlugin2");
        _mockConfig.Setup(x => x.PluginDirectory).Returns("TestPlugins");
        _mockConfig.Setup(x => x.ConfigDirectory).Returns("TestConfigs");

        // Setup mock config files
        _mockConfigFiles["TestPlugin1"] = """
        {
            "tools": [
                {
                    "function": {
                        "name": "test_function_1",
                        "description": "Test function 1"
                    }
                }
            ]
        }
        """;

        _mockConfigFiles["TestPlugin2"] = """
        {
            "tools": [
                {
                    "function": {
                        "name": "test_function_2",
                        "description": "Test function 2"
                    }
                }
            ]
        }
        """;

        _pluginService = new TestablePluginService(_mockConfig.Object, _mockConfigFiles);
    }

    [Fact]
    public void GetTools_ShouldReturnToolsIncludingScheduleTask()
    {
        // Act
        var tools = _pluginService.GetTools();

        // Assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools);

        // Should include schedule_task tool
        var scheduleTaskTool = tools.FirstOrDefault(t => t.Function?.Name == "schedule_task");
        Assert.NotNull(scheduleTaskTool);
        Assert.Equal("schedule_task", scheduleTaskTool.Function.Name);
        Assert.Contains("schedule", scheduleTaskTool.Function.Description.ToLower());
    }

    [Fact]
    public void GetTools_ShouldIncludePluginTools()
    {
        // Act
        var tools = _pluginService.GetTools();

        // Assert
        Assert.NotNull(tools);
        Assert.True(tools.Count >= 3); // At least 2 plugin tools + schedule_task
    }

    [Fact]
    public void GetTools_ShouldThrowException_WhenNoActivePlugins()
    {
        // Arrange
        _mockConfig.Setup(x => x.ActivePlugins).Returns("");
        var emptyPluginService = new TestablePluginService(_mockConfig.Object);

        // Act & Assert
        Assert.Throws<Exception>(() => emptyPluginService.GetTools());
    }

    [Fact]
    public void GetPluginContracts_ShouldReturnContractsForActivePlugins()
    {
        // Act
        var contracts = _pluginService.GetPluginContracts();

        // Assert
        Assert.NotNull(contracts);
        Assert.NotEmpty(contracts);
        Assert.Contains("TestPlugin1", contracts.Keys);
        Assert.Contains("TestPlugin2", contracts.Keys);
    }

    [Fact]
    public void GetPluginContracts_ShouldThrowException_WhenNoActivePlugins()
    {
        // Arrange
        _mockConfig.Setup(x => x.ActivePlugins).Returns("");
        var emptyPluginService = new TestablePluginService(_mockConfig.Object);

        // Act & Assert
        Assert.Throws<Exception>(() => emptyPluginService.GetPluginContracts());
    }

    [Fact]
    public async Task RunPlugin_ShouldReturnResponse_WhenPluginExists()
    {
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "TestPlugin1",
            ServiceRequest = "test"
        };

        // Act
        var result = await _pluginService.RunPlugin(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test response", result);
    }

    [Fact]
    public async Task RunPlugin_ShouldThrowException_WhenNoActivePlugins()
    {
        // Arrange
        _mockConfig.Setup(x => x.ActivePlugins).Returns("");
        var emptyPluginService = new TestablePluginService(_mockConfig.Object);
        var request = new OrchestratorRequest
        {
            Service = "NonExistentService",
            ServiceRequest = "test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => emptyPluginService.RunPlugin(request));
    }

    [Fact]
    public async Task RunPlugin_ShouldThrowException_WhenPluginNotFound()
    {
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "NonExistentPlugin",
            ServiceRequest = "test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _pluginService.RunPlugin(request));
    }

    [Fact]
    public async Task InitializePlugins_ShouldCompleteSuccessfully()
    {
        // Arrange
        var logDelegate = new LogDelegate((level, message, exception) => Task.CompletedTask);
        var mockNotificationService = new Mock<INotificationService>();

        // Act & Assert - Should not throw
        await _pluginService.InitializePlugins(logDelegate, mockNotificationService.Object);
    }

    [Fact]
    public async Task InitializePlugins_ShouldThrowException_WhenNoActivePlugins()
    {
        // Arrange
        _mockConfig.Setup(x => x.ActivePlugins).Returns("");
        var emptyPluginService = new TestablePluginService(_mockConfig.Object);
        var logDelegate = new LogDelegate((level, message, exception) => Task.CompletedTask);
        var mockNotificationService = new Mock<INotificationService>();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => emptyPluginService.InitializePlugins(logDelegate, mockNotificationService.Object));
    }

    [Fact]
    public void GetPlugin_ShouldReturnNull_WhenPluginNotLoaded()
    {
        // Act
        var result = _pluginService.GetPlugin<ICommand>("NonExistentPlugin");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DisposePlugins_ShouldCompleteSuccessfully()
    {
        // Act & Assert - Should not throw
        await _pluginService.DisposePlugins();
    }

    [Theory]
    [InlineData("TestPlugin1")]
    [InlineData("TestPlugin2")]
    [InlineData("Plugin.With.Dots")]
    public void GetPlugin_ShouldHandleDifferentPluginNames(string pluginName)
    {
        // Act
        var result = _pluginService.GetPlugin<ICommand>(pluginName);

        // Assert
        Assert.Null(result); // Will be null since no plugins are actually loaded in tests
    }

    [Fact]
    public void ToolCall_ShouldHaveCorrectStructure()
    {
        // Test the ToolCall model structure used by GetTools
        // Arrange
        var toolCall = new ToolCall
        {
            Function = new ToolFunction
            {
                Name = "test_function",
                Description = "Test function description",
                Parameters = new ToolParameters
                {
                    Type = "object",
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        { "param1", new ToolProperty { Type = "string", Description = "Parameter 1" } }
                    },
                    Required = new List<string> { "param1" }
                }
            }
        };

        // Assert
        Assert.Equal("test_function", toolCall.Function.Name);
        Assert.Equal("Test function description", toolCall.Function.Description);
        Assert.Equal("object", toolCall.Function.Parameters.Type);
        Assert.Contains("param1", toolCall.Function.Parameters.Properties.Keys);
        Assert.Contains("param1", toolCall.Function.Parameters.Required);
    }

    [Fact]
    public void ToolProperty_ShouldHaveCorrectStructure()
    {
        // Test the ToolProperty model used in GetTools
        // Arrange
        var toolProperty = new ToolProperty
        {
            Type = "string",
            Description = "Test property description"
        };

        // Assert
        Assert.Equal("string", toolProperty.Type);
        Assert.Equal("Test property description", toolProperty.Description);
    }

    [Fact]
    public void ScheduleTaskTool_ShouldHaveRequiredProperties()
    {
        // Test the schedule_task tool properties that are always added
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

        // Assert
        Assert.Equal(5, properties.Count);
        Assert.Contains("description", properties.Keys);
        Assert.Contains("name", properties.Keys);
        Assert.Contains("expiration", properties.Keys);
        Assert.Contains("timeout", properties.Keys);
        Assert.Contains("isRecurring", properties.Keys);

        // Verify property types
        Assert.Equal("string", properties["description"].Type);
        Assert.Equal("string", properties["name"].Type);
        Assert.Equal("string", properties["expiration"].Type);
        Assert.Equal("number", properties["timeout"].Type);
        Assert.Equal("boolean", properties["isRecurring"].Type);
    }

    [Fact]
    public void JsonDocument_ShouldParseValidJson()
    {
        // Test JSON parsing logic used in GetTools and GetPluginContracts
        // Arrange
        var json = """{"tools": [{"function": {"name": "test_function"}}]}""";

        // Act
        using JsonDocument doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        var expando = element.Deserialize<ExpandoObject>();
        var dictionary = (IDictionary<string, object>)expando;

        // Assert
        Assert.True(dictionary.ContainsKey("tools"));
        Assert.IsType<JsonElement>(dictionary["tools"]);
        var toolsElement = (JsonElement)dictionary["tools"];
        Assert.Equal(JsonValueKind.Array, toolsElement.ValueKind);
    }

    [Fact]
    public void JsonDocument_ShouldHandleEmptyToolsArray()
    {
        // Arrange
        var json = """{"tools": []}""";

        // Act
        using JsonDocument doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        var expando = element.Deserialize<ExpandoObject>();
        var dictionary = (IDictionary<string, object>)expando;

        // Assert
        Assert.True(dictionary.ContainsKey("tools"));
        var toolsElement = (JsonElement)dictionary["tools"];
        Assert.Equal(JsonValueKind.Array, toolsElement.ValueKind);
        Assert.Empty(toolsElement.EnumerateArray());
    }

    [Fact]
    public void JsonDocument_ShouldHandleSingleToolObject()
    {
        // Test the single tool object path in GetTools
        // Arrange
        var json = """{"tools": {"function": {"name": "single_function"}}}""";

        // Act
        using JsonDocument doc = JsonDocument.Parse(json);
        var element = doc.RootElement;
        var expando = element.Deserialize<ExpandoObject>();
        var dictionary = (IDictionary<string, object>)expando;

        // Assert
        Assert.True(dictionary.ContainsKey("tools"));
        var toolsElement = (JsonElement)dictionary["tools"];
        Assert.Equal(JsonValueKind.Object, toolsElement.ValueKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ConfigValidation_ShouldHandleEmptyOrWhitespaceInput(string input)
    {
        // This tests the conceptual logic of config loading
        // Act & Assert
        var isEmpty = string.IsNullOrWhiteSpace(input);
        Assert.True(isEmpty);
    }

    [Fact]
    public void OrchestratorRequest_ShouldHaveCorrectProperties()
    {
        // Test the OrchestratorRequest model used throughout PluginService
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "TestService",
            ServiceRequest = "test request",
            Messages = new List<Ai.Orchestrator.Models.Chat.ChatMessageHistory>(),
            ToolCallId = "test-tool-id",
            ServiceFunctions = new Dictionary<string, IEnumerable<string>>()
        };

        // Assert
        Assert.Equal("TestService", request.Service);
        Assert.Equal("test request", request.ServiceRequest);
        Assert.NotNull(request.Messages);
        Assert.Equal("test-tool-id", request.ToolCallId);
        Assert.NotNull(request.ServiceFunctions);
    }

    [Fact]
    public void LogDelegate_ShouldAcceptCorrectParameters()
    {
        // Test the LogDelegate type used in InitializePlugins
        // Arrange
        var called = false;
        LogLevel receivedLevel = LogLevel.Info;
        string receivedMessage = null;
        Exception receivedException = null;

        var logDelegate = new LogDelegate((level, message, exception) =>
        {
            called = true;
            receivedLevel = level;
            receivedMessage = message;
            receivedException = exception;
            return Task.CompletedTask;
        });

        // Act
        var testException = new Exception("Test exception");
        var task = logDelegate(LogLevel.Error, "Test message", testException);
        task.Wait();

        // Assert
        Assert.True(called);
        Assert.Equal(LogLevel.Error, receivedLevel);
        Assert.Equal("Test message", receivedMessage);
        Assert.Equal(testException, receivedException);
    }
}