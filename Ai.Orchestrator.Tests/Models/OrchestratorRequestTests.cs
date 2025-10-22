using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;

namespace Ai.Orchestrator.Tests.Models;

public class OrchestratorRequestTests
{
    [Fact]
    public void OrchestratorRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new OrchestratorRequest();

        // Assert
        Assert.NotNull(request.Messages);
        Assert.Empty(request.Messages);
        Assert.Null(request.Service);
        Assert.Null(request.ServiceRequest);
        Assert.Null(request.ToolCallId);
        Assert.Null(request.ServiceFunctions);
    }

    [Fact]
    public void OrchestratorRequest_ShouldSetProperties()
    {
        // Arrange
        var service = "TestService";
        var serviceRequest = new { data = "test" };
        var toolCallId = "tool_123";
        var messages = new List<ChatMessageHistory>
        {
            new() { Role = ChatMessageTypes.User, Content = "Hello" }
        };
        var serviceFunctions = new Dictionary<string, IEnumerable<string>>
        {
            { "service1", new[] { "func1", "func2" } }
        };

        // Act
        var request = new OrchestratorRequest
        {
            Service = service,
            ServiceRequest = serviceRequest,
            ToolCallId = toolCallId,
            Messages = messages,
            ServiceFunctions = serviceFunctions
        };

        // Assert
        Assert.Equal(service, request.Service);
        Assert.Equal(serviceRequest, request.ServiceRequest);
        Assert.Equal(toolCallId, request.ToolCallId);
        Assert.Equal(messages, request.Messages);
        Assert.Equal(serviceFunctions, request.ServiceFunctions);
    }

    [Fact]
    public void OrchestratorRequest_Messages_ShouldBeModifiable()
    {
        // Arrange
        var request = new OrchestratorRequest();
        var message = new ChatMessageHistory { Role = ChatMessageTypes.System, Content = "System message" };

        // Act
        request.Messages.Add(message);

        // Assert
        Assert.Single(request.Messages);
        Assert.Equal(message, request.Messages[0]);
    }
}
