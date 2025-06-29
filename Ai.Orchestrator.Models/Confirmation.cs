namespace Ai.Orchestrator.Models;

public class Confirmation
{
    public string ConfirmationMessage { get; set; }
    public Dictionary<string, bool> Options { get; set; }
    public string Content { get; set; }
    public Guid? Id { get; set; }
    public DateTime? Expiration { get; set; }
}