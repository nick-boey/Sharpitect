using System.CommandLine;
using Sharpitect.Analysis.Persistence;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;
using Sharpitect.MCP.Services;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands used to debug the database created by Sharpitect.
/// </summary>
public static class DebugCommands
{
    public static Command CreateHealthCommand()
    {
        var command = new Command("health", "Check the health of the database.");

        command.SetHandler(async (_) =>
        {
            await ExecuteWithServiceAsync(null,
                async (service, formatter) => { await CheckDuplicateIds(service, formatter); });
            // TODO: Add checks for stale and updated code
        });
        return command;
    }

    private static async Task CheckDuplicateIds(IGraphNavigationService service, IOutputFormatter formatter)
    {
        Console.WriteLine("Checking for duplicate IDs...");

        var nodes = (await service.GetAllNodesAsync()).ToList();
        Console.WriteLine($"Found {nodes.Count} nodes.");

        var duplicateIds = nodes
            .OfType<NodeDetail>()
            .GroupBy(n => n.Id)
            .Where(g => g.Count() > 1).ToList();

        Console.WriteLine($"Found {duplicateIds.Count} duplicate IDs.");

        foreach (var group in duplicateIds)
        {
            Console.WriteLine($"Duplicate ID: {group.Key}");
            foreach (var node in group)
            {
                Console.WriteLine(formatter.Format(node));
            }
        }
    }

    // TODO: Make this a service instead
    private static async Task ExecuteWithServiceAsync(string? databasePath,
        Func<IGraphNavigationService, IOutputFormatter, Task> action)
    {
        var dbPath = ResolveDatabasePath(databasePath);
        if (dbPath == null)
        {
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            await using var repository = new SqliteGraphRepository(dbPath);
            await repository.InitializeAsync();

            var service = new GraphNavigationService(repository);
            var formatter = new TextOutputFormatter();

            await action(service, formatter);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static string? ResolveDatabasePath(string? databasePath)
    {
        if (!string.IsNullOrEmpty(databasePath))
        {
            if (!File.Exists(databasePath))
            {
                Console.Error.WriteLine($"Error: Database file not found: {databasePath}");
                Console.Error.WriteLine("Run 'sharpitect analyze' first to create the database.");
                return null;
            }

            return Path.GetFullPath(databasePath);
        }

        // Look for default database in current directory
        var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), ".sharpitect", "graph.db");
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        Console.Error.WriteLine(
            "Error: No database found. Specify a database path with --database or run 'sharpitect analyze' first.");
        return null;
    }
}