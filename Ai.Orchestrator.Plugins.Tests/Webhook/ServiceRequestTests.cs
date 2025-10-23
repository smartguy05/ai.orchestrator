using Ai.Orchestrator.Plugins.Webhook.Models;

namespace Ai.Orchestrator.Plugins.Tests.Webhook;

public class ServiceRequestTests
{
    [Fact]
    public void ServiceRequest_ShouldInitializeWithNullValues()
    {
        // Arrange & Act
        var request = new ServiceRequest();

        // Assert
        Assert.Null(request.Method);
        Assert.Null(request.ToolCallId);
        Assert.Null(request.RequestingService);
        Assert.Null(request.ConfirmationId);
        Assert.Null(request.WebhookName);
        Assert.Null(request.Value);
    }

    [Fact]
    public void ServiceRequest_ShouldSetProperties()
    {
        // Arrange & Act
        var request = new ServiceRequest
        {
            Method = "send",
            ToolCallId = "tool-123",
            RequestingService = "TestService",
            ConfirmationId = "conf-456",
            WebhookName = "MyWebhook",
            Value = "Test Value"
        };

        // Assert
        Assert.Equal("send", request.Method);
        Assert.Equal("tool-123", request.ToolCallId);
        Assert.Equal("TestService", request.RequestingService);
        Assert.Equal("conf-456", request.ConfirmationId);
        Assert.Equal("MyWebhook", request.WebhookName);
        Assert.Equal("Test Value", request.Value);
    }

    [Fact]
    public void ServiceRequest_ShouldHandleEmptyStrings()
    {
        // Arrange & Act
        var request = new ServiceRequest
        {
            WebhookName = "",
            Value = ""
        };

        // Assert
        Assert.Equal("", request.WebhookName);
        Assert.Equal("", request.Value);
    }

    [Fact]
    public void ServiceRequest_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var request1 = new ServiceRequest
        {
            WebhookName = "TestWebhook",
            Value = "TestValue"
        };

        var request2 = new ServiceRequest
        {
            WebhookName = "TestWebhook",
            Value = "TestValue"
        };

        // Act & Assert
        Assert.Equal(request1, request2);
    }

    [Fact]
    public void ServiceRequest_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new ServiceRequest
        {
            WebhookName = "TestWebhook1",
            Value = "TestValue"
        };

        var request2 = new ServiceRequest
        {
            WebhookName = "TestWebhook2",
            Value = "TestValue"
        };

        // Act & Assert
        Assert.NotEqual(request1, request2);
    }
}
