using System.Text;
using System.Text.Json;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.Webhook.Models;

namespace Ai.Orchestrator.Plugins.Webhook;

public class WebhookCommand: CommandBase<ServiceRequest,ServiceConfig>
{
    public override string Name => "Webhook";
    public override string Description  => "Send a Webhook request";

    public override async Task<object> DoWork(ServiceRequest serviceRequest, ServiceConfig config, IEnumerable<ToolCall> enumerableToolCalls)
    {
        var webhook = config.Webhooks.FirstOrDefault(f =>
            string.Equals(f.Name, serviceRequest.WebhookName, StringComparison.InvariantCultureIgnoreCase));

        if (webhook is not null)
        {
            var httpClient = new HttpClient();
            var content = new
            {
                serviceRequest.Value
            };
            var json = JsonSerializer.Serialize(content);
            StringContent sContent = new StringContent(json, Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(new Uri(webhook.Url), sContent);
        }

        return false;
    }
}