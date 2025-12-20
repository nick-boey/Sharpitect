namespace Sharpitect.MCP.Models;

/// <summary>
/// An entry in an inheritance hierarchy.
/// </summary>
public sealed record InheritanceEntry(
    string Id,
    string Name,
    string Kind,
    string Relationship,
    int Depth);

/// <summary>
/// Result of a get_inheritance operation.
/// </summary>
public sealed record InheritanceResult(
    string NodeId,
    IReadOnlyList<InheritanceEntry> Ancestors,
    IReadOnlyList<InheritanceEntry> Descendants);
