using System.CommandLine;
using Sharpitect.CLI.Commands;

var rootCommand = new RootCommand("Sharpitect - Analyze .NET codebases and build declaration graphs.")
{
    // Analysis commands
    AnalysisCommands.CreateAnalyzeCommand(),
    AnalysisCommands.CreateInitCommand(),

    // MCP server command
    ServerCommands.CreateServeCommand(),

    // Setup commands
    SetupCommands.CreateInstallCommand(),

    // Navigation commands
    NavigationCommands.CreateSearchCommand(),
    NavigationCommands.CreateNodeCommand(),
    NavigationCommands.CreateChildrenCommand(),
    NavigationCommands.CreateAncestorsCommand(),
    NavigationCommands.CreateRelationshipsCommand(),
    NavigationCommands.CreateCallersCommand(),
    NavigationCommands.CreateCalleesCommand(),
    NavigationCommands.CreateInheritanceCommand(),
    NavigationCommands.CreateListCommand(),
    NavigationCommands.CreateDependenciesCommand(),
    NavigationCommands.CreateDependentsCommand(),
    NavigationCommands.CreateFileCommand(),
    NavigationCommands.CreateUsagesCommand(),
    NavigationCommands.CreateSignatureCommand(),
    NavigationCommands.CreateCodeCommand(),
    NavigationCommands.CreateTreeCommand()
};

return await rootCommand.InvokeAsync(args);
