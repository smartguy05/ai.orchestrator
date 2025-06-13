using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;

namespace Ai.Orchestrator.Middleware;

public static class MiddlewareRegistration
{
    public static IServiceCollection RegisterOrchestratorMiddleware(this IServiceCollection services)
    {
        services.AddSingleton<IOrchestrator, Services.Orchestrator>();
        services.AddSingleton<IPluginService, PluginService>();
        services.AddSingleton<ITaskScheduler, Ai.Orchestrator.Services.TaskScheduler>();
        
        return services;
    }
}