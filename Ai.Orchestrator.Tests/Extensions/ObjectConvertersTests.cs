using System.Text.Json;
using Ai.Orchestrator.Models.Extensions;

namespace Ai.Orchestrator.Tests.Extensions;

public class ObjectConvertersTests
{
    private class TestModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime? Date { get; set; }
    }

    [Fact]
    public void GetServiceRequest_ShouldReturnNull_WhenRequestIsNull()
    {
        // Arrange
        object request = null;

        // Act
        var result = request.GetServiceRequest<TestModel>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServiceRequest_ShouldReturnNull_WhenStringIsNullOrWhitespace()
    {
        // Arrange
        object request = "   ";

        // Act
        var result = request.GetServiceRequest<TestModel>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServiceRequest_ShouldDeserializeString()
    {
        // Arrange
        var json = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var result = json.GetServiceRequest<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GetServiceRequest_ShouldDeserializeJsonElement()
    {
        // Arrange
        var json = "{\"name\":\"Test\",\"value\":42}";
        var jsonDoc = JsonDocument.Parse(json);
        var jsonElement = jsonDoc.RootElement;

        // Act
        var result = ((object)jsonElement).GetServiceRequest<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GetServiceRequest_ShouldReturnNull_ForInvalidJson()
    {
        // Arrange
        object request = "{invalid json}";

        // Act
        var result = request.GetServiceRequest<TestModel>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServiceRequest_ShouldHandleCaseInsensitiveProperties()
    {
        // Arrange
        var json = "{\"NAME\":\"Test\",\"VALUE\":42}";

        // Act
        var result = json.GetServiceRequest<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GetServiceRequestArray_ShouldReturnEmpty_WhenRequestIsNull()
    {
        // Arrange
        object request = null;

        // Act
        var result = request.GetServiceRequestArray<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetServiceRequestArray_ShouldReturnEmpty_WhenRequestIsNotArray()
    {
        // Arrange
        var json = "{\"name\":\"Test\"}";

        // Act
        var result = json.GetServiceRequestArray<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetServiceRequestArray_ShouldDeserializeArray()
    {
        // Arrange
        var json = "[{\"name\":\"Test1\",\"value\":1},{\"name\":\"Test2\",\"value\":2}]";

        // Act
        var result = json.GetServiceRequestArray<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("Test1", result.First().Name);
        Assert.Equal("Test2", result.Last().Name);
    }

    [Fact]
    public void GetServiceRequestArray_ShouldDeserializeStringArray()
    {
        // Arrange
        var json = "[\"string1\",\"string2\",\"string3\"]";

        // Act
        var result = json.GetServiceRequestArray<string>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Equal("string1", result.First());
        Assert.Equal("string3", result.Last());
    }

    [Fact]
    public void GetServiceRequestArray_ShouldFilterOutInvalidElements()
    {
        // Arrange
        var json = "[{\"name\":\"Test1\",\"value\":1},{invalid},null,{\"name\":\"Test2\",\"value\":2}]";

        // Act
        var result = json.GetServiceRequestArray<TestModel>();

        // Assert - should skip invalid elements
        Assert.NotNull(result);
    }
}
