# Sharpitect Plugin for Claude Code

This plugin provides intelligent C# codebase navigation through the Sharpitect MCP server and specialized skills. When installed, Claude automatically prefers semantic code analysis over text-based searching when working with C# projects.

## What's Included

- **Sharpitect MCP Server**: Semantic understanding of C# codebases
- **Use-Sharpitect Skill**: Automatically guides Claude to use Sharpitect tools first
- **16+ MCP Tools**: Search, navigate, and understand C# code relationships

## Features

- Find classes, methods, properties by name (not regex)
- Understand inheritance hierarchies and implementations
- Trace method calls (callers and callees)
- Explore project dependencies
- Navigate code structure semantically
- Works across all C# repositories once installed

## Prerequisites

1. **Claude Code CLI** installed
2. **Sharpitect tool** installed and available in PATH:
   ```bash
   dotnet tool install -g sharpitect
   ```

3. **Analyzed codebase**: Before using the plugin in a C# project, analyze it first:
   ```bash
   sharpitect analyze YourSolution.sln
   ```

## Installation

### Option 1: Install from Local Directory (Development/Testing)

1. Navigate to this plugin directory:
   ```bash
   cd path/to/sharpitect-plugin
   ```

2. In Claude Code, add the plugin directory:
   ```
   /plugin add .
   ```

3. Enable the plugin:
   ```
   /plugin enable sharpitect
   ```

### Option 2: Install from GitHub Marketplace (Distribution)

1. Create a GitHub repository for this plugin
2. Push the `sharpitect-plugin` directory contents to the repo
3. Create a `plugins.json` marketplace file:
   ```json
   {
     "plugins": [
       {
         "name": "sharpitect",
         "description": "Sharpitect MCP server with skills for C# navigation",
         "url": "https://github.com/yourusername/sharpitect-plugin"
       }
     ]
   }
   ```

4. Users can then install from the marketplace:
   ```
   /plugin install sharpitect
   ```

### Option 3: Distribute as Package

You can also package this plugin and distribute it via:
- Private plugin marketplace (for teams)
- Public GitHub release
- NPM package
- NuGet package (bundled with the Sharpitect tool)

## Usage

Once installed, the plugin works automatically in C# projects!

### Example Workflow

```
User: "Find all implementations of IGraphRepository"

Claude (automatically uses Sharpitect):
1. search_declarations(query="IGraphRepository", kind="interface")
2. get_inheritance(id="...", direction="descendants")

Result: SqliteGraphRepository found at ...
```

### Manual Skill Activation (Optional)

The `use-sharpitect` skill activates automatically when working with C#, but you can manually invoke it:

```
/use-sharpitect Find all classes in the Persistence namespace
```

## Verifying Installation

1. Check the plugin is enabled:
   ```
   /plugin list
   ```
   You should see `sharpitect` in the enabled plugins.

2. Check the MCP server is running:
   ```
   /mcp
   ```
   You should see `Sharpitect` listed with 16+ tools.

3. Check the skill is available:
   ```
   /skills
   ```
   You should see `use-sharpitect` in the list.

## How It Works

The plugin combines three components:

1. **MCP Server (.mcp.json)**: Runs `sharpitect serve` to provide code intelligence
2. **Skill (SKILL.md)**: Instructs Claude to prefer Sharpitect tools over Grep/Glob
3. **Plugin Manifest (plugin.json)**: Packages everything for easy distribution

When you ask Claude about C# code, the skill automatically activates and guides Claude to use semantic tools like `search_declarations` and `get_inheritance` instead of text-based tools like `Grep`.

## Available Tools

The Sharpitect MCP server provides:

- `search_declarations` - Find classes, methods, properties by name
- `get_inheritance` - Get type hierarchies (base classes, implementations)
- `get_callers` - Find what calls a method
- `get_callees` - Find what a method calls
- `get_usages` - Find all references to a declaration
- `get_relationships` - Explore relationships between declarations
- `get_dependencies` - Get project dependencies
- `get_dependents` - Find projects that depend on a project
- `get_tree` - Get containment hierarchy
- `get_node` - Get detailed node information
- `get_signature` - Get type/method signatures
- `get_children` - Get child declarations
- `get_ancestors` - Get containment path to root
- `get_file_declarations` - Get all declarations in a file
- `list_by_kind` - List all declarations of a specific kind

## Troubleshooting

### Plugin not activating
- Ensure you've analyzed the codebase: `sharpitect analyze YourSolution.sln`
- Check `.sharpitect/graph.db` exists in the project root
- Verify Sharpitect is in PATH: `sharpitect --version`

### MCP server not starting
- Check the MCP server logs in Claude Code
- Ensure the database path is correct (`.sharpitect/graph.db`)
- Try running manually: `sharpitect serve .sharpitect/graph.db`

### Skill not activating automatically
- The skill activates based on context (C# files, .NET projects)
- You can manually invoke it with `/use-sharpitect`
- Check skill is enabled: `/skills`

## Distribution Strategies

### For Teams (Private)
1. Create a private GitHub repo with this plugin
2. Create a `plugins.json` marketplace file
3. Team members add the marketplace and install

### For Public Use
1. Publish to GitHub
2. Create a public marketplace or submit to Claude Code marketplace
3. Users install via `/plugin install`

### Bundle with Sharpitect Tool
1. Include this plugin in the Sharpitect NuGet package
2. Post-install script copies plugin to `~/.claude/plugins/`
3. Users get the plugin automatically when installing Sharpitect

## Updating the Plugin

To update the plugin after making changes:

1. Increment version in `plugin.json`
2. If using a marketplace, commit and push changes
3. Users can update with:
   ```
   /plugin update sharpitect
   ```

Or reinstall locally:
```
/plugin remove sharpitect
/plugin add path/to/sharpitect-plugin
```

## Contributing

To improve the plugin:

1. Enhance the skill instructions in `skills/use-sharpitect/SKILL.md`
2. Add more tools to `allowed-tools` as new Sharpitect features are added
3. Update examples and workflows based on user feedback

## Support

- Sharpitect Documentation: [Link to your docs]
- GitHub Issues: [Link to your repo]
- MCP Protocol: https://modelcontextprotocol.io

## License

[Your License Here]
