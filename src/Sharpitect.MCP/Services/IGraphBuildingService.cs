namespace Sharpitect.MCP.Services;

/// <summary>
/// Service for tracking the state of graph building operations.
/// </summary>
public interface IGraphBuildingService
{
    /// <summary>
    /// Gets whether the graph is currently being built.
    /// </summary>
    bool IsBuilding { get; }

    /// <summary>
    /// Gets whether the graph build has completed successfully.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Gets whether the graph build failed.
    /// </summary>
    bool HasFailed { get; }

    /// <summary>
    /// Gets the current build status message, if any.
    /// </summary>
    string? BuildStatus { get; }

    /// <summary>
    /// Gets the error message if the build failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Waits for the build to complete.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the build is done.</returns>
    Task WaitForCompletionAsync(CancellationToken cancellationToken = default);
}
