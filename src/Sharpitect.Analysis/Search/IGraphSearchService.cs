using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Service for searching declaration nodes in the graph.
/// </summary>
public interface IGraphSearchService
{
    /// <summary>
    /// Searches for nodes matching the specified query.
    /// </summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Nodes matching the search criteria.</returns>
    Task<IReadOnlyList<DeclarationNode>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);
}
