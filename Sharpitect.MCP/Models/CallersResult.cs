namespace Sharpitect.MCP.Models;

/// <summary>
/// A caller entry with depth information.
/// </summary>
public sealed record CallerEntry(
    string Id,
    string Name,
    string Kind,
    string? FilePath,
    int? LineNumber,
    int Depth);

/// <summary>
/// Result of a get_callers operation.
/// </summary>
public sealed record CallersResult(
    string TargetId,
    IReadOnlyList<CallerEntry> Callers,
    int TotalCount,
    bool MaxDepthReached);
