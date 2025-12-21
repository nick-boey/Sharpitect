namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Watches for file changes in a directory.
/// </summary>
public interface IFileChangeWatcher : IAsyncDisposable
{
    /// <summary>
    /// Event raised when file changes are detected (after debouncing).
    /// </summary>
    event EventHandler<IReadOnlyList<FileChange>>? ChangesDetected;

    /// <summary>
    /// Starts watching the specified directory.
    /// </summary>
    /// <param name="directory">The directory to watch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(string directory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops watching for changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the watcher is currently running.
    /// </summary>
    bool IsWatching { get; }
}
