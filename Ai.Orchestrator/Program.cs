using Ai.Orchestrator.Middleware;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using TaskScheduler = Ai.Orchestrator.Services.TaskScheduler;

namespace Ai.Orchestrator;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .RegisterOrchestratorMiddleware();

        var app = builder.Build();

        var pluginService = app.Services.GetRequiredService<IPluginService>();
        // var taskScheduler = app.Services.GetRequiredService<TaskScheduler>();
        ServiceResolver.Initialize(app.Services);
        
        await pluginService.InitializePlugins();
        
        app.UseSwagger()
            .UseSwaggerUI();

        app.UseHttpLogging()
            .UseHttpsRedirection()
            .UseAuthorization();

        app.MapControllers();
        
        await app.RunAsync();

        // if (taskScheduler.Subscriber is not null)
        // {
        //     await taskScheduler.Subscriber.UnsubscribeAllAsync();
        // }
        //
        // if (taskScheduler.ConnectionMultiplexer is not null)
        // {
        //     await taskScheduler.ConnectionMultiplexer.DisposeAsync();
        // }

        await pluginService.DisposePlugins();
    }   
}