using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Interfaces;
using StackExchange.Redis;

namespace Ai.Orchestrator.Services;

public class TaskScheduler: ITaskScheduler
{
    private readonly IOrchestrator _orchestrator;
    private readonly ILoggingService _logger;
    private static string _redisConversationSubject;
    private static ConnectionMultiplexer _redisConnection;

    public TaskScheduler(IOrchestrator orchestrator, ILoggingService logger)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        if (_redisConnection is null || _redisConversationSubject is null)
        {
            var config = new Config();
            _redisConversationSubject = "scheduled_task";
            _redisConnection = ConnectionMultiplexer.Connect(config.RedisConnectionString, x=> x.AllowAdmin = true);   
        }
        StartListeningForExpirationEvents();
    }
    
    public async Task AddScheduledTask(ScheduledTask task)
    {
        if (task == null || string.IsNullOrEmpty(task.Name))
        {
            throw new ArgumentException("ScheduledTask must not be null and must have a valid Id.");
        }

        var database = _redisConnection.GetDatabase();
        var redisKey = $"{_redisConversationSubject}{task.Name}";
        var taskJson = JsonSerializer.Serialize(task);
        var backupKey = $"{redisKey}_backup";

        // Store the backup copy without expiration
        await database.StringSetAsync(backupKey, taskJson);

        TimeSpan expirationTimeSpan;
        if (task.Timeout.HasValue)
        {
            expirationTimeSpan = TimeSpan.FromSeconds(task.Timeout.Value);
        }
        else
        {
            expirationTimeSpan = DateTime.Parse(task.Expiration) - DateTimeOffset.UtcNow;
        }

        if (expirationTimeSpan <= TimeSpan.Zero)
        {
            expirationTimeSpan *= -1;
        }
        if (expirationTimeSpan <= TimeSpan.Zero)
        {
            await _logger.LogWarning($"Warning: Task {task.Name} has an expiration in the past or present. Not caching.");
            return;
        }

        await database.StringSetAsync(redisKey, taskJson, expirationTimeSpan);

        await _logger.LogInformation($"Task {task.Name} added to Redis with key {redisKey} and expiration {task.Expiration}.");
    }
    
    private void StartListeningForExpirationEvents()
    {
        var subscriber = _redisConnection.GetSubscriber();
        
        // Subscribe to keyspace notifications for expired events
        subscriber.Subscribe("__keyevent@0__:expired", async (channel, key) => 
        {
            var keyString = key.ToString();
            
            // Only process keys with our prefix
            if (keyString.StartsWith(_redisConversationSubject))
            {
                await HandleExpiredTask(keyString);
            }
        });
        
        // Ensure keyspace notifications are enabled for expired events
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        server.ConfigSet("notify-keyspace-events", "Ex");
        
        _logger.LogInformation("Started listening for Redis expiration events.").ConfigureAwait(false);
    }
    
    private async Task HandleExpiredTask(string key)
    {
        try
        {
            // Get the task data from backup storage
            // Redis automatically removes expired keys, so we need to store a backup
            var database = _redisConnection.GetDatabase();
            var backupKey = $"{key}_backup";
            var taskJson = await database.StringGetAsync(backupKey);
            
            if (taskJson.IsNullOrEmpty)
            {
                await _logger.LogWarning($"No backup found for expired task: {key}");
                return;
            }
            
            // Parse the task
            var task = JsonSerializer.Deserialize<ScheduledTask>(taskJson);
            if (task == null)
            {
                await _logger.LogWarning($"Failed to deserialize task: {key}");
                return;
            }

            // If ServiceRequest was deserialized as a string (containing JSON), parse it into a JsonElement
            if (task.OrchestratorRequest?.ServiceRequest is string serviceRequestString)
            {
                await _logger.LogInformation($"[TaskScheduler.HandleExpiredTask] OrchestratorRequest.ServiceRequest is a string: {serviceRequestString}");
                try
                {
                    if (!string.IsNullOrWhiteSpace(serviceRequestString) && !serviceRequestString.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        using (JsonDocument doc = JsonDocument.Parse(serviceRequestString))
                        {
                            task.OrchestratorRequest.ServiceRequest = doc.RootElement.Clone(); // Clone to own the data
                            await _logger.LogInformation($"[TaskScheduler.HandleExpiredTask] Successfully parsed ServiceRequest string into JsonElement. New type: {task.OrchestratorRequest.ServiceRequest.GetType().FullName}");
                        }
                    }
                    else
                    {
                        await _logger.LogWarning(
                            $"[TaskScheduler.HandleExpiredTask] ServiceRequest string is null, empty, or literally 'null'. Setting ServiceRequest to null.");
                        task.OrchestratorRequest.ServiceRequest = null;
                    }
                }
                catch (JsonException ex)
                {
                    await _logger.LogError($"[TaskScheduler.HandleExpiredTask] Failed to parse ServiceRequest string into JsonDocument: {ex.Message}. Leaving ServiceRequest as string.");
                }
            }
            
            await _logger.LogInformation($"Processing expired task: {task.Name}");
            
            // Process the request
            if (task.OrchestratorRequest != null)
            {
                await _orchestrator.ProcessRequest(task.OrchestratorRequest);
                await _logger.LogInformation($"Task {task.Name} processed successfully.");
                
                // If recurring, schedule the next occurrence
                if (task.IsRecurring)
                {
                    var nextExecution = DateTime.Now.AddMinutes(task.Timeout ?? 0);
                    var newTask = new ScheduledTask
                    {
                        Name = task.Name,
                        OrchestratorRequest = task.OrchestratorRequest,
                        Expiration = nextExecution.ToShortDateString(),
                        IsRecurring = true
                    };
                        
                    await AddScheduledTask(newTask);
                    await _logger.LogInformation($"Recurring task {task.Name} rescheduled for {newTask.Expiration}");
                }
            }
            
            // Clean up the backup
            await database.KeyDeleteAsync(backupKey);
        }
        catch (Exception ex)
        {
            await _logger.LogError($"Error handling expired task {key}: {ex.Message}");
        }
    }
}