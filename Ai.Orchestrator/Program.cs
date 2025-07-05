using Ai.Orchestrator.Middleware;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        
        builder.Services
            .AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost",
                    policy => policy.SetIsOriginAllowed(origin => origin.Contains("localhost"))
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            })
        #if DEBUG
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddHttpLogging(options =>
            {
                // Configure HTTP logging options
                options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
                options.RequestBodyLogLimit = 4096;
                options.ResponseBodyLogLimit = 4096;
            })
        #endif
            .RegisterOrchestratorMiddleware();

        var app = builder.Build();

        var loggingService = app.Services.GetRequiredService<ILoggingService>();
        await loggingService.Log(LogLevel.Info, "Loading application");
        await loggingService.Log(LogLevel.Trace, "Logging Service Started");
        var pluginService = app.Services.GetRequiredService<IPluginService>();
        await loggingService.Log(LogLevel.Trace, "Plugin Service Started");
        var taskScheduler = app.Services.GetRequiredService<ITaskScheduler>();
        await loggingService.Log(LogLevel.Trace, "Task Scheduler Started");
        var confirmationService = app.Services.GetRequiredService<INotificationService>();
        await loggingService.Log(LogLevel.Trace, "Confirmation Service Started");
        
        ServiceResolver.Initialize(app.Services);
        
        await loggingService.Log(LogLevel.Info, "Initializing Plugins");
        await pluginService.InitializePlugins(loggingService.Log, confirmationService);
        
        #if DEBUG
        app.UseSwagger()
            .UseSwaggerUI();
        #endif
        
        app.UseHttpLogging()
            .UseHttpsRedirection()
            .UseCors("AllowLocalhost")
            .UseAuthorization();

        app.MapControllers();

        try
        {
            await loggingService.Log(LogLevel.Info, "Starting application");
            await app.RunAsync();
        }
        catch (Exception e)
        {
            await loggingService.LogError(e.Message, e);
        }
        finally
        {
            await loggingService.Log(LogLevel.Info, "Disposing plugins");
            await pluginService.DisposePlugins();
        }
    }   
}