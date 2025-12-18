using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Represents search criteria for querying declaration nodes.
/// </summary>
public sealed record SearchQuery
{
    /// <summary>
    /// The text to search for in Name and FullyQualifiedName properties.
    /// </summary>
    public required string SearchText { get; init; }

    /// <summary>
    /// How the search text should match against node names.
    /// Defaults to <see cref="SearchMatchMode.Contains"/>.
    /// </summary>
    public SearchMatchMode MatchMode { get; init; } = SearchMatchMode.Contains;

    /// <summary>
    /// Optional filter to limit results to specific declaration kinds.
    /// If empty, all kinds are included.
    /// </summary>
    public IReadOnlyCollection<DeclarationKind> KindFilter { get; init; } = [];

    /// <summary>
    /// Whether the search should be case-sensitive.
    /// Defaults to false (case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; init; } = false;
}
