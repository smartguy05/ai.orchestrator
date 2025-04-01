using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Services;

public static class ServiceResolver
{
    private static IServiceProvider _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static IOrchestrator GetOrchestrator()
    {
        return GetService<IOrchestrator>();
    }

    public static IPluginService GetPluginService()
    {
        return GetService<IPluginService>();
    }

    // public static ITaskScheduler GetTaskScheduler()
    // {
    //     return GetService<ITaskScheduler>();
    // }
    
    public static T GetService<T>()
    {
        if (_serviceProvider is not null)
        {
            return _serviceProvider.GetService<T>();
        }

        throw new Exception("No Scope Factory Configured");
    }
}