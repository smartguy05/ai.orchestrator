using Ai.Orchestrator.Plugins.OpenAi.Models;

namespace Ai.Orchestrator.Plugins.Tests.OpenAi;

public class ChatCompletionResponseTests
{
    [Fact]
    public void ChatCompletionResponse_ShouldInitialize()
    {
        // Arrange & Act
        var response = new ChatCompletionResponse();

        // Assert
        Assert.Null(response.Id);
        Assert.Null(response.Object);
        Assert.Equal(0, response.Created);
        Assert.Null(response.Model);
        Assert.Null(response.Usage);
        Assert.Null(response.Choices);
    }

    [Fact]
    public void ChatCompletionResponse_ShouldSetProperties()
    {
        // Arrange
        var usage = new Usage
        {
            PromptTokens = 10,
            CompletionTokens = 20,
            TotalTokens = 30
        };

        var choices = new List<Choice>
        {
            new() { Index = 0, FinishReason = ChatFinishReasons.Stop }
        };

        // Act
        var response = new ChatCompletionResponse
        {
            Id = "chatcmpl-123",
            Object = "chat.completion",
            Created = 1234567890,
            Model = "gpt-4",
            Usage = usage,
            Choices = choices
        };

        // Assert
        Assert.Equal("chatcmpl-123", response.Id);
        Assert.Equal("chat.completion", response.Object);
        Assert.Equal(1234567890, response.Created);
        Assert.Equal("gpt-4", response.Model);
        Assert.Equal(usage, response.Usage);
        Assert.Single(response.Choices);
    }

    [Fact]
    public void ChatCompletionResponse_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var response1 = new ChatCompletionResponse
        {
            Id = "chatcmpl-123",
            Model = "gpt-4"
        };

        var response2 = new ChatCompletionResponse
        {
            Id = "chatcmpl-123",
            Model = "gpt-4"
        };

        // Act & Assert
        Assert.Equal(response1, response2);
    }

    [Fact]
    public void ChatCompletionResponse_WithDifferentIds_ShouldNotBeEqual()
    {
        // Arrange
        var response1 = new ChatCompletionResponse { Id = "chatcmpl-123" };
        var response2 = new ChatCompletionResponse { Id = "chatcmpl-456" };

        // Act & Assert
        Assert.NotEqual(response1, response2);
    }
}
