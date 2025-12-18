using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Sharpitect.Analysis.Persistence;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Services;

namespace Sharpitect.MCP;

/// <summary>
/// Host builder extensions for the Sharpitect MCP server.
/// </summary>
public static class McpServerHost
{
    /// <summary>
    /// Creates a configured host builder for the Sharpitect MCP server.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database containing the analyzed graph.</param>
    public static IHostBuilder CreateHostBuilder(string databasePath)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register the graph repository
                services.AddSingleton<IGraphRepository>(_ => new SqliteGraphRepository(databasePath));

                // Register the navigation service
                services.AddSingleton<IGraphNavigationService, GraphNavigationService>();

                // Register the output formatter factory
                services.AddSingleton<IOutputFormatterFactory, OutputFormatterFactory>();

                // Configure MCP server
                services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly();
            });
    }

    /// <summary>
    /// Runs the MCP server with the specified database.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database containing the analyzed graph.</param>
    public static async Task RunAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        var host = CreateHostBuilder(databasePath).Build();

        // Initialize the repository
        var repository = host.Services.GetRequiredService<IGraphRepository>();
        await repository.InitializeAsync().ConfigureAwait(false);

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }
}
