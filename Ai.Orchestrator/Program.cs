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

        // Add JSON options for controllers
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
            });
        
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
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "AI Orchestrator API",
                    Version = "v1",
                    Description = "Multi-user AI agent orchestration platform with JWT authentication and role-based access control",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "AI Orchestrator",
                        Url = new Uri("https://github.com/smartguy05/ai.orchestrator")
                    }
                });

                // Include XML comments
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // Add JWT authentication to Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            })
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
            .UseAuthentication()  // Add authentication BEFORE authorization
            .UseAuthorization();

        app.MapControllers();

        // Seed database with admin user
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeederService>();
            await loggingService.Log(LogLevel.Info, "Seeding database with admin user");
            await seeder.SeedAsync();
            await loggingService.Log(LogLevel.Info, "Database seeding completed");
        }

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