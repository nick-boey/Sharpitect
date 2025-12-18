namespace Sharpitect.MCP.Models;

/// <summary>
/// Result of a list_by_kind operation.
/// </summary>
public sealed record ListByKindResult(
    string Kind,
    string? Scope,
    IReadOnlyList<NodeSummary> Results,
    int TotalCount,
    bool Truncated);
