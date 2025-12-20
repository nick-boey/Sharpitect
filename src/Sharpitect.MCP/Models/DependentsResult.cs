namespace Sharpitect.MCP.Models;

/// <summary>
/// A dependent entry.
/// </summary>
public sealed record DependentEntry(
    string Id,
    string Name,
    string Kind,
    bool IsTransitive);

/// <summary>
/// Result of a get_dependents operation.
/// </summary>
public sealed record DependentsResult(
    string ProjectId,
    IReadOnlyList<DependentEntry> Dependents);
