using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Analyzes and chunks markdown files, producing graph nodes and edges.
/// </summary>
public interface IMarkdownAnalyzer
{
    /// <summary>
    /// Parses a markdown file and returns graph nodes, edges, and section content for embedding.
    /// </summary>
    /// <param name="filePath">Absolute path to the markdown file.</param>
    /// <param name="solutionRootDirectory">Root directory for computing relative paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The analysis result containing nodes, edges, and section content.</returns>
    Task<MarkdownAnalysisResult> AnalyzeAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of markdown analysis integrated with the declaration graph.
/// </summary>
public sealed record MarkdownAnalysisResult
{
    /// <summary>
    /// The content hash of the document (SHA256) for change detection.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>
    /// All declaration nodes extracted (document, headings, sections).
    /// </summary>
    public required IReadOnlyList<DeclarationNode> Nodes { get; init; }

    /// <summary>
    /// All relationship edges (Contains, LinksTo).
    /// </summary>
    public required IReadOnlyList<RelationshipEdge> Edges { get; init; }

    /// <summary>
    /// Section node IDs mapped to their text content for embedding generation.
    /// </summary>
    public required IReadOnlyDictionary<string, string> SectionContents { get; init; }
}
