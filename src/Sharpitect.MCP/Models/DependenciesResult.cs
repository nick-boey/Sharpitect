namespace Sharpitect.MCP.Models;

/// <summary>
/// A dependency entry.
/// </summary>
public sealed record DependencyEntry(
    string Id,
    string Name,
    string Kind,
    bool IsTransitive,
    string? Via,
    string? Version);

/// <summary>
/// Result of a get_dependencies operation.
/// </summary>
public sealed record DependenciesResult(
    string ProjectId,
    IReadOnlyList<DependencyEntry> Dependencies);
