using Ai.Orchestrator.Plugins.Memories.Models;

namespace Ai.Orchestrator.Plugins.Tests.Memories;

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
        Assert.Null(request.MemoryId);
        Assert.Null(request.Text);
        Assert.Null(request.Query);
        Assert.Null(request.MaxResults);
    }

    [Fact]
    public void ServiceRequest_ShouldSetProperties()
    {
        // Arrange & Act
        var request = new ServiceRequest
        {
            Method = "store",
            ToolCallId = "tool-123",
            RequestingService = "TestService",
            ConfirmationId = "conf-456",
            MemoryId = "mem-789",
            Text = "This is a memory",
            Query = "search query",
            MaxResults = 5
        };

        // Assert
        Assert.Equal("store", request.Method);
        Assert.Equal("tool-123", request.ToolCallId);
        Assert.Equal("TestService", request.RequestingService);
        Assert.Equal("conf-456", request.ConfirmationId);
        Assert.Equal("mem-789", request.MemoryId);
        Assert.Equal("This is a memory", request.Text);
        Assert.Equal("search query", request.Query);
        Assert.Equal(5, request.MaxResults);
    }

    [Fact]
    public void ServiceRequest_MaxResults_ShouldAcceptVariousValues()
    {
        // Arrange & Act
        var request1 = new ServiceRequest { MaxResults = 1 };
        var request2 = new ServiceRequest { MaxResults = 100 };
        var request3 = new ServiceRequest { MaxResults = null };

        // Assert
        Assert.Equal(1, request1.MaxResults);
        Assert.Equal(100, request2.MaxResults);
        Assert.Null(request3.MaxResults);
    }

    [Fact]
    public void ServiceRequest_Text_ShouldHandleLongStrings()
    {
        // Arrange
        var longText = new string('a', 10000);

        // Act
        var request = new ServiceRequest { Text = longText };

        // Assert
        Assert.Equal(10000, request.Text.Length);
        Assert.Equal(longText, request.Text);
    }
}
