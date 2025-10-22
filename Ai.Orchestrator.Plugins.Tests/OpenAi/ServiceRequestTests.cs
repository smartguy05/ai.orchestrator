using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Plugins.OpenAi.Models;

namespace Ai.Orchestrator.Plugins.Tests.OpenAi;

public class ServiceRequestTests
{
    [Fact]
    public void ServiceRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new ServiceRequest();

        // Assert
        Assert.Equal(0.7, request.Temperature);
        Assert.Null(request.SystemPrompt);
        Assert.Null(request.UserPrompt);
        Assert.Null(request.Model);
        Assert.Null(request.Agent);
        Assert.Null(request.Messages);
        Assert.Null(request.ConversationId);
        Assert.Null(request.Photo);
    }

    [Fact]
    public void ServiceRequest_ShouldSetProperties()
    {
        // Arrange
        var messages = new List<ChatMessageHistory>
        {
            new() { Role = ChatMessageTypes.User, Content = "Hello" }
        };

        // Act
        var request = new ServiceRequest
        {
            Method = "chat",
            ToolCallId = "tool-123",
            RequestingService = "TestService",
            ConfirmationId = "conf-456",
            SystemPrompt = "You are a helpful assistant",
            UserPrompt = "Hello, how are you?",
            Model = "gpt-4",
            Agent = "TestAgent",
            Messages = messages,
            Temperature = 0.5,
            ConversationId = "conv-789",
            Photo = "base64encodedimage"
        };

        // Assert
        Assert.Equal("chat", request.Method);
        Assert.Equal("tool-123", request.ToolCallId);
        Assert.Equal("TestService", request.RequestingService);
        Assert.Equal("conf-456", request.ConfirmationId);
        Assert.Equal("You are a helpful assistant", request.SystemPrompt);
        Assert.Equal("Hello, how are you?", request.UserPrompt);
        Assert.Equal("gpt-4", request.Model);
        Assert.Equal("TestAgent", request.Agent);
        Assert.Equal(messages, request.Messages);
        Assert.Equal(0.5, request.Temperature);
        Assert.Equal("conv-789", request.ConversationId);
        Assert.Equal("base64encodedimage", request.Photo);
    }

    [Fact]
    public void ServiceRequest_Temperature_ShouldAcceptValidRange()
    {
        // Arrange & Act
        var request1 = new ServiceRequest { Temperature = 0.0 };
        var request2 = new ServiceRequest { Temperature = 1.0 };
        var request3 = new ServiceRequest { Temperature = 0.7 };

        // Assert
        Assert.Equal(0.0, request1.Temperature);
        Assert.Equal(1.0, request2.Temperature);
        Assert.Equal(0.7, request3.Temperature);
    }

    [Fact]
    public void ServiceRequest_Messages_ShouldSupportMultipleMessages()
    {
        // Arrange
        var messages = new List<ChatMessageHistory>
        {
            new() { Role = ChatMessageTypes.System, Content = "You are helpful" },
            new() { Role = ChatMessageTypes.User, Content = "Hello" },
            new() { Role = ChatMessageTypes.Assistant, Content = "Hi there!" }
        };

        // Act
        var request = new ServiceRequest { Messages = messages };

        // Assert
        Assert.Equal(3, request.Messages.Count());
        Assert.Equal(ChatMessageTypes.System, request.Messages.First().Role);
        Assert.Equal(ChatMessageTypes.Assistant, request.Messages.Last().Role);
    }
}
