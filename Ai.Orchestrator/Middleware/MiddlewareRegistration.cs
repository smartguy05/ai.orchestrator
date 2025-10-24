using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Ai.Orchestrator.Services.Agents;
using Ai.Orchestrator.Services.Authentication;
using Ai.Orchestrator.Services.Plugin;
using Ai.Orchestrator.Services.PluginConfigs;
using Ai.Orchestrator.Services.Seeding;
using Ai.Orchestrator.Services.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Ai.Orchestrator.Middleware;

public static class MiddlewareRegistration
{
    public static IServiceCollection RegisterOrchestratorMiddleware(this IServiceCollection services)
    {
        // Register existing services
        services.AddSingleton<IOrchestrator, Services.Orchestrator>();
        services.AddSingleton<IPluginService, PluginService>();
        services.AddSingleton<ITaskScheduler, Services.TaskScheduler>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Register configuration
        services.AddSingleton<IConfig, Models.Configuration.Config>();

        // Register database context
        var config = new Models.Configuration.Config();
        services.AddDbContext<OrchestratorDbContext>(options =>
            options.UseNpgsql(config.PostgresConnectionString));

        // Register new database-backed services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
        services.AddScoped<IPluginConfigurationService, PluginConfigurationService>();
        services.AddScoped<IDatabaseSeederService, DatabaseSeederService>();

        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var key = Encoding.UTF8.GetBytes(config.JwtSecret);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.JwtIssuer,
                ValidAudience = config.JwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero // No clock skew tolerance
            };
        });

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("AgentManager", policy => policy.RequireRole("Admin", "AgentManager"));
            options.AddPolicy("User", policy => policy.RequireRole("Admin", "AgentManager", "User"));
            options.AddPolicy("ReadOnly", policy => policy.RequireRole("Admin", "AgentManager", "User", "ReadOnly"));
        });

        return services;
    }
}