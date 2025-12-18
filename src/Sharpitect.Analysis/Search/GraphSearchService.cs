using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Default implementation of the graph search service.
/// </summary>
public sealed class GraphSearchService : IGraphSearchService
{
    private readonly ISearchableGraphSource _source;

    /// <summary>
    /// Creates a new graph search service.
    /// </summary>
    /// <param name="source">The graph source to search.</param>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public GraphSearchService(ISearchableGraphSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclarationNode>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.SearchText))
        {
            return [];
        }

        // Get candidate nodes (optionally pre-filtered by kind)
        var nodes = query.KindFilter.Count > 0
            ? await _source.GetNodesByKindsAsync(query.KindFilter, cancellationToken)
            : await _source.GetAllNodesAsync(cancellationToken);

        // Determine string comparison based on case sensitivity
        var comparison = query.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // Apply text matching filter
        return nodes
            .Where(node => MatchesText(node, query.SearchText, query.MatchMode, comparison))
            .ToList();
    }

    private static bool MatchesText(
        DeclarationNode node,
        string searchText,
        SearchMatchMode matchMode,
        StringComparison comparison)
    {
        return MatchesField(node.Name, searchText, matchMode, comparison) ||
               MatchesField(node.FullyQualifiedName, searchText, matchMode, comparison);
    }

    private static bool MatchesField(
        string fieldValue,
        string searchText,
        SearchMatchMode matchMode,
        StringComparison comparison)
    {
        return matchMode switch
        {
            SearchMatchMode.Contains => fieldValue.Contains(searchText, comparison),
            SearchMatchMode.StartsWith => fieldValue.StartsWith(searchText, comparison),
            SearchMatchMode.EndsWith => fieldValue.EndsWith(searchText, comparison),
            SearchMatchMode.Exact => fieldValue.Equals(searchText, comparison),
            _ => throw new ArgumentOutOfRangeException(nameof(matchMode), matchMode, "Unknown match mode")
        };
    }
}
