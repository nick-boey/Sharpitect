using System.CommandLine;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for setting up Sharpitect integration.
/// </summary>
public static class SetupCommands
{
    private const string ServerName = "Sharpitect";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly Option<string?> DatabaseOption = new(
        aliases: ["--database", "-d"],
        description: "Path to the SQLite database file. Defaults to .sharpitect/graph.db in current directory.");

    public static Command CreateInstallCommand()
    {
        var command = new Command("install", "Configure Sharpitect as an MCP server for Claude Code.")
        {
            DatabaseOption
        };

        command.SetHandler(async (database) => { await InstallAsync(database); }, DatabaseOption);

        return command;
    }

    public static Command CreateUninstallCommand()
    {
        var command = new Command("uninstall", "Remove Sharpitect MCP server configuration from Claude Code.");

        command.SetHandler(async () => { await UninstallAsync(); });

        return command;
    }

    public static Command CreatePrimeCommand()
    {
        var command = new Command("prime", "Output LLM instructions for using Sharpitect tools.");

        command.SetHandler(PrimeAsync);

        return command;
    }

    private static Task PrimeAsync()
    {
        Console.WriteLine(GetPrimeInstructions());
        return Task.CompletedTask;
    }

    private static string GetPrimeInstructions()
    {
        return """
            # Sharpitect Code Navigation Guide

            This project uses Sharpitect for C# code navigation. Use the MCP tools below instead of directly reading C# files with the Read tool.

            ## CLI Commands

            - `sharpitect analyze` - Analyze a .NET solution and build the declaration graph
            - `sharpitect init` - Initialize a .sln.yml configuration file
            - `sharpitect watch` - Watch for file changes and update the graph
            - `sharpitect serve` - Start the MCP server for IDE integration
            - `sharpitect install` - Configure Sharpitect as an MCP server for Claude Code
            - `sharpitect uninstall` - Remove Sharpitect configuration
            - `sharpitect search` - Search for declarations by name
            - `sharpitect node` - Get detailed information about a node
            - `sharpitect children` - Get immediate children of a declaration
            - `sharpitect ancestors` - Get containment hierarchy path
            - `sharpitect relationships` - Get relationships for a node
            - `sharpitect callers` - Find methods that call a specific method
            - `sharpitect callees` - Find methods called by a specific method
            - `sharpitect inheritance` - Get inheritance hierarchy
            - `sharpitect list` - List declarations by kind
            - `sharpitect dependencies` - Get project dependencies
            - `sharpitect dependents` - Get projects that depend on a project
            - `sharpitect file` - Get all declarations in a source file
            - `sharpitect usages` - Find all usages of a type/method/property
            - `sharpitect signature` - Get full signature and type info
            - `sharpitect code` - Get source code for a declaration
            - `sharpitect tree` - Display containment hierarchy as tree
            - `sharpitect health` - Check database health

            ## MCP Tools

            ### Search & Discovery
            - **SearchDeclarations** - Search for classes, methods, properties by name
            - **ListByKind** - List all declarations of a specific kind (class, method, etc.)
            - **GetFileDeclarations** - Get all declarations in a source file

            ### Navigation
            - **GetNode** - Get detailed info about a declaration by fully qualified name
            - **GetChildren** - Get contained declarations (methods in a class, etc.)
            - **GetAncestors** - Get containment hierarchy path from root
            - **GetTree** - Display containment tree structure

            ### Relationships
            - **GetRelationships** - Get all relationships for a node
            - **GetCallers** - Find what calls a method
            - **GetCallees** - Find what a method calls
            - **GetInheritance** - Get inheritance hierarchy
            - **GetUsages** - Find all usages of a type/method

            ### Dependencies
            - **GetDependencies** - Get project dependencies
            - **GetDependents** - Get projects that depend on a project

            ### Code Reading
            - **GetCode** - Read source code for a specific declaration
            - **GetSignature** - Get signature and type information
            - **ReadFile** - Read entire C# file contents

            ### Documentation
            - **SearchDocuments** - Semantic search in markdown docs
            - **ListDocuments** - List all indexed markdown files
            - **GetDocument** - Get details about a markdown file

            ## Workflow Tips

            1. Start with `SearchDeclarations` to find relevant code
            2. Use `GetNode` to get details about specific declarations
            3. Use `GetCallers`/`GetCallees` to understand call flows
            4. Use `GetCode` to read implementation details
            5. Use `ReadFile` when you need the full file context

            ## Notes

            - Use fully qualified names (e.g., `Namespace.ClassName.MethodName`)
            - The graph is built from the solution's .sln file
            - Source locations include file path and line numbers
            """;
    }

