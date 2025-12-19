using System.CommandLine;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Services;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for navigating the declaration graph.
/// </summary>
public static class NavigationCommands
{
    private static readonly Option<string> DatabaseOption = new(
        aliases: ["--database", "-d"],
        description: "Path to the SQLite database file. Defaults to .sharpitect/graph.db in current directory.")
    {
        IsRequired = false
    };

    private static readonly Option<int> LimitOption = new(
        aliases: ["--limit", "-l"],
        getDefaultValue: () => 50,
        description: "Maximum number of results to return.");

    private static readonly Argument<string> QueryArgument = new(
        name: "query",
        description: "Search text to match against declaration names.");

    private static readonly Option<SearchMatchMode> MatchModeOption = new(
        aliases: ["--match", "-m"],
        getDefaultValue: () => SearchMatchMode.Contains,
        description: "Match mode: Contains, StartsWith, EndsWith, or Exact.");

    private static readonly Option<DeclarationKind?> DeclarationKindOption = new(
        aliases: ["--kind", "-k"],
        description: "Filter by declaration kind (e.g., Class, Method, Property).");

    private static readonly Option<RelationshipKind?> RelationshipKindOption = new(
        aliases: ["--kind", "-k"],
        description: "Filter by relationship kind (e.g., Calls, Inherits, Implements).");

    private static readonly Option<bool> CaseSensitiveOption = new(
        aliases: ["--case-sensitive", "-c"],
        getDefaultValue: () => false,
        description: "Enable case-sensitive matching.");

    private static readonly Argument<string> IdArgument = new(
        name: "name",
        description: "Fully qualified name of the declaration (e.g., Namespace.Class.Method).");

    private static readonly Argument<string> ParentIdArgument = new(
        name: "parent-name",
        description: "Fully qualified name of the parent declaration (e.g., Namespace.Class).");

    private static readonly Option<RelationshipDirection> RelationshipDirectionOption = new(
        aliases: ["--direction"],
        getDefaultValue: () => RelationshipDirection.Both,
        description: "Filter by direction: Outgoing, Incoming, or Both.");

    private static readonly Option<InheritanceDirection> InheritanceDirectionOption = new(
        aliases: ["--direction"],
        getDefaultValue: () => InheritanceDirection.Both,
        description: "Filter by direction: Ancestors, Descendants, or Both.");

    private static readonly Option<int> DepthOption = new(
        aliases: ["--depth"],
        getDefaultValue: () => 1,
        description: "Maximum depth for transitive callers.");

    private static readonly Argument<string> ProjectIdArgument = new(
        name: "project-name",
        description: "Name or fully qualified name of the project.");

    private static readonly Option<bool> TransitiveOption = new(
        aliases: ["--transitive", "-t"],
        getDefaultValue: () => false,
        description: "Include transitive dependencies.");

    private static readonly Option<UsageKind?> UsageKindOption = new(
        aliases: ["--kind", "-k"],
        description: "Filter by usage kind: Call, TypeReference, Inheritance, Instantiation.");

    private static readonly Argument<string> FilePathArgument = new(
        name: "file-path",
        description: "Path to the source file.");

