using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Manages incremental updates to the declaration graph when files change.
/// </summary>
public interface IIncrementalGraphUpdateService : IAsyncDisposable
{
    /// <summary>
    /// Event raised when a graph update completes.
    /// </summary>
    event EventHandler<GraphUpdateEventArgs>? UpdateCompleted;

    /// <summary>
    /// Event raised when an error occurs during update.
    /// </summary>
    event EventHandler<GraphUpdateErrorEventArgs>? UpdateError;

    /// <summary>
    /// Starts watching for file changes and processing updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops watching for file changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers an update for specific files.
    /// </summary>
    /// <param name="filePaths">The file paths to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the service.
    /// </summary>
    IncrementalUpdateState State { get; }

    /// <summary>
    /// Gets the current in-memory graph.
    /// </summary>
    DeclarationGraph Graph { get; }
}
