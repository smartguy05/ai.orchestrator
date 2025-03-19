using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Plugins.OpenAi.Models;
using StackExchange.Redis;

namespace Ai.Orchestrator.Plugins.OpenAi;

public class ChatService
{
    private const string RedisConversationSubject = "openai.plugin";
    private readonly ConnectionMultiplexer _redisConnection;
    
    public ChatService(ServiceConfig config)
    {
        _redisConnection = ConnectionMultiplexer.Connect(config.RedisConnectionString);
    }
    
    public async Task<object> CompleteChat(ServiceRequest request, ServiceConfig config,
        Dictionary<string, IEnumerable<string>> serviceFunctions)
    {
        request.ConversationId ??= Guid.NewGuid().ToString();
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenAiApiKey);

        var tools = config.Tools.Select(s => new ToolOption("function", s.Function)).ToList();
        var cachedMessages = await GetCachedMessages(request.ConversationId);
        var messages = cachedMessages.Concat(request.Messages ?? new List<ChatMessageHistory>()).ToList();

        if (!messages.Any())
        {
            messages = new List<ChatMessageHistory>
            {
                new () { Role = "system", Content = request.SystemPrompt},
                new () { Role = "user", Content = request.UserPrompt}
            };
        }
        else
        {
            messages = messages.Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt) && messages.All(a => a.Role != "system"))
            {
                messages.Add(new ChatMessageHistory{ Role = "system", Content = request.SystemPrompt});   
            }
            if (!string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                messages.Add(new ChatMessageHistory{ Role = "user", Content = request.UserPrompt });
            }
        }
        //normalize messages
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

        // send request
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

        var result = await httpClient.PostAsync(config.OpenAiUrl, content);

        if (!result.IsSuccessStatusCode)
        {
            var errorContent = await result.Content.ReadAsStringAsync();
            throw new Exception($"HTTP Error: {result.StatusCode}\nResponse Content: {errorContent}");
        }
        
        var choice = result.Content.ReadFromJsonAsync<ChatCompletionResponse>().Result.Choices.First();
        
        switch (choice.FinishReason)
        {
            case ChatFinishReasons.Stop:
            {
                var response = choice.Message.GetProperty("content").GetString(); 
                messages.Add(new ()
                {
                    Role = ChatMessageTypes.Assistant,
                    Content = response,
                } );
                await SaveCachedMessages(request.ConversationId, messages);
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
                    foreach (var toolCall in toolCallsResponse.ToolCalls)
                    {
                        messages.Add(new ()
                        {
                            Role = ChatMessageTypes.Assistant,
                            ToolCallId = toolCall.Id,
                            Content = toolCall.Function,
                            ToolCalls = toolCallsResponse.ToolCalls,
                            Name = toolCall.Function.Name
                        } );
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
                    
                    await SaveCachedMessages(request.ConversationId, messages);

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
    
    private async Task<List<ChatMessageHistory>> GetCachedMessages(string conversationId)
    {
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{RedisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            return JsonSerializer.Deserialize<List<ChatMessageHistory>>(cachedMessagesJson);
        }

        return new List<ChatMessageHistory>();
    }
    
    private async Task SaveCachedMessages(string conversationId, List<ChatMessageHistory> messages)
    {
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{RedisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            await database.KeyDeleteAsync($"{RedisConversationSubject}-{conversationId}");
        }

        messages = messages.Distinct().ToList();
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var messagesJson = JsonSerializer.Serialize(messages, options);
        await database.StringSetAsync($"{RedisConversationSubject}-{conversationId}", messagesJson);
    }
}