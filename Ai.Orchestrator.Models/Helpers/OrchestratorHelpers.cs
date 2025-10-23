using Ai.Orchestrator.Models.Extensions;

namespace Ai.Orchestrator.Models.Helpers;

public static class OrchestratorHelpers
{
    public static async Task<OrchestratorRequest> ForkOpenAiRequestFromMessages(ForkOpenAiRequest forkRequest)
    {
        var conversationId = await MessageCache.CreateNewPromptFromLastUserMessage(forkRequest.ConversationId, forkRequest.SystemPrompt, forkRequest.Messages);
        var messages = await MessageCache.GetCachedMessages(conversationId);
        
        var serviceRequest = new
        {
            forkRequest.SystemPrompt,
            UserPrompt = (string)null,
            forkRequest.Model,
            ConversationId = conversationId
        };
        var request = new OrchestratorRequest
        {
            Service = "Ai.Orchestrator.Plugins.OpenAI",
            ServiceRequest = serviceRequest,
            Messages = messages,
            ToolCallId = messages.Last().ToolCallId
        };

        return await request.ReturnNewOrchestratorRequest(forkRequest.RequestingService, forkRequest.ToolResponse);
    }

    public static string GetRequestConversationId(object serviceRequest)
    {
        try
        {
            if (serviceRequest is IDictionary<string, object> dynamicRequest)
            {
                // Try exact match first
                if (dynamicRequest.TryGetValue("ConversationId", out var conversationId) &&
                    conversationId is string typedConversationId)
                {
                    return typedConversationId;
                }

                // Try case-insensitive match
                var key = dynamicRequest.Keys.FirstOrDefault(k =>
                    string.Equals(k, "ConversationId", StringComparison.OrdinalIgnoreCase));

                if (key != null && dynamicRequest[key] is string caseInsensitiveId)
                {
                    return caseInsensitiveId;
                }
            }
        }
        catch (Exception e)
        {
            return null;
        }

        return null;
    }
}