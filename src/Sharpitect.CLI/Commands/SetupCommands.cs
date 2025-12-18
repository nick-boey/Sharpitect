using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sharpitect.CLI.Commands;

/// <summary>
/// Commands for setting up Sharpitect integration.
/// </summary>
public static class SetupCommands
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static Command CreateInstallCommand()
    {
        var globalOption = new Option<bool>(
            aliases: ["--global", "-g"],
            description: "Install to user-level settings (~/.claude/settings.json) instead of project-level.");

        var databaseOption = new Option<string?>(
            aliases: ["--database", "-d"],
            description: "Path to the SQLite database file. Defaults to .sharpitect/graph.db in current directory.");

        var command = new Command("install", "Configure Sharpitect as an MCP server for Claude Code.")
        {
            globalOption,
            databaseOption
        };

        command.SetHandler(async (global, database) =>
        {
            await InstallAsync(global, database);
        }, globalOption, databaseOption);

        return command;
    }

    private static async Task InstallAsync(bool global, string? databasePath)
    {
        // Determine settings file path
        string settingsPath;
        if (global)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            settingsPath = Path.Combine(userHome, ".claude", "settings.json");
        }
        else
        {
            settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ".claude", "settings.json");
        }

        // Determine database path
        var dbPath = databasePath ?? ".sharpitect/graph.db";

        // For global install with relative path, make it absolute from current directory
        if (global && !Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
        }

        try
        {
            // Read existing settings or create new
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

            // Get or create mcpServers object
            if (!settings.ContainsKey("mcpServers"))
            {
                settings["mcpServers"] = new JsonObject();
            }
            var mcpServers = settings["mcpServers"]!.AsObject();

            // Add sharpitect server configuration
            var sharpitectConfig = new JsonObject
            {
                ["command"] = "sharpitect",
                ["args"] = new JsonArray("serve", dbPath)
            };
            mcpServers["sharpitect"] = sharpitectConfig;

            // Ensure directory exists
            var settingsDir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            // Write settings file
            var json = settings.ToJsonString(JsonOptions);
            await File.WriteAllTextAsync(settingsPath, json);

            Console.WriteLine($"Sharpitect MCP server configured in: {settingsPath}");
            Console.WriteLine();
            Console.WriteLine("Configuration added:");
            Console.WriteLine($"  Command: sharpitect serve {dbPath}");
            Console.WriteLine();
            Console.WriteLine("Restart Claude Code to load the MCP server.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
