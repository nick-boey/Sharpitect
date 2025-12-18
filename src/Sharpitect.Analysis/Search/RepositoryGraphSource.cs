using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Adapter that makes IGraphRepository searchable.
/// </summary>
public sealed class RepositoryGraphSource : ISearchableGraphSource
{
    private readonly IGraphRepository _repository;

    /// <summary>
    /// Creates a new repository graph source.
    /// </summary>
    /// <param name="repository">The graph repository to search.</param>
    /// <exception cref="ArgumentNullException">Thrown when repository is null.</exception>
    public RepositoryGraphSource(IGraphRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclarationNode>> GetAllNodesAsync(
        CancellationToken cancellationToken = default)
    {
        var graph = await _repository.LoadGraphAsync(cancellationToken);
        return graph.Nodes.Values.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclarationNode>> GetNodesByKindsAsync(
        IEnumerable<DeclarationKind> kinds,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DeclarationNode>();

        foreach (var kind in kinds)
        {
            var nodes = await _repository.GetNodesByKindAsync(kind, cancellationToken);
            result.AddRange(nodes);
        }

        return result;
    }
}
