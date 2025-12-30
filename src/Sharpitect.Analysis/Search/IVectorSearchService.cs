using Sharpitect.Analysis.Markdown;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Service for semantic similarity search in markdown documents.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Searches for similar content using a natural language query.
    /// </summary>
    /// <param name="query">The natural language search query.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="minSimilarity">Minimum similarity score (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked search results with similarity scores.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int limit = 10,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default);
}
