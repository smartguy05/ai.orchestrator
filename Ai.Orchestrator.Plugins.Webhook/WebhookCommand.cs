using System.Text;
using System.Text.Json;
using Ai.Orchestrator.Common.Extensions;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.Webhook.Models;

namespace Ai.Orchestrator.Plugins.Webhook;

public class WebhookCommand: ICommand
{
    public string Name => "Webhook";
    public string Description  => "Send a Webhook request";
    
    public async Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls)
    {
        var serviceRequest = request.ServiceRequest.GetServiceRequest<ServiceRequest>();
        var config = configString.ReadConfig<ServiceConfig>();

        if (serviceRequest is null)
        {
            throw new Exception("Unable to read webhook service request");
        }
        
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
            var result = await httpClient.PostAsync(new Uri(webhook.Url), sContent);
            return result;
        }

        return false;
    }
}