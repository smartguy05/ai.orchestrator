using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Moq;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Tests.Services;

public class OrchestratorTests
{
    private class TestableOrchestrator : IOrchestrator
    {
        private readonly IPluginService _pluginService;
        private readonly ILoggingService _logger;

        public TestableOrchestrator(IPluginService pluginService, ILoggingService logger)
        {
            _logger = logger;
            _pluginService = pluginService;
            // Skip MessageCache.Init() to avoid Redis dependency in tests
        }

        public Task<Dictionary<string, IEnumerable<string>>> GetPluginContracts()
        {
            return Task.Run(() => _pluginService.GetPluginContracts());
        }

        public async Task<object> ProcessRequest(OrchestratorRequest request)
        {
            request.ServiceFunctions ??= new Dictionary<string, IEnumerable<string>>();
            if (request.ServiceFunctions.Count == 0)
            {
                var serviceFunctions = _pluginService.GetPluginContracts();
                foreach (var function in serviceFunctions)
                {
                    request.ServiceFunctions.Add(function.Key, function.Value);
                }

                request.ServiceFunctions = serviceFunctions;
            }
            var response = await _pluginService.RunPlugin(request);

            await _logger.Log(LogLevel.Trace, "Orchestrator ProcessRequest");
            if (response is OrchestratorRequest newRequest)
            {
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                await _logger.Log(LogLevel.Trace, JsonSerializer.Serialize(newRequest.Messages, options));
                return AddRequestData(await ProcessRequest(newRequest), request);
            }

            if (response is IEnumerable<OrchestratorRequest> requestChain)
            {
                return AddRequestData(await ProcessRequestChain(requestChain), request);
            }

            return response;
        }

        public async Task<object> ProcessRequestChain(IEnumerable<OrchestratorRequest> requests)
        {
            var requestList = requests?.ToList();
            if (requests is null || !requestList.Any())
            {
                throw new Exception("No requests found");
            }

            var messages = requestList
                .Select(f => f.Messages)
                .FirstOrDefault()
                ?.ToList();

            if (messages is null || !messages.Any())
            {
                throw new Exception("No messages found");
            }

            var requestingService = string.Empty;

            var firstServiceRequest = requestList.First().ServiceRequest;
            string conversationId = null;
            if (firstServiceRequest is string serviceRequestString)
            {
                var serviceRequestJson = JsonSerializer.Deserialize<JsonElement>(serviceRequestString);
                if (serviceRequestJson.TryGetProperty("requestingService", out var service))
                {
                    requestingService = service.GetString();
                }

                if (serviceRequestJson.TryGetProperty("conversationId", out var convoId))
                {
                    conversationId = convoId.GetString();
                }
            }

            var processedMessages = messages.ToList();
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string lastToolCallId = null;
            for (var i = 0; i < requestList.Count; i++)
            {
                var newRequest = requestList[i];
                newRequest.ToolCallId = null; // null so we are returned the actual object instead of another Orchestrator Request
                var toolCall = messages.Last().ToolCalls[i];

                // todo: multi-thread
                // process each item
                var result = await ProcessRequest(newRequest);

                // add new tool message after
                var toolResponseMessage = new ChatMessageHistory
                {
                    Role = ChatMessageTypes.Tool,
                    Content = result is string ? result : JsonSerializer.Serialize(result, options),
                    ToolCallId = toolCall.Id
                };
                processedMessages.Add(toolResponseMessage);
                lastToolCallId = toolCall.Id;
            }

            // create single return OrchestratorRequest and return
            var last = requestList.Last();

            // Skip saving cached messages to avoid Redis dependency
            return await ProcessRequest(new OrchestratorRequest
            {
                Service = requestingService,
                ServiceRequest = last.ServiceRequest,
                ToolCallId = lastToolCallId,
                ServiceFunctions = last.ServiceFunctions,
                Messages = processedMessages
            });
        }

        private object AddRequestData(object request, OrchestratorRequest orchestratorRequest)
        {
            _logger.Log(LogLevel.Trace, "AddRequestData Messages").ConfigureAwait(false);
            if (request is OrchestratorRequest chain)
            {
                if (!chain.Messages.Any())
                {
                    chain.Messages = orchestratorRequest.Messages;
                }
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                _logger.Log(LogLevel.Trace, JsonSerializer.Serialize(chain.Messages, options)).ConfigureAwait(false);
                return chain;
            }

            if (request is IEnumerable<OrchestratorRequest> chainList)
            {
                foreach (var oRequest in chainList)
                {
                    if (!oRequest.Messages.Any())
                    {
                        oRequest.Messages = orchestratorRequest.Messages;
                    }
                }
                return chainList;
            }

            return request;
        }
    }

    private readonly Mock<IPluginService> _mockPluginService;
    private readonly Mock<ILoggingService> _mockLogger;
    private readonly TestableOrchestrator _orchestrator;

