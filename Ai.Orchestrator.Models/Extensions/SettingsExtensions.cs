using System.Text.Json;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models.Extensions;

public static class SettingsExtensions
{
    public static T ReadConfig<T>(this string config) where T: IPluginConfig
    {
        if (!string.IsNullOrWhiteSpace(config))
        {
            return JsonSerializer.Deserialize<T>(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
        }

        return default;
    }
}