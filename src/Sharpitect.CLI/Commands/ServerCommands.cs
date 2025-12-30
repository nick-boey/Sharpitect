using System.CommandLine;
using Sharpitect.MCP;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for running the MCP server.
/// </summary>
public static class ServerCommands
{
    private static readonly Argument<string?> PathArgument = new(
        name: "path",
        description: "Path to a .sln file or directory containing one. Defaults to current directory.",
        getDefaultValue: () => null);

    private static readonly Option<string?> OutputOption = new(
        name: "--output",
        description:
        "Path to the SQLite database file. Defaults to .sharpitect/graph.db in the solution directory.");

    public static Command CreateServeCommand()
    {
        var command = new Command("serve", "Start the MCP server for IDE integration. Builds the graph on startup.")
        {
            PathArgument,
            OutputOption
        };

        command.SetHandler(HandleServeCommand, PathArgument, OutputOption);
        return command;
    }

    private static async Task HandleServeCommand(string? path, string? outputPath)
    {
        var solutionPath = ResolveSolutionPath(path);
        if (solutionPath == null)
        {
            Environment.ExitCode = 1;
            return;
        }

        // Determine database path
        var dbPath = outputPath;
        if (string.IsNullOrEmpty(dbPath))
        {
            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var sharpitectDir = Path.Combine(solutionDir, ".sharpitect");
            Directory.CreateDirectory(sharpitectDir);
            dbPath = Path.Combine(sharpitectDir, "graph.db");
        }

        try
        {
            await McpServerHost.RunWithBuildAsync(solutionPath, dbPath);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error running MCP server: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static string? ResolveSolutionPath(string? path)
    {
        // Default to current directory if no path provided
        var targetPath = string.IsNullOrEmpty(path) ? Directory.GetCurrentDirectory() : path;

        // If it's a .sln file, use it directly
        if (targetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetPath))
            {
                Console.Error.WriteLine($"Error: Solution file not found: {targetPath}");
                return null;
            }

            return Path.GetFullPath(targetPath);
        }

        // Otherwise, treat it as a directory and look for .sln files
        var searchPath = targetPath;

        // Handle case where path doesn't have .sln but might be a file path
        if (File.Exists(targetPath + ".sln"))
        {
            return Path.GetFullPath(targetPath + ".sln");
        }

        if (!Directory.Exists(searchPath))
        {
            Console.Error.WriteLine($"Error: Directory not found: {searchPath}");
            return null;
        }

        var slnFiles = Directory.GetFiles(searchPath, "*.sln");

        if (slnFiles.Length == 0)
        {
            Console.Error.WriteLine($"Error: No .sln file found in: {Path.GetFullPath(searchPath)}");
            return null;
        }

        if (slnFiles.Length > 1)
        {
            Console.Error.WriteLine($"Error: Multiple .sln files found in: {searchPath}");
            foreach (var sln in slnFiles)
            {
                Console.Error.WriteLine($"  - {Path.GetFileName(sln)}");
            }

            Console.Error.WriteLine("Please specify which solution to use.");
            return null;
        }

        return Path.GetFullPath(slnFiles[0]);
    }
}