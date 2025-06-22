using System.Text.Json;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models.Extensions;

public static class SettingsExtensions
{
    public static T ReadPluginConfig<T>(this string config) where T: IPluginConfig
    {
        return ReadConfig<T>(config);
    }
    
    public static T ReadLoggingConfig<T>(this string config) where T: ILoggingConfig
    {
        return ReadConfig<T>(config);
    }

    private static T ReadConfig<T>(this string config)
    {
        if (!string.IsNullOrWhiteSpace(config))
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(config, options);
        }

        return default;
    }
}