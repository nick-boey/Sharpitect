using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Formats results as JSON.
/// </summary>
public sealed class JsonOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions SOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string FormatName => OutputFormat.Json;

    public string Format<T>(T result) where T : notnull
    {
        return JsonSerializer.Serialize(result, SOptions);
    }
}
