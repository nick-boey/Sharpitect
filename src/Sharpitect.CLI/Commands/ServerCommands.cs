using System.CommandLine;
using Sharpitect.MCP;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for running the MCP server.
/// </summary>
public static class ServerCommands
{
    public static Command CreateServeCommand()
    {
        var databaseArgument = new Argument<string>(
            name: "database",
            description: "Path to the SQLite database file containing the analyzed graph.");

        var command = new Command("serve", "Start the MCP server for IDE integration.")
        {
            databaseArgument
        };

        command.SetHandler(HandleServeCommand, databaseArgument);
        return command;
    }

    private static async Task HandleServeCommand(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            await Console.Error.WriteLineAsync($"Error: Database file not found: {databasePath}");
            await Console.Error.WriteLineAsync("Run 'sharpitect analyze' first to create the database.");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            await McpServerHost.RunAsync(databasePath);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error running MCP server: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
