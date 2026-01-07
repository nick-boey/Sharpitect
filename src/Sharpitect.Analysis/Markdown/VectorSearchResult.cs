using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Result from a vector similarity search.
/// </summary>
public sealed record VectorSearchResult
{
    /// <summary>
    /// The matching section node (MarkdownSection kind).
    /// </summary>
    public required DeclarationNode SectionNode { get; init; }

    /// <summary>
    /// The text content of the section.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The parent document node (MarkdownDocument kind).
    /// </summary>
    public required DeclarationNode DocumentNode { get; init; }

    /// <summary>
    /// Cosine similarity score (0.0 to 1.0).
    /// </summary>
    public required double SimilarityScore { get; init; }
}
