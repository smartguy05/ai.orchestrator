using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Exceptions;

namespace Ai.Orchestrator.Common.Extensions;

public static class OrchestratorRequestExtensions
{
    public static OrchestratorRequest ReturnNewOrchestratorRequest(this OrchestratorRequest request, string requestingService, object data)
    {
        if (request.Messages is not null && request.Messages.Any())
        {
            var chatMessages = request.Messages.ToList();
            if (chatMessages.Any())
            {
                chatMessages.Add(new ChatMessageHistory
                {
                    Role = ChatMessageTypes.Tool,
                    Content = data is string ? data : JsonSerializer.Serialize(data),
                    ToolCallId = request.ToolCallId
                });
                return new OrchestratorRequest
                {
                    Service = requestingService,
                    ServiceRequest = null,
                    ToolCallId = request.ToolCallId,
                    ServiceFunctions = request.ServiceFunctions,
                    Messages = chatMessages
                };
            }
        }

        throw new OrchestratorException("Unable to form new Orchestrator request");
    }
}