using Ai.Orchestrator.Models.Helpers;

namespace Ai.Orchestrator.Tests.Helpers;

public class OrchestratorHelpersTests
{
    [Fact]
    public void GetRequestConversationId_ShouldReturnConversationId_WhenPresent()
    {
        // Arrange
        var serviceRequest = new Dictionary<string, object>
        {
            { "ConversationId", "test-conversation-123" }
        };

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Equal("test-conversation-123", result);
    }

    [Fact]
    public void GetRequestConversationId_ShouldReturnNull_WhenConversationIdNotPresent()
    {
        // Arrange
        var serviceRequest = new Dictionary<string, object>
        {
            { "SomeOtherKey", "value" }
        };

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequestConversationId_ShouldReturnNull_WhenConversationIdIsNotString()
    {
        // Arrange
        var serviceRequest = new Dictionary<string, object>
        {
            { "ConversationId", 12345 }
        };

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequestConversationId_ShouldReturnNull_WhenRequestIsNull()
    {
        // Arrange
        object serviceRequest = null;

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequestConversationId_ShouldReturnNull_WhenRequestIsNotDictionary()
    {
        // Arrange
        var serviceRequest = "not a dictionary";

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequestConversationId_ShouldHandleCaseInsensitiveKeys()
    {
        // Arrange
        var serviceRequest = new Dictionary<string, object>
        {
            { "conversationId", "test-conversation-456" }
        };

        // Act
        var result = OrchestratorHelpers.GetRequestConversationId(serviceRequest);

        // Assert
        Assert.Equal("test-conversation-456", result);
    }
}
