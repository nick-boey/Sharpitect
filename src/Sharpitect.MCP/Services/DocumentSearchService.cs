using System.Text.Json;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Implementation of document search service using the graph repository.
/// </summary>
public sealed class DocumentSearchService : IDocumentSearchService
{
    private readonly IGraphRepository _repository;
    private readonly IVectorSearchService? _vectorSearchService;

    public DocumentSearchService(
        IGraphRepository repository,
        IVectorSearchService? vectorSearchService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _vectorSearchService = vectorSearchService;
    }

    public bool IsSemanticSearchAvailable => _vectorSearchService != null;

    public async Task<DocumentSearchResults> SearchAsync(
        string query,
        int limit = 10,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);

        if (_vectorSearchService == null)
        {
            // Semantic search not available - return empty results
            return new DocumentSearchResults(
                Array.Empty<DocumentSearchMatch>(),
                0,
                query);
        }

        var searchResults = await _vectorSearchService.SearchAsync(
            query,
            limit,
            minSimilarity,
            cancellationToken);

        var matches = new List<DocumentSearchMatch>();

        foreach (var result in searchResults)
        {
            var headingPath = await GetHeadingPathAsync(result.SectionNode, cancellationToken);
            var title = GetTitleFromMetadata(result.DocumentNode.Metadata);

            matches.Add(new DocumentSearchMatch(
                result.DocumentNode.Id,
                title,
                result.SectionNode.Id,
                headingPath,
                result.Content,
                result.SectionNode.StartLine,
                result.SectionNode.EndLine,
                result.SimilarityScore));
        }

        return new DocumentSearchResults(matches, matches.Count, query);
    }

    public async Task<MarkdownDocumentList> ListDocumentsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var documentNodes = await _repository.GetNodesByKindAsync(
            DeclarationKind.MarkdownDocument,
            cancellationToken);

        var documents = new List<MarkdownDocumentSummary>();
        var truncated = documentNodes.Count > limit;
        var nodesToProcess = documentNodes.Take(limit);

        foreach (var doc in nodesToProcess)
        {
            var title = GetTitleFromMetadata(doc.Metadata);

            // Count headings and sections for this document
            var headingCount = 0;
            var sectionCount = 0;

            var allNodes = await _repository.GetNodesByFileAsync(doc.FilePath ?? doc.Id, cancellationToken);
            foreach (var node in allNodes)
            {
                if (node.Kind == DeclarationKind.MarkdownHeading)
                {
                    headingCount++;
                }
                else if (node.Kind == DeclarationKind.MarkdownSection)
                {
                    sectionCount++;
                }
            }

            documents.Add(new MarkdownDocumentSummary(
                doc.Id,
                doc.Name,
                title,
                headingCount,
                sectionCount));
        }

        return new MarkdownDocumentList(documents, documentNodes.Count, truncated);
    }

    public async Task<MarkdownDocumentDetail?> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        var docNode = await _repository.GetNodeAsync(documentId, cancellationToken);
        if (docNode == null || docNode.Kind != DeclarationKind.MarkdownDocument)
        {
            return null;
        }

        var title = GetTitleFromMetadata(docNode.Metadata);
        var contentHash = GetContentHashFromMetadata(docNode.Metadata);

        // Get all nodes for this document
        var allNodes = await _repository.GetNodesByFileAsync(docNode.FilePath ?? docNode.Id, cancellationToken);

        // Extract headings
        var headings = allNodes
            .Where(n => n.Kind == DeclarationKind.MarkdownHeading)
            .Select(n => new MarkdownHeadingSummary(
                n.Id,
                n.Name,
                GetHeadingLevelFromMetadata(n.Metadata),
                n.StartLine))
            .OrderBy(h => h.LineNumber)
            .ToList();

        // Count sections
        var sectionCount = allNodes.Count(n => n.Kind == DeclarationKind.MarkdownSection);

        // Get outgoing links (LinksTo relationships from this document)
        var outgoingEdges = await _repository.GetOutgoingEdgesAsync(docNode.Id, cancellationToken);
        var outgoingLinks = outgoingEdges
            .Where(e => e.Kind == RelationshipKind.LinksTo)
            .Select(e => new MarkdownLinkSummary(
                e.TargetId,
                GetLinkTextFromMetadata(e.Metadata),
                IsWikilinkFromMetadata(e.Metadata),
                GetSourceLineFromMetadata(e.Metadata)))
            .ToList();

        // Get incoming links (LinksTo relationships to this document)
        var incomingEdges = await _repository.GetIncomingEdgesAsync(docNode.Id, cancellationToken);
        var incomingLinks = incomingEdges
            .Where(e => e.Kind == RelationshipKind.LinksTo)
            .Select(e => new MarkdownLinkSummary(
                e.SourceId,
                GetLinkTextFromMetadata(e.Metadata),
                IsWikilinkFromMetadata(e.Metadata),
                GetSourceLineFromMetadata(e.Metadata)))
            .ToList();

        return new MarkdownDocumentDetail(
            docNode.Id,
            docNode.Name,
            title,
            contentHash,
            headings,
            outgoingLinks,
            incomingLinks,
            sectionCount);
    }

    public async Task<string?> GetSectionContentAsync(
        string sectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sectionId);

        var sectionNode = await _repository.GetNodeAsync(sectionId, cancellationToken);
        if (sectionNode == null || sectionNode.Kind != DeclarationKind.MarkdownSection)
        {
            return null;
        }

        // Try to get content from metadata
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
        return $"Section from {sectionNode.FilePath} (lines {sectionNode.StartLine}-{sectionNode.EndLine})";
    }

    private async Task<string?> GetHeadingPathAsync(
        DeclarationNode sectionNode,
        CancellationToken cancellationToken)
    {
        // Get the parent heading hierarchy
        var incomingEdges = await _repository.GetIncomingEdgesAsync(sectionNode.Id, cancellationToken);
        var containsEdge = incomingEdges.FirstOrDefault(e => e.Kind == RelationshipKind.Contains);

        if (containsEdge == null)
        {
            return null;
        }

        var parentNode = await _repository.GetNodeAsync(containsEdge.SourceId, cancellationToken);
        if (parentNode == null)
        {
            return null;
        }

        // Build the heading path by traversing up
        var path = new List<string>();
        var currentNode = parentNode;

        while (currentNode != null && currentNode.Kind == DeclarationKind.MarkdownHeading)
        {
            path.Insert(0, currentNode.Name);

            var parentEdges = await _repository.GetIncomingEdgesAsync(currentNode.Id, cancellationToken);
            var parentContainsEdge = parentEdges.FirstOrDefault(e => e.Kind == RelationshipKind.Contains);

            if (parentContainsEdge == null)
            {
                break;
            }

            currentNode = await _repository.GetNodeAsync(parentContainsEdge.SourceId, cancellationToken);
        }

        return path.Count > 0 ? string.Join(" > ", path) : null;
    }

    private static string? GetTitleFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("title", out var titleElement))
            {
                return titleElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static string? GetContentHashFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("contentHash", out var hashElement))
            {
                return hashElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static int GetHeadingLevelFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return 1;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("level", out var levelElement))
            {
                return levelElement.GetInt32();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 1;
    }

    private static string? GetLinkTextFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("linkText", out var element))
            {
                return element.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static bool IsWikilinkFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("isWikilink", out var element))
            {
                return element.GetBoolean();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }

    private static int GetSourceLineFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return 0;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            if (doc.RootElement.TryGetProperty("sourceLine", out var element))
            {
                return element.GetInt32();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 0;
    }
}
