using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaimCheckPlayground.Processor;

/// <summary>Shared JSON serialiser defaults for the Processor service.</summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
