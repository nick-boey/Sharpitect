namespace Sharpitect.MCP.Models;

/// <summary>
/// Result containing the contents of a source file.
/// </summary>
public sealed record FileContentResult(
    string FilePath,
    string? Content,
    string? Error,
    int? LineCount);
