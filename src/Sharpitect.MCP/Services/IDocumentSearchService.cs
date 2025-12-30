using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Service for searching and navigating markdown documents in the graph.
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Searches for markdown content using semantic similarity.
    /// </summary>
    /// <param name="query">The natural language search query.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="minSimilarity">Minimum similarity score (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ranked by similarity.</returns>
    Task<DocumentSearchResults> SearchAsync(
        string query,
        int limit = 10,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all indexed markdown documents.
    /// </summary>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of markdown documents.</returns>
    Task<MarkdownDocumentList> ListDocumentsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific markdown document.
    /// </summary>
    /// <param name="documentId">The document ID (relative path).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed document information, or null if not found.</returns>
    Task<MarkdownDocumentDetail?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the content of a markdown document section.
    /// </summary>
    /// <param name="sectionId">The section node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The section content, or null if not found.</returns>
    Task<string?> GetSectionContentAsync(
        string sectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if semantic search is available (embedding service configured).
    /// </summary>
    bool IsSemanticSearchAvailable { get; }
}
