using System.Collections.Concurrent;

namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// File watcher that debounces rapid changes and filters for C# files.
/// </summary>
public sealed class DebouncedFileChangeWatcher : IFileChangeWatcher
{
    private readonly TimeSpan _debounceInterval;
    private readonly ConcurrentDictionary<string, FileChange> _pendingChanges = new();
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private bool _isWatching;

    /// <summary>
    /// Creates a new debounced file change watcher.
    /// </summary>
    /// <param name="debounceInterval">Interval to wait before firing events. Default is 300ms.</param>
    public DebouncedFileChangeWatcher(TimeSpan? debounceInterval = null)
    {
        _debounceInterval = debounceInterval ?? TimeSpan.FromMilliseconds(300);
    }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<FileChange>>? ChangesDetected;

    /// <inheritdoc />
    public bool IsWatching => _isWatching;

    /// <inheritdoc />
    public Task StartAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (_isWatching)
        {
            return Task.CompletedTask;
        }

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.cs",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;

        _isWatching = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isWatching)
        {
            return Task.CompletedTask;
        }

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileCreated;
            _watcher.Changed -= OnFileChanged;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Dispose();
            _watcher = null;
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;

        _isWatching = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        QueueChange(new FileChange(e.FullPath, FileChangeKind.Created));
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        QueueChange(new FileChange(e.FullPath, FileChangeKind.Modified));
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        QueueChange(new FileChange(e.FullPath, FileChangeKind.Deleted));
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // If renaming from .cs to non-.cs, treat as delete
        if (!e.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            if (e.OldFullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                QueueChange(new FileChange(e.OldFullPath, FileChangeKind.Deleted));
            }
            return;
        }

        // If renaming from non-.cs to .cs, treat as create
        if (!e.OldFullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            QueueChange(new FileChange(e.FullPath, FileChangeKind.Created));
            return;
        }

        // Both are .cs files, it's a rename
        QueueChange(new FileChange(e.FullPath, FileChangeKind.Renamed, e.OldFullPath));
    }

    private void QueueChange(FileChange change)
    {
        // Use the file path as key, overwrite any pending change for this file
        // Priority: Delete > Rename > Create > Modified
        _pendingChanges.AddOrUpdate(
            change.FilePath,
            change,
            (_, existing) => MergeChanges(existing, change));

        // Reset the debounce timer
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(OnDebounceElapsed, null, _debounceInterval, Timeout.InfiniteTimeSpan);
    }

    private static FileChange MergeChanges(FileChange existing, FileChange incoming)
    {
        // If new change is delete, it takes precedence (file no longer exists)
        if (incoming.Kind == FileChangeKind.Deleted)
        {
            return incoming;
        }

        // If existing is delete, incoming must be create (file recreated)
        if (existing.Kind == FileChangeKind.Deleted)
        {
            return incoming;
        }

        // Multiple modifications become single modification
        // Keep the latest change type
        return incoming;
    }

    private void OnDebounceElapsed(object? state)
    {
        if (_pendingChanges.IsEmpty)
        {
            return;
        }

        // Take all pending changes
        var changes = new List<FileChange>();
        foreach (var key in _pendingChanges.Keys.ToList())
        {
            if (_pendingChanges.TryRemove(key, out var change))
            {
                changes.Add(change);
            }
        }

        if (changes.Count > 0)
        {
            ChangesDetected?.Invoke(this, changes);
        }
    }
}
