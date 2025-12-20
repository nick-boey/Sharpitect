namespace Sharpitect.MCP.Models;

/// <summary>
/// Result containing a node's declaration summary and source code.
/// </summary>
public sealed record CodeResult(
    NodeDetail Node,
    string? SourceCode,
    string? Error);
