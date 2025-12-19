namespace Sharpitect.Analysis.Graph;

/// <summary>
/// Represents a code declaration node in the graph.
/// </summary>
public sealed record DeclarationNode
{
    /// <summary>
    /// The fully qualified name including namespace/type hierarchy.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The simple name of the declaration (e.g., "OrderService", "CreateOrder").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The kind of declaration.
    /// </summary>
    public required DeclarationKind Kind { get; init; }

    /// <summary>
    /// The file path where this declaration is located.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The starting line number (1-based).
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// The starting column number (1-based).
    /// </summary>
    public required int StartColumn { get; init; }

    /// <summary>
    /// The ending line number (1-based).
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// The ending column number (1-based).
    /// </summary>
    public required int EndColumn { get; init; }

    /// <summary>
    /// Optional C4 model level annotation.
    /// </summary>
    public C4Level C4Level { get; init; } = C4Level.None;

    /// <summary>
    /// Optional C4 description from attributes.
    /// </summary>
    public string? C4Description { get; init; }

    /// <summary>
    /// Optional additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; init; }
}