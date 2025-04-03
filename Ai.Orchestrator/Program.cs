using Ai.Orchestrator.Middleware;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
// todo: uncomment with task scheduler work
// using TaskScheduler = Ai.Orchestrator.Services.TaskScheduler;

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
        // todo: uncomment with task scheduler work
        // var taskScheduler = app.Services.GetRequiredService<TaskScheduler>();
        ServiceResolver.Initialize(app.Services);
        
        await pluginService.InitializePlugins();
        
        app.UseSwagger()
            .UseSwaggerUI();

        app.UseHttpLogging()
            .UseHttpsRedirection()
            .UseAuthorization();

        app.MapControllers();

        try
        {
            await app.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            // todo: uncomment with task scheduler work
            // taskScheduler?.Dispose();
            await pluginService?.DisposePlugins();    
        }
    }   
}