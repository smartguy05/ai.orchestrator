using Ai.Orchestrator.Models.Extensions;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.Memories.Models;

namespace Ai.Orchestrator.Plugins.Memories;

public class MemoryCommand: CommandBase<ServiceRequest, ServiceConfig>
{
    public override string Name => "Memories";
    public override string Description => "A plugin to save memories for Agent use";
    private static ChromaService _chromaService;

    public override async Task<object> DoWork(ServiceRequest serviceRequest, ServiceConfig config, IEnumerable<ToolCall> enumerableToolCalls)
    {
        try
        {
            _chromaService ??= new ChromaService(config);
            return serviceRequest.Method.ToLower() switch
            {
                "memory_health_check" => await _chromaService.TestConnectionAsync(),
                "add_memory" => await _chromaService.AddMemoryAsync(serviceRequest.Text, config.DefaultCollectionName),
                "find_memory" => await _chromaService.SearchMemoriesByTextAsync(serviceRequest.Query, null, null, 5, config.DefaultCollectionName, false),
                "delete_memory" => await _chromaService.DeleteMemoryAsync(serviceRequest.MemoryId, config.DefaultCollectionName),
                "edit_memory" => await _chromaService.EditMemoryAsync(null, config.DefaultCollectionName),
                "get_memory" => await _chromaService.GetMemoryByIdAsync(serviceRequest.MemoryId, config.DefaultCollectionName),
                _ => new  { Success = false, Message = $"Action '{serviceRequest.Method}' not supported" }
            };
        }
        catch (Exception ex)
        {
            Console.Write($"Error executing action '{serviceRequest.Method}'", ex);
            return new
            {
                Success = false, 
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<object> Initialize(string configString)
    {
        var config = configString.ReadConfig<ServiceConfig>();
        _chromaService ??= new ChromaService(config);
        var collection = await _chromaService.GetOrCreateCollectionClientAsync(config.DefaultCollectionName);

        if (collection is not null)
        {
            Console.WriteLine($"Collection {config.DefaultCollectionName} exists in ChromaDB");
            
            // todo: Add logic to periodically review messages and save relevant information to memories
        }
        else
        {
            Console.WriteLine($"Collection {config.DefaultCollectionName} does not exist in ChromaDB");
        }

        return Task.CompletedTask;
    }
}