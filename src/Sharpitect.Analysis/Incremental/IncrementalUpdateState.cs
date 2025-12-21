namespace Sharpitect.Analysis.Incremental;

/// <summary>
/// State of the incremental update service.
/// </summary>
public enum IncrementalUpdateState
{
    /// <summary>
    /// The service is stopped and not watching for changes.
    /// </summary>
    Stopped,

    /// <summary>
    /// The service is watching for file changes.
    /// </summary>
    Watching,

    /// <summary>
    /// The service is currently processing an update.
    /// </summary>
    Updating
}
