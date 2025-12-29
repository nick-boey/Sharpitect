using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Result of analyzing a single file.
/// </summary>
public sealed record FileAnalysisResult
{
    /// <summary>
    /// Gets the nodes discovered in the file.
    /// </summary>
    public required IReadOnlyList<DeclarationNode> Nodes { get; init; }

    /// <summary>
    /// Gets the edges originating from this file.
    /// </summary>
    public required IReadOnlyList<RelationshipEdge> Edges { get; init; }

    /// <summary>
    /// Gets the symbol display strings to node ID mappings discovered.
    /// </summary>
    public required IReadOnlyDictionary<string, string> SymbolMappings { get; init; }
}
