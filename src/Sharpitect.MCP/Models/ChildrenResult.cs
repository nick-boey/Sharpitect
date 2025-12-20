namespace Sharpitect.MCP.Models;

/// <summary>
/// Result of a get_children operation.
/// </summary>
public sealed record ChildrenResult(
    string ParentId,
    IReadOnlyList<NodeSummary> Children,
    int TotalCount,
    bool Truncated);