    private static async Task InstallAsync(string? databasePath)
    {
        var projectDir = Directory.GetCurrentDirectory();
        var dbPath = databasePath ?? ".sharpitect/graph.db";

        try
        {
            // 1. Update .mcp.json with MCP server configuration
            await UpdateMcpJsonAsync(projectDir, dbPath);

            // 2. Update .claude/settings.local.json with:
            //    - Server enabled
            //    - SessionStart hook for sharpitect prime
            //    - Permission denials for *.cs files
            await UpdateSettingsLocalJsonAsync(projectDir);

            Console.WriteLine("Sharpitect MCP server installed successfully.");
            Console.WriteLine();
            Console.WriteLine("Files updated:");
            Console.WriteLine("  - .mcp.json (MCP server configuration)");
            Console.WriteLine("  - .claude/settings.local.json (server enabled, hooks, permissions)");
            Console.WriteLine();
            Console.WriteLine("Configuration added:");
            Console.WriteLine("  - SessionStart hook: runs 'sharpitect prime' at session start");
            Console.WriteLine("  - Permission denials: Read/Write/Edit *.cs blocked (use Sharpitect MCP tools)");
            Console.WriteLine();
            Console.WriteLine("Restart Claude Code to load the MCP server.");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task UpdateMcpJsonAsync(string projectDir, string dbPath)
    {
        var mcpJsonPath = Path.Combine(projectDir, ".mcp.json");

        // Read existing or create new
        JsonObject mcpJson;
        if (File.Exists(mcpJsonPath))
        {
            var existingContent = await File.ReadAllTextAsync(mcpJsonPath);
            mcpJson = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            mcpJson = new JsonObject();
        }

        // Get or create mcpServers object
        if (!mcpJson.ContainsKey("mcpServers"))
        {
            mcpJson["mcpServers"] = new JsonObject();
        }

        var mcpServers = mcpJson["mcpServers"]!.AsObject();

        // Build command args based on platform
        JsonArray args;
        string command;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            command = "cmd";
            args = new JsonArray("/c", "sharpitect", "serve", dbPath);
        }
        else
        {
            command = "sharpitect";
            args = new JsonArray("serve", dbPath);
        }

        // Add sharpitect server configuration
        var sharpitectConfig = new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = command,
            ["args"] = args,
            ["env"] = new JsonObject()
        };
        mcpServers[ServerName] = sharpitectConfig;

        // Write .mcp.json
        var json = mcpJson.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(mcpJsonPath, json + Environment.NewLine);
    }

