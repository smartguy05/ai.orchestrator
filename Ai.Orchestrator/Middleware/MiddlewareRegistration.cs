using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Ai.Orchestrator.Services.Plugin;

namespace Ai.Orchestrator.Middleware;

public static class MiddlewareRegistration
{
    public static IServiceCollection RegisterOrchestratorMiddleware(this IServiceCollection services)
    {
        services.AddSingleton<IOrchestrator, Services.Orchestrator>();
        services.AddSingleton<IPluginService, PluginService>();
        services.AddSingleton<ITaskScheduler, Services.TaskScheduler>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        
        return services;
    }
}