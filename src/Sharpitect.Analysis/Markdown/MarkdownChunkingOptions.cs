namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Configuration options for markdown chunking.
/// </summary>
public sealed record MarkdownChunkingOptions
{
    /// <summary>
    /// Maximum characters per chunk. Default: 1000.
    /// </summary>
    public int MaxChunkSize { get; init; } = 1000;

    /// <summary>
    /// Overlap between chunks when splitting large sections. Default: 100.
    /// </summary>
    public int ChunkOverlap { get; init; } = 100;

    /// <summary>
    /// Minimum chunk size to keep. Default: 50.
    /// </summary>
    public int MinChunkSize { get; init; } = 50;
}
