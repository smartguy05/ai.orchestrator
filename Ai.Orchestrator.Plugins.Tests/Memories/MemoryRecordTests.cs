using Ai.Orchestrator.Plugins.Memories.Models;

namespace Ai.Orchestrator.Plugins.Tests.Memories;

public class MemoryRecordTests
{
    [Fact]
    public void MemoryRecord_ShouldInitializeWithRequiredParameters()
    {
        // Arrange
        var id = "mem-123";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var record = new MemoryRecord(id, embedding);

        // Assert
        Assert.Equal(id, record.Id);
        Assert.Equal(embedding, record.Embedding);
        Assert.Null(record.Text);
        Assert.NotNull(record.Metadata);
        Assert.Empty(record.Metadata);
    }

    [Fact]
    public void MemoryRecord_ShouldInitializeWithAllParameters()
    {
        // Arrange
        var id = "mem-123";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var text = "Sample memory text";
        var metadata = new Dictionary<string, object>
        {
            { "category", "test" },
            { "priority", 1 }
        };

        // Act
        var record = new MemoryRecord(id, embedding, text, metadata);

        // Assert
        Assert.Equal(id, record.Id);
        Assert.Equal(embedding, record.Embedding);
        Assert.Equal(text, record.Text);
        Assert.NotNull(record.Metadata);
        Assert.Equal(3, record.Metadata.Count); // includes text_content
        Assert.Equal("test", record.Metadata["category"]);
        Assert.Equal(1, record.Metadata["priority"]);
        Assert.Equal(text, record.Metadata["text_content"]);
    }

    [Fact]
    public void MemoryRecord_ShouldAddTextContentToMetadata_WhenTextProvided()
    {
        // Arrange
        var id = "mem-123";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var text = "Sample memory text";

        // Act
        var record = new MemoryRecord(id, embedding, text);

        // Assert
        Assert.Contains("text_content", record.Metadata.Keys);
        Assert.Equal(text, record.Metadata["text_content"]);
    }

    [Fact]
    public void MemoryRecord_ShouldNotOverwriteTextContentMetadata_WhenAlreadyPresent()
    {
        // Arrange
        var id = "mem-123";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var text = "Sample memory text";
        var metadata = new Dictionary<string, object>
        {
            { "text_content", "Original content" }
        };

        // Act
        var record = new MemoryRecord(id, embedding, text, metadata);

        // Assert
        Assert.Equal("Original content", record.Metadata["text_content"]);
    }

    [Fact]
    public void MemoryRecord_ShouldThrowException_WhenIdIsNull()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MemoryRecord(null, embedding));
    }

    [Fact]
    public void MemoryRecord_ShouldAcceptNullEmbedding()
    {
        // Arrange & Act
        var record = new MemoryRecord("mem-123", null);

        // Assert
        Assert.Equal("mem-123", record.Id);
        Assert.Null(record.Embedding);
    }

    [Fact]
    public void MemoryRecord_ShouldHandleEmptyText()
    {
        // Arrange
        var id = "mem-123";
        var embedding = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
        var text = "";

        // Act
        var record = new MemoryRecord(id, embedding, text);

        // Assert
        Assert.Equal("", record.Text);
        Assert.DoesNotContain("text_content", record.Metadata.Keys);
    }

    [Fact]
    public void MemoryRecord_ShouldInitializeEmptyMetadata_WhenNullMetadataProvided()
    {
        // Arrange & Act
        var record = new MemoryRecord("mem-123", null, null, null);

        // Assert
        Assert.NotNull(record.Metadata);
        Assert.Empty(record.Metadata);
    }
}
