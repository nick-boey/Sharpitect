using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Sharpitect.Analysis.Persistence;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Services;

namespace Sharpitect.MCP;

/// <summary>
/// Host builder extensions for the Sharpitect MCP server.
/// </summary>
public static class McpServerHost
{
    /// <summary>
    /// Creates a configured host builder for the Sharpitect MCP server with graph building support.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database for the graph.</param>
    /// <param name="buildingService">Optional graph building service for tracking build state.</param>
    public static IHostBuilder CreateHostBuilder(string databasePath, GraphBuildingService? buildingService = null)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register the graph repository
                services.AddSingleton<IGraphRepository>(_ => new SqliteGraphRepository(databasePath));

                // Register the graph building service if provided
                if (buildingService != null)
                {
                    services.AddSingleton<IGraphBuildingService>(buildingService);
                }
                else
                {
                    // Register a no-op building service that reports complete
                    services.AddSingleton<IGraphBuildingService, CompletedGraphBuildingService>();
                }

                // Register the navigation service
                services.AddSingleton<IGraphNavigationService, GraphNavigationService>();

                // Register the document search service (without embedding service for now)
                // Semantic search will be unavailable until embedding service is configured
                services.AddSingleton<IDocumentSearchService>(sp =>
                    new DocumentSearchService(
                        sp.GetRequiredService<IGraphRepository>(),
                        sp.GetService<IVectorSearchService>()));

                // Register the output formatter factory
                services.AddSingleton<IOutputFormatterFactory, OutputFormatterFactory>();

                // Register the tool result builder for MCP responses
                services.AddSingleton<TextOutputFormatter>();
                services.AddSingleton<ToolResultBuilder>();

                // Configure MCP server
                services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly();
            });
    }

    /// <summary>
    /// Runs the MCP server and builds the graph from a solution in the background.
    /// The server starts immediately but tools return "building" status until analysis is complete.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file to analyze.</param>
    /// <param name="databasePath">Path to the SQLite database for the graph.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RunWithBuildAsync(string solutionPath, string databasePath, CancellationToken cancellationToken = default)
    {
        // Create the building service
        var buildingService = new GraphBuildingService();

        var host = CreateHostBuilder(databasePath, buildingService).Build();

        // Initialize the repository
        var repository = host.Services.GetRequiredService<IGraphRepository>();
        await repository.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Start the graph build in the background
        buildingService.StartBuildAsync(solutionPath, repository, cancellationToken);

        // Run the server (this blocks until shutdown)
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the MCP server with a pre-built database.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database containing the analyzed graph.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RunAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        var host = CreateHostBuilder(databasePath).Build();

        // Initialize the repository
        var repository = host.Services.GetRequiredService<IGraphRepository>();
        await repository.InitializeAsync().ConfigureAwait(false);

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// A no-op graph building service that reports the build as already complete.
/// Used when the server is started with a pre-built database.
/// </summary>
internal sealed class CompletedGraphBuildingService : IGraphBuildingService
{
    public bool IsBuilding => false;
    public bool IsComplete => true;
    public bool HasFailed => false;
    public string? BuildStatus => null;
    public string? ErrorMessage => null;

    public Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
