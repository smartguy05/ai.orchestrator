using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Orchestrator.Middleware;

public static class MiddlewareRegistration
{
    public static IServiceCollection RegisterOrchestratorMiddleware(this IServiceCollection services)
    {
        services.AddScoped<IOrchestrator, Services.Orchestrator>();
        services.AddScoped<IPluginService, PluginService>();
        
        return services;
    }
}