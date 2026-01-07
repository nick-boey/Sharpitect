using System.Diagnostics;
using System.Text.Json;
using Sharpitect.Analysis.Embedding;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Service for indexing markdown files with embeddings into the declaration graph.
/// </summary>
public sealed class MarkdownIndexingService : IMarkdownIndexingService
{
    private readonly IGraphRepository _repository;
    private readonly IMarkdownAnalyzer _analyzer;
    private readonly IEmbeddingService? _embeddingService;

    /// <summary>
    /// Creates a new markdown indexing service.
    /// </summary>
    /// <param name="repository">The graph repository for persistence.</param>
    /// <param name="analyzer">The markdown analyzer.</param>
    /// <param name="embeddingService">Optional embedding service for vector search. If null, embeddings are skipped.</param>
    public MarkdownIndexingService(
        IGraphRepository repository,
        IMarkdownAnalyzer analyzer,
        IEmbeddingService? embeddingService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public async Task<MarkdownIndexingResult> IndexAllAsync(
        string solutionDirectory,
        IProgress<MarkdownIndexingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(solutionDirectory);

        var stopwatch = Stopwatch.StartNew();

        // Find all markdown files
        var markdownFiles = Directory.GetFiles(solutionDirectory, "*.md", SearchOption.AllDirectories)
            .Where(f => !IsExcludedPath(f))
            .ToList();

        var documentsIndexed = 0;
        var documentsSkipped = 0;
        var chunksCreated = 0;

        for (int i = 0; i < markdownFiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = markdownFiles[i];
            var relativePath = GetRelativePath(filePath, solutionDirectory);

            progress?.Report(new MarkdownIndexingProgress
            {
                CurrentFile = relativePath,
                ProcessedFiles = i,
                TotalFiles = markdownFiles.Count
            });

            // Check if document already exists and has same hash
            var existingNode = await _repository.GetNodeAsync(relativePath, cancellationToken);
            if (existingNode != null)
            {
                var existingHash = GetContentHashFromMetadata(existingNode.Metadata);
                var currentHash = await ComputeFileHashAsync(filePath, cancellationToken);

                if (existingHash == currentHash)
                {
                    documentsSkipped++;
                    continue;
                }

                // Remove existing nodes for this document
                await RemoveDocumentNodesAsync(relativePath, cancellationToken);
            }

            // Analyze and index the file
            var result = await _analyzer.AnalyzeAsync(filePath, solutionDirectory, cancellationToken);

            // Save nodes
            await _repository.SaveNodesAsync(result.Nodes, cancellationToken);

            // Save edges
            await _repository.SaveEdgesAsync(result.Edges, cancellationToken);

            // Generate and save embeddings for sections
            if (_embeddingService != null && result.SectionContents.Count > 0)
            {
                var embeddings = new List<(string NodeId, float[] Embedding)>();

                foreach (var (nodeId, content) in result.SectionContents)
                {
                    var embedding = await _embeddingService.EmbedAsync(content, cancellationToken);
                    embeddings.Add((nodeId, embedding));
                }

                await _repository.SaveEmbeddingsAsync(embeddings, cancellationToken);
            }

            documentsIndexed++;
            chunksCreated += result.SectionContents.Count;
        }

        progress?.Report(new MarkdownIndexingProgress
        {
            CurrentFile = "",
            ProcessedFiles = markdownFiles.Count,
            TotalFiles = markdownFiles.Count
        });

        stopwatch.Stop();

        return new MarkdownIndexingResult
        {
            DocumentsIndexed = documentsIndexed,
            DocumentsSkipped = documentsSkipped,
            ChunksCreated = chunksCreated,
            Duration = stopwatch.Elapsed
        };
    }

    /// <inheritdoc />
    public async Task IndexFileAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(solutionRootDirectory);

        var relativePath = GetRelativePath(filePath, solutionRootDirectory);

        // Remove existing nodes for this document
        await RemoveDocumentNodesAsync(relativePath, cancellationToken);

        // Analyze and index the file
        var result = await _analyzer.AnalyzeAsync(filePath, solutionRootDirectory, cancellationToken);

        // Save nodes
        await _repository.SaveNodesAsync(result.Nodes, cancellationToken);

        // Save edges
        await _repository.SaveEdgesAsync(result.Edges, cancellationToken);

        // Generate and save embeddings for sections
        if (_embeddingService != null && result.SectionContents.Count > 0)
        {
            var embeddings = new List<(string NodeId, float[] Embedding)>();

            foreach (var (nodeId, content) in result.SectionContents)
            {
                var embedding = await _embeddingService.EmbedAsync(content, cancellationToken);
                embeddings.Add((nodeId, embedding));
            }

            await _repository.SaveEmbeddingsAsync(embeddings, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveFileAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(solutionRootDirectory);

        var relativePath = GetRelativePath(filePath, solutionRootDirectory);
        await RemoveDocumentNodesAsync(relativePath, cancellationToken);
    }

    private async Task RemoveDocumentNodesAsync(string documentId, CancellationToken cancellationToken)
    {
        // Get all nodes for this document (document, headings, sections)
        var nodes = await _repository.GetNodesByFileAsync(documentId, cancellationToken);

        if (nodes.Count == 0)
        {
            return;
        }

        // Delete embeddings first (for section nodes)
        var sectionNodes = nodes.Where(n => n.Kind == DeclarationKind.MarkdownSection);
        foreach (var section in sectionNodes)
        {
            await _repository.DeleteEmbeddingAsync(section.Id, cancellationToken);
        }

        // Delete all nodes (cascade will handle edges)
        await _repository.DeleteNodesAsync(nodes.Select(n => n.Id), cancellationToken);
    }

    private static bool IsExcludedPath(string filePath)
    {
        // Exclude common directories that shouldn't be indexed
        var excludedPatterns = new[]
        {
            "/node_modules/",
            "/.git/",
            "/bin/",
            "/obj/",
            "/.vs/",
            "/packages/",
            "/TestResults/"
        };

        var normalizedPath = filePath.Replace('\\', '/');
        return excludedPatterns.Any(pattern =>
            normalizedPath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRelativePath(string filePath, string rootDirectory)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fullRoot = Path.GetFullPath(rootDirectory);

        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath[fullRoot.Length..].Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
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

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
