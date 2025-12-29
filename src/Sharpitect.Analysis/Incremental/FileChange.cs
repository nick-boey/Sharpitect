namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// Type of file change detected.
/// </summary>
public enum FileChangeKind
{
    /// <summary>
    /// A new file was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file was renamed.
    /// </summary>
    Renamed
}

/// <summary>
/// Represents a file change event.
/// </summary>
/// <param name="FilePath">The path of the file that changed.</param>
/// <param name="Kind">The type of change.</param>
/// <param name="OldFilePath">For renames, the previous file path.</param>
public sealed record FileChange(
    string FilePath,
    FileChangeKind Kind,
    string? OldFilePath = null);
