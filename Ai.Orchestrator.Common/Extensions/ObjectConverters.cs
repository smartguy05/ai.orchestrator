using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ai.Orchestrator.Common.Extensions;

public static class ObjectConverters
{
    public static IEnumerable<T> GetServiceRequestArray<T>(this object request)
    {
        if (request is not null)
        {
            using JsonDocument doc = JsonDocument.Parse(request.ToString());
            var element = doc.RootElement;
            if (element.ValueKind == JsonValueKind.Array)
            {
                if (typeof(T) == typeof(string))
                {
                    return element.EnumerateArray()
                        .Select(s => s.GetString())
                        .ToList() as List<T>;
                }
                
                return element.EnumerateArray()
                    .Select(s => JsonSerializer.Deserialize<T>(s.GetString()))
                    .ToList();
            }   
        }

        return Enumerable.Empty<T>();
    }
    
    public static T GetServiceRequest<T>(this object request) where T: class
    {
        if (request is not null)
        {
            using JsonDocument doc = JsonDocument.Parse(request.ToString());
            var element = doc.RootElement;
            return element.Deserialize<T>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
        }

        return default;
    }
}