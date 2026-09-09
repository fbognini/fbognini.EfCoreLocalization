using System.Text.Json;
using System.Text.Json.Serialization;

namespace fbognini.EfCoreLocalization.Dashboard.Helpers;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };
}
