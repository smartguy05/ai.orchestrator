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
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{_redisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            return JsonSerializer.Deserialize<List<ChatMessageHistory>>(cachedMessagesJson);
        }

        return new List<ChatMessageHistory>();
    }
    
    public static async Task SaveCachedMessages(string conversationId, List<ChatMessageHistory> messages)
    {
        var database = _redisConnection.GetDatabase();
        var cachedMessagesJson = await database.StringGetAsync($"{_redisConversationSubject}-{conversationId}");

        if (cachedMessagesJson.HasValue)
        {
            await database.KeyDeleteAsync($"{_redisConversationSubject}-{conversationId}");
        }

        messages = messages.Distinct().ToList();
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var messagesJson = JsonSerializer.Serialize(messages, options);
        await database.StringSetAsync($"{_redisConversationSubject}-{conversationId}", messagesJson);
    }
}