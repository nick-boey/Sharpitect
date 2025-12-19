namespace Sharpitect.Analysis.Graph;

/// <summary>
/// Represents a directed relationship between two declaration nodes.
/// </summary>
public sealed record RelationshipEdge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The ID of the source node.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// The ID of the target node.
    /// </summary>
    public required string TargetId { get; init; }

    /// <summary>
    /// The kind of relationship.
    /// </summary>
    public required RelationshipKind Kind { get; init; }

    /// <summary>
    /// The file path where this relationship originates (for calls, references, etc.).
    /// </summary>
    public string? SourceFilePath { get; init; }

    /// <summary>
    /// The line number where this relationship originates.
    /// </summary>
    public int? SourceLine { get; init; }

    /// <summary>
    /// Optional additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; init; }
}