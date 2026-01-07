namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Service for indexing markdown files with embeddings.
/// </summary>
public interface IMarkdownIndexingService
{
    /// <summary>
    /// Indexes all markdown files in the solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The root directory to search for markdown files.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexing result with statistics.</returns>
    Task<MarkdownIndexingResult> IndexAllAsync(
        string solutionDirectory,
        IProgress<MarkdownIndexingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single markdown file (incremental update).
    /// </summary>
    /// <param name="filePath">Absolute path to the markdown file.</param>
    /// <param name="solutionRootDirectory">Root directory for computing relative paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexFileAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a markdown file from the index.
    /// </summary>
    /// <param name="filePath">Absolute path to the markdown file.</param>
    /// <param name="solutionRootDirectory">Root directory for computing relative paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveFileAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of markdown indexing operation.
/// </summary>
public sealed record MarkdownIndexingResult
{
    /// <summary>
    /// Number of documents that were indexed (new or updated).
    /// </summary>
    public int DocumentsIndexed { get; init; }

    /// <summary>
    /// Number of documents skipped (unchanged based on hash).
    /// </summary>
    public int DocumentsSkipped { get; init; }

    /// <summary>
    /// Total number of chunks created.
    /// </summary>
    public int ChunksCreated { get; init; }

    /// <summary>
    /// Total duration of the indexing operation.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Progress information for markdown indexing.
/// </summary>
public sealed record MarkdownIndexingProgress
{
    /// <summary>
    /// The file currently being processed.
    /// </summary>
    public string CurrentFile { get; init; } = "";

    /// <summary>
    /// Number of files processed so far.
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// Total number of files to process.
    /// </summary>
    public int TotalFiles { get; init; }
}
