namespace Ai.Orchestrator.Plugins.Email.Models;

public record ServiceRequest
{
    public string Method { get; set; }
    public string Account { get; set; }
    public string RecipientName { get; set; }
    public string Destination { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Sender { get; set; }
    public string SearchSubject { get; set; }
}