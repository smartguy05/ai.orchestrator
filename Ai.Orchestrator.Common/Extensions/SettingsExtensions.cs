using System.Dynamic;
using System.Text.Json;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Common.Extensions;

public static class SettingsExtensions
{
    public static T ReadConfig<T>(this string config) where T: IPluginConfig
    {
        if (!string.IsNullOrWhiteSpace(config))
        {
            var deserialized = JsonSerializer.Deserialize<T>(config, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
            using JsonDocument doc = JsonDocument.Parse(deserialized.Contract.ToString());
            var element = doc.RootElement;
            var contract = element.Deserialize<ExpandoObject>();
            deserialized.Contract = contract;
            return deserialized;
        }

        return default;
    }
}