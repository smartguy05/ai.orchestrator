using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models.Configuration;

public class Config: IConfig
{
    public string ApiKey { get; set; }
    public string PluginDirectory { get; set; }
    public string ConfigDirectory { get; set; }
    public string ActivePlugins { get; set; }
    public string ConfirmationPlugin { get; set; }
    public int ConfirmationExpirationMinutes { get; set; }
    public int NotificationTimeoutHours { get; set; }
    public string RedisConnectionString { get; set; }
    public string RedisConversationSubject { get; set; }
    public string LoggingPluginsString { get; set; }
    public List<string> LoggingPlugins => LoggingPluginsString.Split(",").ToList();
    public bool LogToConsole { get; set; }

    // Database configuration
    public string PostgresConnectionString { get; set; }

    // JWT configuration
    public string JwtSecret { get; set; }
    public string JwtIssuer { get; set; }
    public string JwtAudience { get; set; }
    public int JwtExpirationMinutes { get; set; }

    // Refresh token configuration
    public int RefreshTokenExpirationDays { get; set; }

    // Admin user configuration
    public string AdminUsername { get; set; }
    public string AdminPassword { get; set; }

    public Config()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        configBuilder.Build().Bind(this);
    }
}