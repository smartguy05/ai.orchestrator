using System.Text.Json;
using System.Text.Json.Nodes;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Configuration;
using Ai.Orchestrator.Models.Interfaces;
using StackExchange.Redis;

namespace Ai.Orchestrator.Services;

public class NotificationService : INotificationService
{
    private readonly ILoggingService _loggingService;
    private readonly IOrchestrator _orchestrator;
    private static string _redisConversationSubject;
    private static ConnectionMultiplexer _redisConnection;
    private static Config _config = new();
    private static IConfirmationPlugin _plugin;
    private Dictionary<Guid, Confirmation> _confirmations = new();
    private readonly IPluginService _pluginService;
    
    public NotificationService(IOrchestrator orchestrator, ILoggingService loggingService, IPluginService pluginService)
    {
        _pluginService = pluginService;
        _orchestrator = orchestrator;
        _loggingService = loggingService;
        if (_redisConnection is null || _redisConversationSubject is null)
        {
            var config = new Config();
            _redisConversationSubject = "confirmation";
            _redisConnection = ConnectionMultiplexer.Connect(config.RedisConnectionString, x=> x.AllowAdmin = true);   
        }

        _plugin = pluginService.GetPlugin<IConfirmationPlugin>(_config.ConfirmationPlugin);
    }

    public async Task<object> SendNotification(string message)
    {
        var confirmation = new Confirmation
        {
            Id = Guid.NewGuid(),
            ConfirmationMessage = message
        };
        var result = await SendConfirmation(confirmation, null);
        var isSuccessful = (bool?)result.GetType().GetProperty("Success")?.GetValue(result) ?? false;
        if (isSuccessful)
        {
            return new
            {
                Success = true,
                ConfirmationId = confirmation.Id.ToString()
            };    
        }
                    
        return new
        {
            Success = false
        };
    }
    
    public async Task<object> RequestConfirmation(string serviceName, Confirmation confirmation, IPluginServiceRequest serviceRequest)
    {
        if (serviceRequest is null)
        {
            await _loggingService.LogError("RequestConfirmation: serviceRequest is null");
            throw new Exception("Service Request is null");
        }
        
        confirmation.Id ??= Guid.NewGuid();
        serviceRequest.ConfirmationId = confirmation.Id.ToString();
        var request = new OrchestratorRequest
        {
            Service = serviceName,
            ServiceRequest = serviceRequest
        };
        var confirmationRequest = await ProcessRequestConfirmation(confirmation, request, _config.ConfirmationExpirationMinutes);
        var isSuccessful = (bool?)confirmationRequest.GetType().GetProperty("Success")?.GetValue(confirmationRequest) ?? false;
        if (isSuccessful)
        {
            return new
            {
                Success = true,
                ConfirmationId = confirmation.Id.ToString()
            };    
        }
                    
        return new
        {
            Success = false
        };
    }
    
    public async Task<object> Confirm(Guid confirmationId, bool confirm)
    {
        // get request
        var database = _redisConnection.GetDatabase();
        var redisKey = $"{_redisConversationSubject}_{confirmationId}";
        var requestJson = await database.StringGetAsync(redisKey);
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            var errorMessage = $"No request found for confirmation id {confirmationId}";
            await _loggingService.LogError(errorMessage);
            return new
            {
                Success = false,
                Error = errorMessage
            };
        }
        
        var confirmation = _confirmations[confirmationId];
        if (confirmation is null)
        {
            var errorMessage = $"No confirmation found for confirmation id {confirmationId}";
            await _loggingService.LogError(errorMessage);
            return new
            {
                Success = false,
                Error = errorMessage
            };
        }
        
        if (confirmation.Expiration < DateTime.Now)
        {
            await _loggingService.LogInformation($"Confirmation {confirmation.Id} expired!");
            return new
            {
                Success = false,
                Message = "Confirmation expired"
            };
        }
        
        try
        {
            if (confirm)
            {
                var orchestratorRequest = !string.IsNullOrWhiteSpace(requestJson)
                    ? JsonSerializer.Deserialize<OrchestratorRequest>(requestJson)
                    : new OrchestratorRequest();
                
                if (orchestratorRequest.ServiceRequest is JsonElement serviceRequestElement)
                {
                    var serviceRequestObject = JsonSerializer.Deserialize<JsonObject>(serviceRequestElement);
                    if (serviceRequestObject is not null)
                    {
                        serviceRequestObject["ConfirmationId"] = confirmationId;
                        orchestratorRequest.ServiceRequest = JsonSerializer.Serialize(serviceRequestObject);
                    }
                }
                
                return await _orchestrator.ProcessRequest(orchestratorRequest);
            }
        }
        catch (Exception ex)
        {
            await _loggingService.LogError($"Error handling confirmation task {redisKey}: {ex.Message}");
        }
        finally
        {
            // Clean up the backup
            await database.KeyDeleteAsync(redisKey);
            _confirmations.Remove(confirmation.Id ?? Guid.Empty);
        }

        return new
        {
            Success = false
        };
    }

    public bool DoesConfirmationExist(Guid confirmationId, out Confirmation confirmation)
    {
        if (_confirmations.TryGetValue(confirmationId, out confirmation))
        {
            if (DateTime.Now > _confirmations[confirmationId].Expiration)
            {
                _confirmations.Remove(confirmationId);
            }
            else
            {
                return true;
            }
        }
        return false;
    }
    
    private async Task<object> ProcessRequestConfirmation(Confirmation confirmation, OrchestratorRequest request, int timeoutInMinutes)
    {
        if (confirmation == null)
        {
            throw new ArgumentException("No confirmation found");
        }
        
        confirmation.Id ??= Guid.NewGuid();
        confirmation.Expiration = DateTime.Now.AddMinutes(timeoutInMinutes);
        _confirmations.TryAdd((Guid)confirmation.Id, confirmation);
        
        var database = _redisConnection.GetDatabase();
        var redisKey = $"{_redisConversationSubject}_{confirmation.Id}";
        var requestJson = JsonSerializer.Serialize(request);
        var expiration = TimeSpan.FromMinutes(_config.ConfirmationExpirationMinutes);
        
        await database.StringSetAsync(redisKey, requestJson, expiration);
        return await SendConfirmation(confirmation, request);
    }

    private async Task<object> SendConfirmation(Confirmation confirmation, OrchestratorRequest request)
    {
        if (_plugin is null)
        {
            _plugin = _pluginService.GetPlugin<IConfirmationPlugin>(_config.ConfirmationPlugin);
        }

        if (_plugin is not null)
        {
            return await _plugin.RequestConfirmation(confirmation, request);
        }

        return false;
    }
}