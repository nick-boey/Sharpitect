using System.Text.Json;
using Sharpitect.Analysis.Embedding;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Markdown;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Search;

/// <summary>
/// Implementation of vector similarity search for markdown documents.
/// </summary>
public sealed class VectorSearchService : IVectorSearchService
{
    private readonly IGraphRepository _repository;
    private readonly IEmbeddingService _embeddingService;

    /// <summary>
    /// Creates a new vector search service.
    /// </summary>
    /// <param name="repository">The graph repository.</param>
    /// <param name="embeddingService">The embedding service for query embedding.</param>
    public VectorSearchService(
        IGraphRepository repository,
        IEmbeddingService embeddingService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int limit = 10,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);

        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.EmbedAsync(query, cancellationToken);

        // Search for similar embeddings
        var similarNodes = await _repository.SearchSimilarEmbeddingsAsync(
            queryEmbedding,
            limit,
            minSimilarity,
            cancellationToken);

        if (similarNodes.Count == 0)
        {
            return Array.Empty<VectorSearchResult>();
        }

        // Fetch the section nodes and their parent documents
        var results = new List<VectorSearchResult>();

        foreach (var (nodeId, similarity) in similarNodes)
        {
            var sectionNode = await _repository.GetNodeAsync(nodeId, cancellationToken);
            if (sectionNode == null || sectionNode.Kind != DeclarationKind.MarkdownSection)
            {
                continue;
            }

            // Find the parent document
            var documentNode = await FindParentDocumentAsync(sectionNode, cancellationToken);
            if (documentNode == null)
            {
                continue;
            }

            // Get the content from the section (we need to re-read it from file or cache)
            var content = await GetSectionContentAsync(sectionNode, documentNode, cancellationToken);

            results.Add(new VectorSearchResult
            {
                SectionNode = sectionNode,
                Content = content ?? "",
                DocumentNode = documentNode,
                SimilarityScore = similarity
            });
        }

        return results;
    }

    private async Task<DeclarationNode?> FindParentDocumentAsync(
        DeclarationNode sectionNode,
        CancellationToken cancellationToken)
    {
        // The document ID is the file path, which is stored in FilePath
        var documentId = sectionNode.FilePath;

        var documentNode = await _repository.GetNodeAsync(documentId, cancellationToken);
        if (documentNode?.Kind == DeclarationKind.MarkdownDocument)
        {
            return documentNode;
        }

        // Fallback: search for document node by file path
        var nodes = await _repository.GetNodesByFileAsync(documentId, cancellationToken);
        return nodes.FirstOrDefault(n => n.Kind == DeclarationKind.MarkdownDocument);
    }

    private async Task<string?> GetSectionContentAsync(
        DeclarationNode sectionNode,
        DeclarationNode documentNode,
        CancellationToken cancellationToken)
    {
        // Try to extract content from the source file based on line numbers
        var filePath = documentNode.FilePath;

        // We need to resolve the actual file path from the solution root
        // For now, we'll return an empty string and the caller can read from metadata or file
        // In a full implementation, we'd need access to the solution root path

        // As a simple approach, check if metadata contains the content
        if (!string.IsNullOrEmpty(sectionNode.Metadata))
        {
            try
            {
                using var doc = JsonDocument.Parse(sectionNode.Metadata);
                if (doc.RootElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString();
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        // Return a description based on the node info
        return $"Section from {filePath} (lines {sectionNode.StartLine}-{sectionNode.EndLine})";
    }
}
