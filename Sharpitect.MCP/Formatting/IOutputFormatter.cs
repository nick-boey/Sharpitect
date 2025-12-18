namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Formats results for output.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// The format identifier (e.g., "json", "text").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Formats an object for output.
    /// </summary>
    string Format<T>(T result) where T : notnull;
}
