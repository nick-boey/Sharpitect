namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Factory for creating output formatters.
/// </summary>
public sealed class OutputFormatterFactory : IOutputFormatterFactory
{
    private readonly JsonOutputFormatter _jsonFormatter = new();
    private readonly TextOutputFormatter _textFormatter = new();

    public IOutputFormatter GetFormatter(string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return _jsonFormatter;
        }

        return format.ToLowerInvariant() switch
        {
            OutputFormat.Text => _textFormatter,
            _ => _jsonFormatter
        };
    }
}
