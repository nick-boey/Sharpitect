using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Adapter that makes DeclarationGraph searchable.
/// </summary>
public sealed class InMemoryGraphSource : ISearchableGraphSource
{
    private readonly DeclarationGraph _graph;

    /// <summary>
    /// Creates a new in-memory graph source.
    /// </summary>
    /// <param name="graph">The declaration graph to search.</param>
    /// <exception cref="ArgumentNullException">Thrown when graph is null.</exception>
    public InMemoryGraphSource(DeclarationGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeclarationNode>> GetAllNodesAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = _graph.Nodes.Values.ToList();
        return Task.FromResult<IReadOnlyList<DeclarationNode>>(nodes);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeclarationNode>> GetNodesByKindsAsync(
        IEnumerable<DeclarationKind> kinds,
        CancellationToken cancellationToken = default)
    {
        var kindSet = kinds.ToHashSet();
        var nodes = _graph.Nodes.Values
            .Where(n => kindSet.Contains(n.Kind))
            .ToList();
        return Task.FromResult<IReadOnlyList<DeclarationNode>>(nodes);
    }
}
