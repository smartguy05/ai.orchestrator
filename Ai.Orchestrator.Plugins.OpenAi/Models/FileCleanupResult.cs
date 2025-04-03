namespace Ai.Orchestrator.Plugins.OpenAi.Models;

/// <summary>
/// Represents the result of a file cleanup operation
/// </summary>
public class FileCleanupResult
{
    /// <summary>
    /// Files that were successfully deleted
    /// </summary>
    public List<FileResponse> SuccessfullyDeletedFiles { get; set; } = new ();

    /// <summary>
    /// Files that could not be deleted
    /// </summary>
    public List<FileResponse> FailedDeletionFiles { get; set; } = new ();

    /// <summary>
    /// Error message if the entire operation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Gets a summary of the cleanup operation
    /// </summary>
    public string GetSummary()
    {
        return $"Cleanup Complete. " +
               $"Deleted: {SuccessfullyDeletedFiles.Count}, " +
               $"Failed: {FailedDeletionFiles.Count}, " +
               $"Error: {(string.IsNullOrEmpty(ErrorMessage) ? "None" : ErrorMessage)}";
    }
}