    public static Command CreateSearchCommand()
    {
        var command = new Command("search", "Search for declarations by name.")
        {
            QueryArgument,
            DatabaseOption,
            MatchModeOption,
            DeclarationKindOption,
            CaseSensitiveOption,
            LimitOption
        };

        command.SetHandler(async (query, database, matchMode, kind, caseSensitive, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var kindFilter = kind.HasValue ? new[] { kind.Value } : null;
                var result = await service.SearchAsync(query, matchMode, kindFilter, caseSensitive, limit);
                Console.WriteLine(formatter.Format(result));
            });
        }, QueryArgument, DatabaseOption, MatchModeOption, DeclarationKindOption, CaseSensitiveOption, LimitOption);

        return command;
    }

    public static Command CreateNodeCommand()
    {
        var command = new Command("node", "Get detailed information about a declaration node.")
        {
            IdArgument,
            DatabaseOption
        };

        command.SetHandler(async (id, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetNodeAsync(id);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {id}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption);

        return command;
    }

    public static Command CreateChildrenCommand()
    {
        var command = new Command("children", "Get children (contained declarations) of a node.")
        {
            ParentIdArgument,
            DatabaseOption,
            DeclarationKindOption,
            LimitOption
        };

        command.SetHandler(async (parentId, database, kind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetChildrenAsync(parentId, kind, limit);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {parentId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, ParentIdArgument, DatabaseOption, DeclarationKindOption, LimitOption);

        return command;
    }

    public static Command CreateAncestorsCommand()
    {
        var command = new Command("ancestors", "Get containment hierarchy path from root to a node.")
        {
            IdArgument,
            DatabaseOption
        };

        command.SetHandler(async (nodeId, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetAncestorsAsync(nodeId);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption);

        return command;
    }

    public static Command CreateRelationshipsCommand()
    {
        var command = new Command("relationships", "Get relationships (calls, inherits, references, etc.) for a node.")
        {
            IdArgument,
            DatabaseOption,
            RelationshipDirectionOption,
            RelationshipKindOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, direction, kind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetRelationshipsAsync(nodeId, direction, kind, limit);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, RelationshipDirectionOption, RelationshipKindOption, LimitOption);

        return command;
    }

    public static Command CreateCallersCommand()
    {
        var command = new Command("callers", "Find methods/properties that call a specific method.")
        {
            IdArgument,
            DatabaseOption,
            DepthOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, depth, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetCallersAsync(nodeId, depth, limit);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, DepthOption, LimitOption);

        return command;
    }

    public static Command CreateCalleesCommand()
    {
        var command = new Command("callees", "Find methods/properties called by a specific method.")
        {
            IdArgument,
            DatabaseOption,
            DepthOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, depth, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetCalleesAsync(nodeId, depth, limit);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, DepthOption, LimitOption);

        return command;
    }

    public static Command CreateInheritanceCommand()
    {
        var command = new Command("inheritance", "Get inheritance hierarchy (base types and derived types).")
        {
            IdArgument,
            DatabaseOption,
            InheritanceDirectionOption,
            DepthOption
        };

        command.SetHandler(async (nodeId, database, direction, depth) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetInheritanceAsync(nodeId, direction, depth);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, InheritanceDirectionOption, DepthOption);

        return command;
    }

    public static Command CreateListCommand()
    {
        var kindArgument = new Argument<DeclarationKind>(
            name: "kind",
            description: "Declaration kind to list (e.g., Class, Method, Property).");

        var scopeOption = new Option<string?>(
            aliases: ["--scope", "-s"],
            description: "Fully qualified name of a scope to limit search within.");

        var command = new Command("list", "List all declarations of a specific kind.")
        {
            kindArgument,
            DatabaseOption,
            scopeOption,
            LimitOption
        };

        command.SetHandler(async (kind, database, scope, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.ListByKindAsync(kind, scope, limit);
                Console.WriteLine(formatter.Format(result));
            });
        }, kindArgument, DatabaseOption, scopeOption, LimitOption);

        return command;
    }

    public static Command CreateDependenciesCommand()
    {
        var command = new Command("dependencies", "Get project-level dependencies.")
        {
            ProjectIdArgument,
            DatabaseOption,
            TransitiveOption
        };

        command.SetHandler(async (projectId, database, transitive) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetDependenciesAsync(projectId, transitive);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Project not found: {projectId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, ProjectIdArgument, DatabaseOption, TransitiveOption);

        return command;
    }

    public static Command CreateDependentsCommand()
    {
        var command = new Command("dependents", "Get projects that depend on a given project.")
        {
            ProjectIdArgument,
            DatabaseOption,
            TransitiveOption
        };

        command.SetHandler(async (projectId, database, transitive) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetDependentsAsync(projectId, transitive);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Project not found: {projectId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, ProjectIdArgument, DatabaseOption, TransitiveOption);

        return command;
    }

    public static Command CreateFileCommand()
    {
        var command = new Command("file", "Get all declarations in a specific source file.")
        {
            FilePathArgument,
            DatabaseOption
        };

        command.SetHandler(async (filePath, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetFileDeclarationsAsync(filePath);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: No declarations found for file: {filePath}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, FilePathArgument, DatabaseOption);

        return command;
    }

    public static Command CreateUsagesCommand()
    {
        var command = new Command("usages", "Find all usages of a type, method, or property.")
        {
            IdArgument,
            DatabaseOption,
            UsageKindOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, usageKind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetUsagesAsync(nodeId, usageKind, limit);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, UsageKindOption, LimitOption);

        return command;
    }

    public static Command CreateSignatureCommand()
    {
        var command = new Command("signature", "Get full signature and type information.")
        {
            IdArgument,
            DatabaseOption
        };

        command.SetHandler(async (nodeId, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetSignatureAsync(nodeId);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption);

        return command;
    }

    public static Command CreateCodeCommand()
    {
        var command = new Command("code", "Display declaration summary and source code.")
        {
            IdArgument,
            DatabaseOption
        };

        command.SetHandler(async (nodeId, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetCodeAsync(nodeId);
                if (result == null)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption);

        return command;
    }

    public static Command CreateTreeCommand()
    {
        var command = new Command("tree", "Display the containment tree starting from a node or solution root.")
        {
            IdArgument,
            DatabaseOption,
            DeclarationKindOption,
            DepthOption
        };

        command.SetHandler(async (nodeName, database, kind, depth) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetTreeAsync(nodeName, kind, depth);
                if (result.Roots.Count == 0)
                {
                    await Console.Error.WriteLineAsync($"Error: Node not found: {nodeName}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine(formatter.Format(result));
            });
        }, IdArgument, DatabaseOption, DeclarationKindOption, DepthOption);

        return command;
    }

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