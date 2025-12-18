namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Factory for creating output formatters.
/// </summary>
public interface IOutputFormatterFactory
{
    /// <summary>
    /// Gets a formatter for the specified format.
    /// </summary>
    /// <param name="format">The format identifier (e.g., "json", "text"). Defaults to "json" if null or empty.</param>
    /// <returns>The appropriate formatter.</returns>
    IOutputFormatter GetFormatter(string? format);
}
