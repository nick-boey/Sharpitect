using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Service that manages graph building in the background.
/// </summary>
public sealed class GraphBuildingService : IGraphBuildingService
{
    private readonly TaskCompletionSource _completionSource = new();
    private volatile bool _isBuilding = true;
    private volatile bool _isComplete;
    private volatile bool _hasFailed;
    private volatile string? _buildStatus;
    private volatile string? _errorMessage;

    /// <inheritdoc />
    public bool IsBuilding => _isBuilding;

    /// <inheritdoc />
    public bool IsComplete => _isComplete;

    /// <inheritdoc />
    public bool HasFailed => _hasFailed;

    /// <inheritdoc />
    public string? BuildStatus => _buildStatus;

    /// <inheritdoc />
    public string? ErrorMessage => _errorMessage;

    /// <inheritdoc />
    public Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        return _completionSource.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Starts building the graph in the background.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file.</param>
    /// <param name="repository">The graph repository to persist to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void StartBuildAsync(string solutionPath, IGraphRepository repository, CancellationToken cancellationToken = default)
    {
        _ = BuildGraphAsync(solutionPath, repository, cancellationToken);
    }

    private async Task BuildGraphAsync(string solutionPath, IGraphRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            _buildStatus = "Initializing analysis...";

            var analyzer = new GraphSolutionAnalyzer(repository);

            _buildStatus = $"Analyzing solution: {Path.GetFileName(solutionPath)}";

            var graph = await analyzer.AnalyzeAsync(solutionPath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _buildStatus = $"Analysis complete: {graph.NodeCount} nodes, {graph.EdgeCount} edges";
            _isComplete = true;
            _isBuilding = false;
            _completionSource.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _errorMessage = "Build was cancelled";
            _hasFailed = true;
            _isBuilding = false;
            _completionSource.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _hasFailed = true;
            _isBuilding = false;
            _completionSource.TrySetException(ex);
        }
    }
}
