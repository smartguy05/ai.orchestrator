using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public class FileResponse
{
    /// <summary>
    /// Type of the object (typically "file")
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// Unique identifier for the uploaded file
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Purpose of the file upload (e.g., "assistants", "fine-tune")
    /// </summary>
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; }

    /// <summary>
    /// Original filename of the uploaded file
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    /// Unix timestamp of file creation
    /// </summary>
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// Unix timestamp when the file expires (null if no expiration)
    /// </summary>
    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// Current processing status of the file
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Additional details about the file status
    /// </summary>
    [JsonPropertyName("status_details")]
    public string StatusDetails { get; set; }

    /// <summary>
    /// Converts CreatedAt Unix timestamp to DateTime
    /// </summary>
    public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeSeconds(CreatedAt).DateTime;

    /// <summary>
    /// Converts ExpiresAt Unix timestamp to DateTime (if not null)
    /// </summary>
    public DateTime? ExpiresDateTime => ExpiresAt.HasValue 
        ? DateTimeOffset.FromUnixTimeSeconds(ExpiresAt.Value).DateTime 
        : null;
}