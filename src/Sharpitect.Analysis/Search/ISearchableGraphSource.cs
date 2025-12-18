using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Abstraction for graph data sources that support search operations.
/// </summary>
public interface ISearchableGraphSource
{
    /// <summary>
    /// Gets all nodes from the source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All declaration nodes.</returns>
    Task<IReadOnlyList<DeclarationNode>> GetAllNodesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nodes filtered by kind.
    /// </summary>
    /// <param name="kinds">The kinds to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Nodes matching any of the specified kinds.</returns>
    Task<IReadOnlyList<DeclarationNode>> GetNodesByKindsAsync(
        IEnumerable<DeclarationKind> kinds,
        CancellationToken cancellationToken = default);
}