    private static async Task UpdateSettingsLocalJsonAsync(string projectDir)
    {
        var claudeDir = Path.Combine(projectDir, ".claude");
        var settingsPath = Path.Combine(claudeDir, "settings.local.json");

        // Ensure .claude directory exists
        if (!Directory.Exists(claudeDir))
        {
            Directory.CreateDirectory(claudeDir);
        }

        // Read existing or create new
        JsonObject settings;
        if (File.Exists(settingsPath))
        {
            var existingContent = await File.ReadAllTextAsync(settingsPath);
            settings = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            settings = new JsonObject();
        }

        // Enable project MCP servers
        settings["enableAllProjectMcpServers"] = true;

        // Handle enabledMcpjsonServers array
        EnsureServerEnabled(settings);

        // Add SessionStart hook for sharpitect prime
        AddSessionStartHook(settings);

        // Add permission denials for *.cs files
        AddPermissionDenials(settings);

        // Write settings.local.json
        var json = settings.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json + Environment.NewLine);
    }

    private static void EnsureServerEnabled(JsonObject settings)
    {
        JsonArray enabledServers;
        if (settings.ContainsKey("enabledMcpjsonServers") &&
            settings["enabledMcpjsonServers"] is JsonArray existingArray)
        {
            enabledServers = existingArray;
        }
        else
        {
            enabledServers = new JsonArray();
            settings["enabledMcpjsonServers"] = enabledServers;
        }

        var hasServer = enabledServers.Any(s => s?.GetValue<string>() == ServerName);
        if (!hasServer)
        {
            enabledServers.Add(ServerName);
        }
    }

    private static void AddSessionStartHook(JsonObject settings)
    {
        // Structure: hooks.SessionStart[].hooks[].{type, command}
        if (!settings.ContainsKey("hooks"))
        {
            settings["hooks"] = new JsonObject();
        }

        var hooks = settings["hooks"]!.AsObject();

        if (!hooks.ContainsKey("SessionStart"))
        {
            hooks["SessionStart"] = new JsonArray();
        }

        var sessionStart = hooks["SessionStart"]!.AsArray();

        // Check if sharpitect prime hook already exists
        const string hookCommand = "sharpitect prime";
        var hasHook = sessionStart.Any(h =>
        {
            if (h is not JsonObject hookObj) return false;
            if (!hookObj.ContainsKey("hooks")) return false;
            var innerHooks = hookObj["hooks"]!.AsArray();
            return innerHooks.Any(ih =>
            {
                if (ih is not JsonObject innerHookObj) return false;
                return innerHookObj.TryGetPropertyValue("command", out var cmd) &&
                       cmd?.GetValue<string>() == hookCommand;
            });
        });

        if (!hasHook)
        {
            var newHook = new JsonObject
            {
                ["hooks"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "command",
                        ["command"] = hookCommand
                    }
                }
            };
            sessionStart.Add(newHook);
        }
    }

    private static void AddPermissionDenials(JsonObject settings)
    {
        // Structure: permissions.deny[]
        if (!settings.ContainsKey("permissions"))
        {
            settings["permissions"] = new JsonObject();
        }

        var permissions = settings["permissions"]!.AsObject();

        if (!permissions.ContainsKey("deny"))
        {
            permissions["deny"] = new JsonArray();
        }

        var deny = permissions["deny"]!.AsArray();

        // Add denials for Read, Write, Edit on *.cs files
        var denials = new[] { "Read(*.cs)", "Write(*.cs)", "Edit(*.cs)" };

        foreach (var denial in denials)
        {
            var hasDenial = deny.Any(d => d?.GetValue<string>() == denial);

            if (!hasDenial)
            {
                deny.Add(denial);
            }
        }
    }

    private static async Task UninstallAsync()
    {
        var projectDir = Directory.GetCurrentDirectory();
        var filesModified = new List<string>();

        try
        {
            // 1. Remove from .mcp.json
            if (await RemoveFromMcpJsonAsync(projectDir))
            {
                filesModified.Add(".mcp.json");
            }

            // 2. Remove from .claude/settings.local.json
            if (await RemoveFromSettingsLocalJsonAsync(projectDir))
            {
                filesModified.Add(".claude/settings.local.json");
            }

            if (filesModified.Count > 0)
            {
                Console.WriteLine("Sharpitect MCP server uninstalled successfully.");
                Console.WriteLine();
                Console.WriteLine("Files updated:");
                foreach (var file in filesModified)
                {
                    Console.WriteLine($"  - {file}");
                }

                Console.WriteLine();
                Console.WriteLine("Restart Claude Code to apply changes.");
            }
            else
            {
                Console.WriteLine("Sharpitect was not installed in this project.");
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task<bool> RemoveFromMcpJsonAsync(string projectDir)
    {
        var mcpJsonPath = Path.Combine(projectDir, ".mcp.json");

        if (!File.Exists(mcpJsonPath))
        {
            return false;
        }

        var content = await File.ReadAllTextAsync(mcpJsonPath);
        var mcpJson = JsonNode.Parse(content)?.AsObject();

        if (mcpJson == null)
        {
            return false;
        }

        if (!mcpJson.ContainsKey("mcpServers"))
        {
            return false;
        }

        var mcpServers = mcpJson["mcpServers"]!.AsObject();

        if (!mcpServers.ContainsKey(ServerName))
        {
            return false;
        }

        mcpServers.Remove(ServerName);

        // Write back
        var json = mcpJson.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(mcpJsonPath, json + Environment.NewLine);

        return true;
    }

    private static async Task<bool> RemoveFromSettingsLocalJsonAsync(string projectDir)
    {
        var settingsPath = Path.Combine(projectDir, ".claude", "settings.local.json");

        if (!File.Exists(settingsPath))
        {
            return false;
        }

        var content = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonNode.Parse(content)?.AsObject();

        if (settings == null)
        {
            return false;
        }

        var modified = false;

        // Remove from enabledMcpjsonServers array
        modified |= RemoveServerFromEnabled(settings);

        // Remove SessionStart hook for sharpitect prime
        modified |= RemoveSessionStartHook(settings);

        // Remove permission denials for *.cs files
        modified |= RemovePermissionDenials(settings);

        if (modified)
        {
            var json = settings.ToJsonString(JsonOptions);
            await File.WriteAllTextAsync(settingsPath, json + Environment.NewLine);
        }

        return modified;
    }

    private static bool RemoveServerFromEnabled(JsonObject settings)
    {
        if (!settings.ContainsKey("enabledMcpjsonServers") ||
            settings["enabledMcpjsonServers"] is not JsonArray enabledServers)
        {
            return false;
        }

        var modified = false;
        for (var i = enabledServers.Count - 1; i >= 0; i--)
        {
            if (enabledServers[i]?.GetValue<string>() == ServerName)
            {
                enabledServers.RemoveAt(i);
                modified = true;
            }
        }

        return modified;
    }

    private static bool RemoveSessionStartHook(JsonObject settings)
    {
        if (!settings.ContainsKey("hooks"))
        {
            return false;
        }

        var hooks = settings["hooks"]!.AsObject();

        if (!hooks.ContainsKey("SessionStart"))
        {
            return false;
        }

        var sessionStart = hooks["SessionStart"]!.AsArray();

        const string hookCommand = "sharpitect prime";
        var modified = false;

        for (var i = sessionStart.Count - 1; i >= 0; i--)
        {
            if (sessionStart[i] is not JsonObject hookObj) continue;
            if (!hookObj.ContainsKey("hooks")) continue;

            var innerHooks = hookObj["hooks"]!.AsArray();
            for (var j = innerHooks.Count - 1; j >= 0; j--)
            {
                if (innerHooks[j] is JsonObject innerHook &&
                    innerHook.TryGetPropertyValue("command", out var cmd) &&
                    cmd?.GetValue<string>() == hookCommand)
                {
                    innerHooks.RemoveAt(j);
                    modified = true;
                }
            }

            // Remove the outer hook object if inner hooks array is now empty
            if (innerHooks.Count == 0)
            {
                sessionStart.RemoveAt(i);
            }
        }

        // Clean up empty SessionStart array
        if (sessionStart.Count == 0)
        {
            hooks.Remove("SessionStart");
        }

        // Clean up empty hooks object
        if (hooks.Count == 0)
        {
            settings.Remove("hooks");
        }

        return modified;
    }

    private static bool RemovePermissionDenials(JsonObject settings)
    {
        if (!settings.ContainsKey("permissions"))
        {
            return false;
        }

        var permissions = settings["permissions"]!.AsObject();

        if (!permissions.ContainsKey("deny"))
        {
            return false;
        }

        var deny = permissions["deny"]!.AsArray();

        var denials = new[] { "Read(*.cs)", "Write(*.cs)", "Edit(*.cs)" };
        var modified = false;

        for (var i = deny.Count - 1; i >= 0; i--)
        {
            var value = deny[i]?.GetValue<string>();
            if (value != null && denials.Contains(value))
            {
                deny.RemoveAt(i);
                modified = true;
            }
        }

        // Clean up empty deny array
        if (deny.Count == 0)
        {
            permissions.Remove("deny");
        }

        // Clean up empty permissions object
        if (permissions.Count == 0)
        {
            settings.Remove("permissions");
        }

        return modified;
    }
}