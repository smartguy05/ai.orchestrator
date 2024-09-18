
using System.Collections.Generic;

namespace Ai.Orchestrator.Models.Dto;

public record WebhookChain
{
    public IEnumerable<ChainRequest> Request { get; set; }
}

public record ChainRequest {
    public int Order { get; set; }
    public Webhook.Webhook Request { get; set; }
}