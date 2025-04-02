using System.Text.Json;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Configuration;
using StackExchange.Redis;

namespace Ai.Orchestrator.Models;

public static class MessageCache
{
    private static string _redisConversationSubject;
    private static ConnectionMultiplexer _redisConnection;

    public static void Init()
    {
        if (_redisConnection is null || _redisConversationSubject is null)
        {
            var config = new Config();
            _redisConversationSubject = config.RedisConversationSubject;
            _redisConnection = ConnectionMultiplexer.Connect(config.RedisConnectionString);   
        }
    }
    
    public static async Task<List<ChatMessageHistory>> GetCachedMessages(string conversationId)
    {
        Init();
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{_redisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            return JsonSerializer.Deserialize<List<ChatMessageHistory>>(cachedMessagesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return new List<ChatMessageHistory>();
    }
    
    public static async Task SaveCachedMessages(string conversationId, List<ChatMessageHistory> messages)
    {
        Init();
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{_redisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            await database.KeyDeleteAsync($"{_redisConversationSubject}-{conversationId}");
        }

        messages = messages.Distinct().ToList();
        messages = TrimOldMessages(messages);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var messagesJson = JsonSerializer.Serialize(messages, options);
        await database.StringSetAsync($"{_redisConversationSubject}-{conversationId}", messagesJson, TimeSpan.FromMinutes(10));
    }

    public static async Task<string> CreateNewPromptFromLastUserMessage(string conversationId, string systemPrompt, List<ChatMessageHistory> messages)
    {
        Init();
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new Exception("ConversationId cannot be null or empty");
        }
        
        messages ??= await GetCachedMessages(conversationId);
        if (!messages.Any())
        {
            throw new Exception("Unable to create new prompt. User messages are missing from messaging cache");
        }

        var userRequest = messages.LastOrDefault(l => l.Role.ToLower() == "user");
        if (userRequest is null)
        {
            throw new Exception("Unable to create new prompt. User request is missing from messaging");
        }

        var userRequestIndex = messages.IndexOf(userRequest);
        var latestMessages = messages.Skip(userRequestIndex).ToList();

        var newMessages = new List<ChatMessageHistory>
        {
            new()
            {
                Role = "system",
                Content = systemPrompt
            }
        };
        newMessages.AddRange(latestMessages);
        
        conversationId = Guid.NewGuid().ToString();
            
        await SaveCachedMessages(conversationId, newMessages);

        return conversationId;
    }
    
    public static async Task ClearMessageCache(string conversationId)
    {
        Init();
        var database = _redisConnection.GetDatabase();
        await database.KeyDeleteAsync($"{_redisConversationSubject}-{conversationId}");
    }
    
    private static List<ChatMessageHistory> TrimOldMessages(List<ChatMessageHistory> messages)
    {
        var lastUserMessage = messages.FindLastIndex(0, f => f.Role.ToLower() == "user");
        if (lastUserMessage < 0)
        {
            return messages;
        }
        var index = 0;
        
        while (index < lastUserMessage)
        {
            messages[index].ToolCalls = null;
        }

        return messages;
    }
}