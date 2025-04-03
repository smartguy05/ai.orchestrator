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
            if (serviceRequest is IDictionary<string, object> dynamicRequest && 
                dynamicRequest.TryGetValue("ConversationId", out var conversationId) && 
                conversationId is string typedConversationId)
            {
                return typedConversationId;
            }
        }
        catch (Exception e)
        {
            return null;
        }
        
        return null;
    }
}