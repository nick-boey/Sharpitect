namespace Sharpitect.Analysis.Embedding;

/// <summary>
/// Service for generating text embeddings.
/// </summary>
public interface IEmbeddingService : IDisposable
{
    /// <summary>
    /// Gets the embedding dimension.
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Generates an embedding for a single text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts (batch processing).
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vectors in the same order as input texts.</returns>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
}
