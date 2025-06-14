using System;
using System.Collections.Generic;

namespace Ai.Orchestrator.Plugins.Memories.Models;

/// <summary>
/// Represents a memory record to be stored in ChromaDB.
/// </summary>
public class MemoryRecord
{
    /// <summary>
    /// Unique identifier for the memory.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The textual content of the memory.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The vector embedding for the memory content.
    /// </summary>
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Additional metadata associated with the memory.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public MemoryRecord(string id, ReadOnlyMemory<float>? embedding, string? text = null, Dictionary<string, object>? metadata = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Embedding = embedding;
        Text = text;
        Metadata = metadata ?? new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(Text) && !Metadata.ContainsKey("text_content"))
        {
            Metadata["text_content"] = Text;
        }
    }
}