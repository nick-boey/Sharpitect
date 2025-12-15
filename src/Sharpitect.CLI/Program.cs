using System.CommandLine;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Output;

var pathArgument = new Argument<string?>(
    name: "path",
    description: "Path to a .sln file or directory containing one. Defaults to current directory.",
    getDefaultValue: () => null);

var debugOption = new Option<bool>(
    name: "--debug",
    description: "Output the architecture model as a debug tree.");

var buildCommand = new Command("build", "Analyze a .NET solution and build the architecture model.")
{
    pathArgument,
    debugOption
};

buildCommand.SetHandler(HandleBuildCommand, pathArgument, debugOption);

var initPathArgument = new Argument<string?>(
    name: "path",
    description: "Path to a directory containing a .sln or .slnx file. Defaults to current directory.",
    getDefaultValue: () => null);

var initCommand = new Command("init", "Initialize a new .sln.yml configuration file for a solution.")
{
    initPathArgument
};

initCommand.SetHandler(HandleInitCommand, initPathArgument);

var rootCommand = new RootCommand("Sharpitect - Generate C4 architecture diagrams from annotated C# codebases.")
{
    buildCommand,
    initCommand
};

return await rootCommand.InvokeAsync(args);

static void HandleBuildCommand(string? path, bool debug)
{
    var solutionPath = ResolveSolutionPath(path);
    if (solutionPath == null)
    {
        Environment.ExitCode = 1;
        return;
    }

    if (!debug)
    {
        Console.Error.WriteLine("Error: An output format is required. Use --debug to output the architecture model.");
        Environment.ExitCode = 1;
        return;
    }

    var sourceProvider = new FileSystemSourceProvider();
    var analyzer = new SolutionAnalyzer(sourceProvider);

    try
    {
        var model = analyzer.Analyze(solutionPath);
        var printer = new DebugPrinter();
        printer.Write(model, Console.Out);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error analyzing solution: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static string? ResolveSolutionPath(string? path)
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

static void HandleInitCommand(string? path)
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