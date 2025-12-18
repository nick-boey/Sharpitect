namespace Sharpitect.Analysis.Search;

/// <summary>
/// Specifies how search queries match against node names.
/// </summary>
public enum SearchMatchMode
{
    /// <summary>
    /// Match if the search text is contained anywhere in the name.
    /// </summary>
    Contains,

    /// <summary>
    /// Match if the name starts with the search text.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Match if the name ends with the search text.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Match if the name exactly equals the search text.
    /// </summary>
    Exact
}
