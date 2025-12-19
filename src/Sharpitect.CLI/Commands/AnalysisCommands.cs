using System.CommandLine;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for analyzing solutions and initializing configuration.
/// </summary>
public static class AnalysisCommands
{
    public static Command CreateAnalyzeCommand()
    {
        var pathArgument = new Argument<string?>(
            name: "path",
            description: "Path to a .sln file or directory containing one. Defaults to current directory.",
            getDefaultValue: () => null);

        var outputOption = new Option<string?>(
            name: "--output",
            description: "Path to the SQLite database file. Defaults to .sharpitect/graph.db in the solution directory.");

        var command = new Command("analyze", "Analyze a .NET solution and build the declaration graph.")
        {
            pathArgument,
            outputOption
        };

        command.SetHandler(HandleAnalyzeCommand, pathArgument, outputOption);
        return command;
    }

    public static Command CreateInitCommand()
    {
        var pathArgument = new Argument<string?>(
            name: "path",
            description: "Path to a directory containing a .sln or .slnx file. Defaults to current directory.",
            getDefaultValue: () => null);

        var command = new Command("init", "Initialize a new .sln.yml configuration file for a solution.")
        {
            pathArgument
        };

        command.SetHandler(HandleInitCommand, pathArgument);
        return command;
    }

    private static async Task HandleAnalyzeCommand(string? path, string? outputPath)
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

        Console.WriteLine($"Analyzing solution: {solutionPath}");
        Console.WriteLine($"Output database: {dbPath}");

        try
        {
            await using var repository = new SqliteGraphRepository(dbPath);
            var analyzer = new GraphSolutionAnalyzer(repository);

            var graph = await analyzer.AnalyzeAsync(solutionPath);

            Console.WriteLine();
            Console.WriteLine($"Analysis complete:");
            Console.WriteLine($"  Nodes: {graph.NodeCount}");
            Console.WriteLine($"  Edges: {graph.EdgeCount}");
            Console.WriteLine();
            Console.WriteLine($"Graph saved to: {dbPath}");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error analyzing solution: {ex.Message}");
            if (ex.InnerException != null)
            {
                await Console.Error.WriteLineAsync($"  Inner: {ex.InnerException.Message}");
            }
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

    private static void HandleInitCommand(string? path)
    {
        var targetPath = string.IsNullOrEmpty(path) ? Directory.GetCurrentDirectory() : path;

        if (!Directory.Exists(targetPath))
        {
            Console.Error.WriteLine($"Error: Directory not found: {targetPath}");
            Environment.ExitCode = 1;
            return;
        }

        var slnFiles = Directory.GetFiles(targetPath, "*.sln")
            .Concat(Directory.GetFiles(targetPath, "*.slnx"))
            .ToArray();

        if (slnFiles.Length == 0)
        {
            Console.Error.WriteLine($"Error: No .sln or .slnx file found in: {Path.GetFullPath(targetPath)}");
            Environment.ExitCode = 1;
            return;
        }

        if (slnFiles.Length > 1)
        {
            Console.Error.WriteLine($"Error: Multiple solution files found in: {targetPath}");
            foreach (var sln in slnFiles)
            {
                Console.Error.WriteLine($"  - {Path.GetFileName(sln)}");
            }
            Console.Error.WriteLine("Please specify which solution to initialize.");
            Environment.ExitCode = 1;
            return;
        }

        var solutionPath = slnFiles[0];
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        var configFileName = Path.GetFileName(solutionPath) + ".yml";
        var configFilePath = Path.Combine(Path.GetDirectoryName(solutionPath)!, configFileName);

        if (File.Exists(configFilePath))
        {
            Console.Error.WriteLine($"Error: Configuration file already exists: {configFileName}");
            Environment.ExitCode = 1;
            return;
        }

        var yamlContent = $"""
            # Sharpitect C4 Configuration for {solutionName}
            # See documentation for full configuration options.

            system:
              name: "{solutionName}"
              description: "Description of the {solutionName} system."

            # Define people/actors who interact with the system
            people:
              - name: "User"
                description: "A user of the system."

            # Define external systems that this system interacts with
            externalSystems: []
            #  - name: "External Service"
            #    description: "An external service the system depends on."

            # Define external containers (databases, message queues, etc.)
            externalContainers: []
            #  - name: "Database"
            #    description: "The database used by the system."
            #    technology: "PostgreSQL"

            # Define relationships between elements
            relationships: []
            #  - from: "User"
            #    to: "{solutionName}"
            #    description: "Uses"
            """;

        File.WriteAllText(configFilePath, yamlContent);
        Console.WriteLine($"Created: {configFileName}");
    }
}
