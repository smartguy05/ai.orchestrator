using System.Text.Json;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Exceptions;

namespace Ai.Orchestrator.Models.Extensions;

public static class OrchestratorRequestExtensions
{
    public static async  Task<OrchestratorRequest> ReturnNewOrchestratorRequest(this OrchestratorRequest request, string requestingService, object data)
    {
        if (request.Messages is not null && request.Messages.Any())
        {
            var chatMessages = request.Messages.ToList();
            if (chatMessages.Any())
            {
                var confirmationId = (string)data.GetType().GetProperty("ConfirmationId")?.GetValue(data);
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                chatMessages.Add(new ChatMessageHistory
                {
                    Role = ChatMessageTypes.Tool,
                    Content = data is string ? data : JsonSerializer.Serialize(data, options),
                    ToolCallId = request.ToolCallId
                });
                string conversationId = null;
                if (request.ServiceRequest is string serviceRequestString)
                {
                    var serviceRequestJson = JsonSerializer.Deserialize<JsonElement>(serviceRequestString);
                    if (serviceRequestJson.TryGetProperty("conversationId", out var convoId))
                    {
                        conversationId = convoId.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(confirmationId))
                    {
                        var serviceRequestObject = JsonSerializer.Deserialize<Dictionary<string, object>>(serviceRequestString);
                        serviceRequestObject["confirmationId"] = confirmationId;
                        request.ServiceRequest = JsonSerializer.Serialize(serviceRequestObject);
                    }
                }

                await MessageCache.SaveCachedMessages(conversationId, chatMessages);
                return new OrchestratorRequest
                {
                    Service = requestingService,
                    ServiceRequest = request.ServiceRequest,
                    ToolCallId = request.ToolCallId,
                    ServiceFunctions = request.ServiceFunctions,
                    Messages = chatMessages
                };
            }
        }

        throw new OrchestratorException("Unable to form new Orchestrator request");
    }
}