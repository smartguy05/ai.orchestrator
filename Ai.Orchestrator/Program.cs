using Ai.Orchestrator.Middleware;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services.Agents;

namespace Ai.Orchestrator;

public class Program
{
    public static async Task Main(string[] args)
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

        // Seed database with admin user and default roles
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeederService>();
            await seeder.SeedAsync();
        }

        // Initialize per-agent services for all active agents
        var agentServiceManager = app.Services.GetRequiredService<AgentServiceManager>();
        await agentServiceManager.InitializeAllAgentsAsync();
        Console.WriteLine("Agent services initialized for all active agents");

        await app.RunAsync();
    }   
}