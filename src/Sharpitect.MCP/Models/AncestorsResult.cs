namespace Sharpitect.MCP.Models;

/// <summary>
/// Result of a get_ancestors operation.
/// </summary>
public sealed record AncestorsResult(
    string NodeId,
    IReadOnlyList<NodeSummary> Ancestors);
