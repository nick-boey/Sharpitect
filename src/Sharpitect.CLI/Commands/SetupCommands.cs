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

    private static async Task InstallAsync(string? databasePath)
    {
        var projectDir = Directory.GetCurrentDirectory();
        var dbPath = databasePath ?? ".sharpitect/graph.db";

        try
        {
            // 1. Update .mcp.json with MCP server configuration
            await UpdateMcpJsonAsync(projectDir, dbPath);

            // 2. Update .claude/settings.local.json to enable the server
            await UpdateSettingsLocalJsonAsync(projectDir);

            Console.WriteLine("Sharpitect MCP server installed successfully.");
            Console.WriteLine();
            Console.WriteLine("Files updated:");
            Console.WriteLine($"  - .mcp.json (MCP server configuration)");
            Console.WriteLine($"  - .claude/settings.local.json (server enabled)");
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

        // Get or create enabledMcpjsonServers array
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

        // Add Sharpitect if not already present
        var hasServer = enabledServers.Any(s => s?.GetValue<string>() == ServerName);
        if (!hasServer)
        {
            enabledServers.Add(ServerName);
        }

        // Write settings.local.json
        var json = settings.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json + Environment.NewLine);
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
        if (settings.ContainsKey("enabledMcpjsonServers") &&
            settings["enabledMcpjsonServers"] is JsonArray enabledServers)
        {
            for (int i = enabledServers.Count - 1; i >= 0; i--)
            {
                if (enabledServers[i]?.GetValue<string>() == ServerName)
                {
                    enabledServers.RemoveAt(i);
                    modified = true;
                }
            }
        }

        if (modified)
        {
            var json = settings.ToJsonString(JsonOptions);
            await File.WriteAllTextAsync(settingsPath, json + Environment.NewLine);
        }

        return modified;
    }
}