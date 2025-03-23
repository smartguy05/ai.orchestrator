using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Plugins.OpenAi.Models;
using Ai.Orchestrator.Services;

namespace Ai.Orchestrator.Plugins.OpenAi;

public class ChatService
{
    public ChatService()
    {
        MessageCache.Init();
    }
    
    public async Task<object> CompleteChat(ServiceRequest request, ServiceConfig config,
        Dictionary<string, IEnumerable<string>> serviceFunctions)
    {
        request.ConversationId ??= Guid.NewGuid().ToString();
        
        var tools = config.Tools.Select(s => new ToolOption("function", s.Function)).ToList();
        var messages = await GetMessages(request);
        var result = await SendRequest(config, request, messages, tools);

        if (!result.IsSuccessStatusCode)
        {
            var errorContent = await result.Content.ReadAsStringAsync();
            throw new Exception($"HTTP Error: {result.StatusCode}\nResponse Content: {errorContent}");
        }
        
        var choice = result.Content.ReadFromJsonAsync<ChatCompletionResponse>().Result.Choices.First();

        return await ProcessResponse(choice, request, messages, serviceFunctions);
    }
    
    private static void NormalizeMessages(ref List<ChatMessageHistory> messages)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            if (messages[i].Content is not null && messages[i].Content is not string)
            {
                var newMessage = new ChatMessageHistory
                {
                    Role = messages[i].Role,
                    Content = JsonSerializer.Serialize(messages[i].Content),
                    ToolCallId = messages[i].ToolCallId,
                    Id = messages[i].Id,
                    ToolCalls = messages[i].ToolCalls
                };
                messages[i] = newMessage;
            }
        }
    }

    private async Task<List<ChatMessageHistory>> GetMessages(ServiceRequest request)
    {
        var cachedMessages = await MessageCache.GetCachedMessages(request.ConversationId);
        var messages = cachedMessages ?? request.Messages.ToList(); // cachedMessages.Concat(request.Messages ?? new List<ChatMessageHistory>()).ToList();
        var systemPrompt = AddContext(request.SystemPrompt);

        if (!messages.Any())
        {
            messages = new List<ChatMessageHistory>
            {
                new () { Role = "system", Content = systemPrompt},
                new () { Role = "user", Content = request.UserPrompt}
            };
        }
        else
        {
            messages = messages.Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(systemPrompt) && messages.All(a => a.Role != "system"))
            {
                messages.Add(new ChatMessageHistory{ Role = "system", Content = systemPrompt});   
            }
            if (!string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                messages.Add(new ChatMessageHistory{ Role = "user", Content = request.UserPrompt });
            }
        }

        NormalizeMessages(ref messages);
        return messages;
    }

    private async Task<object> ProcessResponse(Choice choice, ServiceRequest request, List<ChatMessageHistory> messages, Dictionary<string, IEnumerable<string>> serviceFunctions)
    {
        switch (choice.FinishReason)
        {
            case ChatFinishReasons.Stop:
            {
                var response = choice.Message.GetProperty("content").GetString(); 
                messages.Add(new ()
                {
                    Role = ChatMessageTypes.Assistant,
                    Content = response,
                });
                await MessageCache.SaveCachedMessages(request.ConversationId, messages);
                return new {
                    request.ConversationId,
                    Result = response
                };
            }
            case ChatFinishReasons.ToolCalls:
            {
                var requests = new List<OrchestratorRequest>();
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(choice.Message.ToString());
                    var root = doc.RootElement;
                    var toolCallsResponse = root.Deserialize<ToolCallsResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    messages.Add(new ()
                    {
                        Role = ChatMessageTypes.Assistant,
                        ToolCallId = null,
                        Content = null,
                        ToolCalls = toolCallsResponse.ToolCalls,
                        Name = null
                    });
                    foreach (var toolCall in toolCallsResponse.ToolCalls)
                    {
                        var serviceFunction = serviceFunctions
                            .FirstOrDefault(w => w.Value.ToList().Contains(toolCall.Function.Name));
                        using var argumentsJson = JsonDocument.Parse(toolCall.Function.Arguments);
                    
                        var serviceRequest = new Dictionary<string, object>();
                        serviceRequest.Add("method", toolCall.Function.Name);
                        serviceRequest.Add("requestingService", "Ai.Orchestrator.Plugins.OpenAi");
                        serviceRequest.Add("conversationId", request.ConversationId);
                        foreach (JsonProperty property in argumentsJson.RootElement.EnumerateObject())
                        {
                            serviceRequest.Add(char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1)
                                , property.Value);
                        }
                        var stringified = JsonSerializer.Serialize(serviceRequest);
                    
                        Console.WriteLine("Tool call required");
                        Console.WriteLine(JsonSerializer.Serialize(messages));

                        requests.Add(new OrchestratorRequest
                        {
                            Service = serviceFunction.Key,
                            ServiceRequest = stringified,
                            ToolCallId = toolCall.Id,
                            ServiceFunctions = serviceFunctions,
                            Messages = messages
                        });
                    }
                    
                    await MessageCache.SaveCachedMessages(request.ConversationId, messages);

                    if (requests.Count == 1)
                    {
                        return requests.First();
                    }

                    return requests;
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occurred processing return value: {JsonSerializer.Serialize(choice)}", e);
                }
            }
            case ChatFinishReasons.Length:
            {
                throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");
            }
            case ChatFinishReasons.ContentFilter:
            {
                throw new NotImplementedException("Omitted content due to a content filter flag.");
            }
            case ChatFinishReasons.FunctionCall:
            {
                throw new NotImplementedException("Deprecated in favor of tool calls.");
            }
            default:
            {
                throw new NotImplementedException(choice.FinishReason);
            }
        }
    }

    private async Task<HttpResponseMessage> SendRequest(ServiceConfig config, ServiceRequest request, List<ChatMessageHistory> messages, List<ToolOption> tools)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenAiApiKey);

        var oAiRequest = new ApiRequest
        {
            Model = request.Model,
            Messages = messages,
            Temperature = request.Temperature,
            Tools = tools
        };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var body = JsonSerializer.Serialize(oAiRequest, options);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        return await httpClient.PostAsync(config.OpenAiUrl, content);
    }
    
    private string AddContext(string systemPrompt)
    {
        if (!string.IsNullOrEmpty(systemPrompt) && !systemPrompt.Contains("<context>"))
        {
            systemPrompt += $@"
                <context>{Environment.NewLine}
                    Current Date: {DateTime.Now.ToShortDateString()} {Environment.NewLine}
                    Current Time: {DateTime.Now.ToShortTimeString()} {Environment.NewLine}
                    Timezone: {TimeZoneInfo.Local.Id} {Environment.NewLine}
                </context>
            ";
        }

        return systemPrompt;
    }
}