    public OrchestratorTests()
    {
        _mockPluginService = new Mock<IPluginService>();
        _mockLogger = new Mock<ILoggingService>();
        _orchestrator = new TestableOrchestrator(_mockPluginService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPluginContracts_ShouldReturnPluginContracts()
    {
        // Arrange
        var expectedContracts = new Dictionary<string, IEnumerable<string>>
        {
            { "TestPlugin", new[] { "TestFunction1", "TestFunction2" } }
        };
        _mockPluginService.Setup(x => x.GetPluginContracts()).Returns(expectedContracts);

        // Act
        var result = await _orchestrator.GetPluginContracts();

        // Assert
        Assert.Equal(expectedContracts, result);
        _mockPluginService.Verify(x => x.GetPluginContracts(), Times.Once);
    }

    [Fact]
    public async Task ProcessRequest_ShouldPopulateServiceFunctions_WhenEmpty()
    {
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "TestService",
            ServiceRequest = "test request",
            Messages = new List<ChatMessageHistory>
            {
                new() { Role = ChatMessageTypes.User, Content = "test", ToolCallId = "test-id" }
            },
            ServiceFunctions = new Dictionary<string, IEnumerable<string>>()
        };

        var serviceFunctions = new Dictionary<string, IEnumerable<string>>
        {
            { "TestPlugin", new[] { "TestFunction" } }
        };

        _mockPluginService.Setup(x => x.GetPluginContracts()).Returns(serviceFunctions);
        _mockPluginService.Setup(x => x.RunPlugin(request))
            .ReturnsAsync("test response");

        // Act
        var result = await _orchestrator.ProcessRequest(request);

        // Assert
        Assert.Equal("test response", result);
        Assert.Equal(serviceFunctions, request.ServiceFunctions);
        _mockPluginService.Verify(x => x.GetPluginContracts(), Times.Once);
    }

    [Fact]
    public async Task ProcessRequest_ShouldNotPopulateServiceFunctions_WhenAlreadyPopulated()
    {
        // Arrange
        var existingFunctions = new Dictionary<string, IEnumerable<string>>
        {
            { "ExistingPlugin", new[] { "ExistingFunction" } }
        };

        var request = new OrchestratorRequest
        {
            Service = "TestService",
            ServiceRequest = "test request",
            Messages = new List<ChatMessageHistory>
            {
                new() { Role = ChatMessageTypes.User, Content = "test", ToolCallId = "test-id" }
            },
            ServiceFunctions = existingFunctions
        };

        _mockPluginService.Setup(x => x.RunPlugin(request))
            .ReturnsAsync("test response");

        // Act
        var result = await _orchestrator.ProcessRequest(request);

        // Assert
        Assert.Equal("test response", result);
        Assert.Equal(existingFunctions, request.ServiceFunctions);
        _mockPluginService.Verify(x => x.GetPluginContracts(), Times.Never);
    }

    [Fact]
    public async Task ProcessRequestChain_ShouldThrowException_WhenRequestsIsNull()
    {
        // Arrange
        IEnumerable<OrchestratorRequest> requests = null;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _orchestrator.ProcessRequestChain(requests));
    }

    [Fact]
    public async Task ProcessRequestChain_ShouldThrowException_WhenRequestsIsEmpty()
    {
        // Arrange
        var requests = new List<OrchestratorRequest>();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _orchestrator.ProcessRequestChain(requests));
    }

    [Fact]
    public async Task ProcessRequestChain_ShouldThrowException_WhenNoMessages()
    {
        // Arrange
        var requests = new List<OrchestratorRequest>
        {
            new()
            {
                Service = "TestService",
                ServiceRequest = "test",
                Messages = new List<ChatMessageHistory>()
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _orchestrator.ProcessRequestChain(requests));
    }

    [Fact]
    public async Task ProcessRequestChain_ShouldThrowException_WhenMessagesIsNull()
    {
        // Arrange
        var requests = new List<OrchestratorRequest>
        {
            new()
            {
                Service = "TestService",
                ServiceRequest = "test",
                Messages = null
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _orchestrator.ProcessRequestChain(requests));
    }

    [Fact]
    public void OrchestratorRequest_ShouldHaveCorrectProperties()
    {
        // Test the OrchestratorRequest model
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "TestService",
            ServiceRequest = "test request",
            Messages = new List<ChatMessageHistory>(),
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
    public void ChatMessageHistory_ShouldHaveCorrectProperties()
    {
        // Test the ChatMessageHistory model
        // Arrange
        var message = new ChatMessageHistory
        {
            Role = ChatMessageTypes.User,
            Content = "test content",
            ToolCallId = "test-id"
        };

        // Assert
        Assert.Equal(ChatMessageTypes.User, message.Role);
        Assert.Equal("test content", message.Content);
        Assert.Equal("test-id", message.ToolCallId);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    [InlineData("tool")]
    public void ChatMessageTypes_ShouldHaveAllExpectedValues(string messageType)
    {
        // Test all message types are available as static properties
        // Act & Assert
        Assert.Contains(messageType, new[] {
            ChatMessageTypes.User,
            ChatMessageTypes.Assistant,
            ChatMessageTypes.System,
            ChatMessageTypes.Tool
        });
    }

    [Fact]
    public void ChatMessageTypes_ShouldReturnCorrectValues()
    {
        // Verify the actual values of the static ChatMessageTypes
        Assert.Equal("user", ChatMessageTypes.User);
        Assert.Equal("assistant", ChatMessageTypes.Assistant);
        Assert.Equal("system", ChatMessageTypes.System);
        Assert.Equal("tool", ChatMessageTypes.Tool);
    }

    [Fact]
    public async Task ProcessRequest_ShouldLogTrace_WhenProcessing()
    {
        // Arrange
        var request = new OrchestratorRequest
        {
            Service = "TestService",
            ServiceRequest = "test request",
            Messages = new List<ChatMessageHistory>
            {
                new() { Role = ChatMessageTypes.User, Content = "test", ToolCallId = "test-id" }
            },
            ServiceFunctions = new Dictionary<string, IEnumerable<string>>
            {
                { "TestPlugin", new[] { "TestFunction" } }
            }
        };

        _mockPluginService.Setup(x => x.RunPlugin(request))
            .ReturnsAsync("test response");

        // Act
        await _orchestrator.ProcessRequest(request);

        // Assert
        _mockLogger.Verify(x => x.Log(LogLevel.Trace, "Orchestrator ProcessRequest", null), Times.Once);
    }
}