namespace Sharpitect.MCP.Models;

/// <summary>
/// A callee entry with depth information.
/// </summary>
public sealed record CalleeEntry(
    string Id,
    string Name,
    string Kind,
    string? FilePath,
    int? LineNumber,
    int Depth);

/// <summary>
/// Result of a get_callees operation.
/// </summary>
public sealed record CalleesResult(
    string SourceId,
    IReadOnlyList<CalleeEntry> Callees,
    int TotalCount,
    bool MaxDepthReached);
