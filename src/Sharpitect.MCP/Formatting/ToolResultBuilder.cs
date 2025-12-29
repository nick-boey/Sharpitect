using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Builds MCP tool results with both human-readable text content and structured JSON content.
/// </summary>
/// <remarks>
/// Claude Code prioritizes structured content for model processing but displays text content
/// to users. This builder ensures both are provided for optimal experience.
/// </remarks>
public sealed class ToolResultBuilder
{
    private readonly TextOutputFormatter _textFormatter;
    private readonly JsonSerializerOptions _jsonOptions;

    public ToolResultBuilder(TextOutputFormatter textFormatter)
    {
        _textFormatter = textFormatter;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Creates a CallToolResult with both text content (for display) and structured content (for model processing).
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="result">The result object to format.</param>
    /// <returns>A CallToolResult containing both content types.</returns>
    public CallToolResult Build<T>(T result) where T : notnull
    {
        var textContent = _textFormatter.Format(result);
        var structuredJson = JsonSerializer.SerializeToNode(result, _jsonOptions);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = textContent }],
            StructuredContent = structuredJson
        };
    }

    /// <summary>
    /// Creates a CallToolResult for an error condition.
    /// </summary>
    /// <typeparam name="T">The error result type.</typeparam>
    /// <param name="errorResult">The error result object.</param>
    /// <returns>A CallToolResult marked as an error.</returns>
    public CallToolResult BuildError<T>(T errorResult) where T : notnull
    {
        var textContent = _textFormatter.Format(errorResult);
        var structuredJson = JsonSerializer.SerializeToNode(errorResult, _jsonOptions);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = textContent }],
            StructuredContent = structuredJson,
            IsError = true
        };
    }
}
