using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Email.Models;

public record ServiceRequest: IPluginServiceRequest
{
    public string Method { get; set; }
    public string ToolCallId { get; set; }
    public string RequestingService { get; set; }
    public string ConfirmationId { get; set; }
    public string Account { get; set; }
    public string RecipientName { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Sender { get; set; }
    public string SearchSubject { get; set; }
    public string MessageId { get; set; }
    public int MaxReturnedEmails { get; set; } = 10;
    public bool UnreadOnly { get; set; }
    public string EmailsSentAfter { get; set; }
    public string EmailsSentBefore { get; set; }
}