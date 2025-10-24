using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Services;
using Ai.Orchestrator.Services.Agents;
using Ai.Orchestrator.Services.Auditing;
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
        // Register configuration (still needed for database connection, JWT, etc.)
        services.AddSingleton<IConfig, Models.Configuration.Config>();

        // Create config instance for use throughout middleware registration
        var config = new Models.Configuration.Config();

        // Register database context (only if NOT in test environment)
        // Integration tests set IS_INTEGRATION_TEST=true and register their own InMemory database
        var isIntegrationTest = Environment.GetEnvironmentVariable("IS_INTEGRATION_TEST") == "true";

        if (!isIntegrationTest)
        {
            // Production/Development environment - register PostgreSQL
            services.AddDbContext<OrchestratorDbContext>(options =>
                options.UseNpgsql(config.PostgresConnectionString));
        }
        // In test environment, tests will register InMemory database themselves

        // Register new database-backed services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
        services.AddScoped<IPluginConfigurationService, PluginConfigurationService>();
        services.AddScoped<IDatabaseSeederService, DatabaseSeederService>();
        services.AddScoped<IAuditService, AuditService>();

        // Register AgentServiceManager as singleton (manages per-agent service instances)
        services.AddSingleton<AgentServiceManager>();

        // Register HttpContextAccessor for audit logging
        services.AddHttpContextAccessor();

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

        // Configure health checks
        services.AddHealthChecks()
            .AddDbContextCheck<OrchestratorDbContext>(
                name: "database",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql", "ready" });

        return services;
    }
}