using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Models.Extensions;

public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] DateTimeFormats = new[]
    {
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "MM/dd/yyyy",
        "MM/dd/yyyy HH:mm:ss",
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm:ss"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();

        if (string.IsNullOrEmpty(stringValue))
            throw new JsonException("Cannot convert null or empty string to DateTime");

        // Try parsing with various formats
        foreach (var format in DateTimeFormats)
        {
            if (DateTime.TryParseExact(stringValue, format, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var result))
                return result;
        }

        // Try standard parsing as fallback
        if (DateTime.TryParse(stringValue, out var fallbackResult))
            return fallbackResult;

        throw new JsonException($"Unable to convert '{stringValue}' to DateTime");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

public class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly FlexibleDateTimeConverter _baseConverter = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return _baseConverter.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            _baseConverter.Write(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}

public static class ObjectConverters
{
    private static JsonSerializerOptions GetDeserializeOptions()
    {
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true
        };
        
        options.Converters.Add(new FlexibleDateTimeConverter());
        options.Converters.Add(new FlexibleNullableDateTimeConverter());
        
        return options;
    }

    public static IEnumerable<T> GetServiceRequestArray<T>(this object request)
    {
        if (request is not null)
        {
            try
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

                    var options = GetDeserializeOptions();
                    return element.EnumerateArray()
                        .Select(jsonElement =>
                        {
                            try
                            {
                                // Deserialize directly from the JsonElement instead of converting to string first
                                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), options);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[GetServiceRequestArray<{typeof(T).Name}>] Error deserializing array element: {ex.Message}");
                                return default;
                            }
                        })
                        .Where(item => item != null)
                        .ToList();
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GetServiceRequestArray<{typeof(T).Name}>] Error parsing JSON: {ex.Message}");
                // Return empty collection for invalid JSON
            }
        }

        return Enumerable.Empty<T>();
    }
    
    public static T GetServiceRequest<T>(this object request) where T: class
    {
        Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Received request of type: {request?.GetType().FullName ?? "null"}");

        if (request == null)
        {
            Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Request is null, returning null.");
            return null;
        }

        var deserializeOptions = GetDeserializeOptions();

        if (request is string stringRequest)
        {
            Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Request is string: {stringRequest}");
            if (string.IsNullOrWhiteSpace(stringRequest) || stringRequest.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] String request is null, empty, whitespace, or the literal string 'null'. Returning null.");
                return null;
            }
            try
            {
                T result = JsonSerializer.Deserialize<T>(stringRequest, deserializeOptions);
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Deserialized string to result: {result?.ToString() ?? "null"}");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] JsonException while deserializing string: {ex.Message}. StackTrace: {ex.StackTrace}. Returning null.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Exception while deserializing string: {ex.ToString()}. Returning null.");
                return null;
            }
        }

        if (request is JsonElement jsonElement)
        {
            Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Request is JsonElement. ValueKind: {jsonElement.ValueKind}");
            if (jsonElement.ValueKind == JsonValueKind.Null)
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] JsonElement is null. Returning null.");
                return null;
            }
            try
            {
                T result;
                // Special handling for string-wrapped JSON objects
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    string jsonString = jsonElement.GetString();
                    Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] JsonElement is a string value: {jsonString}");
                    
                    // Check if the string looks like JSON
                    if (!string.IsNullOrEmpty(jsonString) && 
                        (jsonString.StartsWith("{") && jsonString.EndsWith("}")) || 
                        (jsonString.StartsWith("[") && jsonString.EndsWith("]")))
                    {
                        try
                        {
                            // Parse the string as JSON first
                            result = JsonSerializer.Deserialize<T>(jsonString, deserializeOptions);
                            Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Successfully parsed string-wrapped JSON to result: {result?.ToString() ?? "null"}");
                            return result;
                        }
                        catch (JsonException innerEx)
                        {
                            Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] JsonException while parsing string-wrapped JSON: {innerEx.Message}");
                            // Fall through to try direct deserialization
                        }
                    }
                }
                
                // Standard deserialization for non-string or fallback
                result = JsonSerializer.Deserialize<T>(jsonElement, deserializeOptions);
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Deserialized JsonElement to result: {result?.ToString() ?? "null"}");
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] JsonException while deserializing JsonElement: {ex.Message}. StackTrace: {ex.StackTrace}. Returning null.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Exception while deserializing JsonElement: {ex.ToString()}. Returning null.");
                return null;
            }
        }
        
        Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Request is not string or JsonElement. Attempting direct cast.");
        T castResult = request as T;
        Console.WriteLine($"[GetServiceRequest<{typeof(T).Name}>] Cast result: {castResult?.ToString() ?? "null"}");
        return castResult;
    }
}