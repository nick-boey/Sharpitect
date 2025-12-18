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

    public static Command CreateSearchCommand()
    {
        var queryArgument = new Argument<string>(
            name: "query",
            description: "Search text to match against declaration names.");

        var matchModeOption = new Option<SearchMatchMode>(
            aliases: ["--match", "-m"],
            getDefaultValue: () => SearchMatchMode.Contains,
            description: "Match mode: Contains, StartsWith, EndsWith, or Exact.");

        var kindOption = new Option<DeclarationKind?>(
            aliases: ["--kind", "-k"],
            description: "Filter by declaration kind (e.g., Class, Method, Property).");

        var caseSensitiveOption = new Option<bool>(
            aliases: ["--case-sensitive", "-c"],
            getDefaultValue: () => false,
            description: "Enable case-sensitive matching.");

        var command = new Command("search", "Search for declarations by name.")
        {
            queryArgument,
            DatabaseOption,
            matchModeOption,
            kindOption,
            caseSensitiveOption,
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
        }, queryArgument, DatabaseOption, matchModeOption, kindOption, caseSensitiveOption, LimitOption);

        return command;
    }

    public static Command CreateNodeCommand()
    {
        var idArgument = new Argument<string>(
            name: "id",
            description: "Fully qualified ID of the declaration node.");

        var command = new Command("node", "Get detailed information about a declaration node.")
        {
            idArgument,
            DatabaseOption
        };

        command.SetHandler(async (id, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetNodeAsync(id);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {id}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, idArgument, DatabaseOption);

        return command;
    }

    public static Command CreateChildrenCommand()
    {
        var parentIdArgument = new Argument<string>(
            name: "parent-id",
            description: "Fully qualified ID of the parent node.");

        var kindOption = new Option<DeclarationKind?>(
            aliases: ["--kind", "-k"],
            description: "Filter children by declaration kind.");

        var command = new Command("children", "Get children (contained declarations) of a node.")
        {
            parentIdArgument,
            DatabaseOption,
            kindOption,
            LimitOption
        };

        command.SetHandler(async (parentId, database, kind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetChildrenAsync(parentId, kind, limit);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {parentId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, parentIdArgument, DatabaseOption, kindOption, LimitOption);

        return command;
    }

    public static Command CreateAncestorsCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the node.");

        var command = new Command("ancestors", "Get containment hierarchy path from root to a node.")
        {
            nodeIdArgument,
            DatabaseOption
        };

        command.SetHandler(async (nodeId, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetAncestorsAsync(nodeId);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption);

        return command;
    }

    public static Command CreateRelationshipsCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the node.");

        var directionOption = new Option<RelationshipDirection>(
            aliases: ["--direction"],
            getDefaultValue: () => RelationshipDirection.Both,
            description: "Filter by direction: Outgoing, Incoming, or Both.");

        var kindOption = new Option<RelationshipKind?>(
            aliases: ["--kind", "-k"],
            description: "Filter by relationship kind (e.g., Calls, Inherits, Implements).");

        var command = new Command("relationships", "Get relationships (calls, inherits, references, etc.) for a node.")
        {
            nodeIdArgument,
            DatabaseOption,
            directionOption,
            kindOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, direction, kind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetRelationshipsAsync(nodeId, direction, kind, limit);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption, directionOption, kindOption, LimitOption);

        return command;
    }

    public static Command CreateCallersCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the method or property.");

        var depthOption = new Option<int>(
            aliases: ["--depth"],
            getDefaultValue: () => 1,
            description: "Maximum depth for transitive callers.");

        var command = new Command("callers", "Find methods/properties that call a specific method.")
        {
            nodeIdArgument,
            DatabaseOption,
            depthOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, depth, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetCallersAsync(nodeId, depth, limit);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption, depthOption, LimitOption);

        return command;
    }

    public static Command CreateCalleesCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the method or property.");

        var depthOption = new Option<int>(
            aliases: ["--depth"],
            getDefaultValue: () => 1,
            description: "Maximum depth for transitive callees.");

        var command = new Command("callees", "Find methods/properties called by a specific method.")
        {
            nodeIdArgument,
            DatabaseOption,
            depthOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, depth, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetCalleesAsync(nodeId, depth, limit);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption, depthOption, LimitOption);

        return command;
    }

    public static Command CreateInheritanceCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the class or interface.");

        var directionOption = new Option<InheritanceDirection>(
            aliases: ["--direction"],
            getDefaultValue: () => InheritanceDirection.Both,
            description: "Filter by direction: Ancestors, Descendants, or Both.");

        var depthOption = new Option<int>(
            aliases: ["--depth"],
            getDefaultValue: () => 10,
            description: "Maximum depth for inheritance traversal.");

        var command = new Command("inheritance", "Get inheritance hierarchy (base types and derived types).")
        {
            nodeIdArgument,
            DatabaseOption,
            directionOption,
            depthOption
        };

        command.SetHandler(async (nodeId, database, direction, depth) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetInheritanceAsync(nodeId, direction, depth);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption, directionOption, depthOption);

        return command;
    }

    public static Command CreateListCommand()
    {
        var kindArgument = new Argument<DeclarationKind>(
            name: "kind",
            description: "Declaration kind to list (e.g., Class, Method, Property).");

        var scopeOption = new Option<string?>(
            aliases: ["--scope", "-s"],
            description: "Scope node ID to limit search within.");

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
        var projectIdArgument = new Argument<string>(
            name: "project-id",
            description: "Fully qualified ID of the project.");

        var transitiveOption = new Option<bool>(
            aliases: ["--transitive", "-t"],
            getDefaultValue: () => false,
            description: "Include transitive dependencies.");

        var command = new Command("dependencies", "Get project-level dependencies.")
        {
            projectIdArgument,
            DatabaseOption,
            transitiveOption
        };

        command.SetHandler(async (projectId, database, transitive) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetDependenciesAsync(projectId, transitive);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Project not found: {projectId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, projectIdArgument, DatabaseOption, transitiveOption);

        return command;
    }

    public static Command CreateDependentsCommand()
    {
        var projectIdArgument = new Argument<string>(
            name: "project-id",
            description: "Fully qualified ID of the project.");

        var transitiveOption = new Option<bool>(
            aliases: ["--transitive", "-t"],
            getDefaultValue: () => false,
            description: "Include transitive dependents.");

        var command = new Command("dependents", "Get projects that depend on a given project.")
        {
            projectIdArgument,
            DatabaseOption,
            transitiveOption
        };

        command.SetHandler(async (projectId, database, transitive) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetDependentsAsync(projectId, transitive);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Project not found: {projectId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, projectIdArgument, DatabaseOption, transitiveOption);

        return command;
    }

    public static Command CreateFileCommand()
    {
        var filePathArgument = new Argument<string>(
            name: "file-path",
            description: "Path to the source file.");

        var command = new Command("file", "Get all declarations in a specific source file.")
        {
            filePathArgument,
            DatabaseOption
        };

        command.SetHandler(async (filePath, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetFileDeclarationsAsync(filePath);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: No declarations found for file: {filePath}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, filePathArgument, DatabaseOption);

        return command;
    }

    public static Command CreateUsagesCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the type, method, or property.");

        var usageKindOption = new Option<UsageKind?>(
            aliases: ["--kind", "-k"],
            description: "Filter by usage kind: Call, TypeReference, Inheritance, Instantiation.");

        var command = new Command("usages", "Find all usages of a type, method, or property.")
        {
            nodeIdArgument,
            DatabaseOption,
            usageKindOption,
            LimitOption
        };

        command.SetHandler(async (nodeId, database, usageKind, limit) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetUsagesAsync(nodeId, usageKind, limit);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption, usageKindOption, LimitOption);

        return command;
    }

    public static Command CreateSignatureCommand()
    {
        var nodeIdArgument = new Argument<string>(
            name: "node-id",
            description: "Fully qualified ID of the method, property, or type.");

        var command = new Command("signature", "Get full signature and type information.")
        {
            nodeIdArgument,
            DatabaseOption
        };

        command.SetHandler(async (nodeId, database) =>
        {
            await ExecuteWithServiceAsync(database, async (service, formatter) =>
            {
                var result = await service.GetSignatureAsync(nodeId);
                if (result == null)
                {
                    Console.Error.WriteLine($"Error: Node not found: {nodeId}");
                    Environment.ExitCode = 1;
                    return;
                }
                Console.WriteLine(formatter.Format(result));
            });
        }, nodeIdArgument, DatabaseOption);

        return command;
    }

    private static async Task ExecuteWithServiceAsync(string? databasePath, Func<IGraphNavigationService, IOutputFormatter, Task> action)
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
            Console.Error.WriteLine($"Error: {ex.Message}");
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

        Console.Error.WriteLine("Error: No database found. Specify a database path with --database or run 'sharpitect analyze' first.");
        return null;
    }
}
