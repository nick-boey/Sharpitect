namespace Sharpitect.MCP.Models;

/// <summary>
/// Result of a search_declarations operation.
/// </summary>
public sealed record SearchResults(
    IReadOnlyList<NodeSummary> Results,
    int TotalCount,
    bool Truncated);
