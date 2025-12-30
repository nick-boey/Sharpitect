namespace Sharpitect.MCP.Models;

/// <summary>
/// A single result from a document semantic search.
/// </summary>
public sealed record DocumentSearchMatch(
    string DocumentId,
    string? DocumentTitle,
    string SectionId,
    string? HeadingPath,
    string Content,
    int StartLine,
    int EndLine,
    double SimilarityScore);

/// <summary>
/// Results from a document semantic search operation.
/// </summary>
public sealed record DocumentSearchResults(
    IReadOnlyList<DocumentSearchMatch> Results,
    int TotalCount,
    string Query);
