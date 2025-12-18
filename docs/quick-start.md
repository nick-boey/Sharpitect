# Sharpitect Quick Start Guide

Sharpitect analyzes .NET codebases and builds a declaration graph that enables powerful code navigation and exploration. This guide covers installation and usage of the CLI tool and MCP server.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [Install as a Global Tool](#install-as-a-global-tool)
  - [Build from Source](#build-from-source)
- [CLI Usage](#cli-usage)
  - [Analyzing a Solution](#analyzing-a-solution)
  - [Searching the Graph](#searching-the-graph)
  - [Navigating Declarations](#navigating-declarations)
  - [Exploring Relationships](#exploring-relationships)
  - [Project Dependencies](#project-dependencies)
- [MCP Server](#mcp-server)
  - [Starting the Server](#starting-the-server)
  - [Claude Code Integration](#claude-code-integration)
  - [Available Tools](#available-tools)
- [Development Setup](#development-setup)

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- A .NET solution (`.sln`) to analyze

---

## Installation

### Install as a Global Tool

Install Sharpitect as a global .NET tool:

```bash
dotnet tool install -g Sharpitect.Tool
```

Verify the installation:

```bash
sharpitect --help
```

To update to the latest version:

```bash
dotnet tool update -g Sharpitect.Tool
```

### Build from Source

Clone the repository and build:

```bash
git clone https://github.com/nick-boey/sharpitect.git
cd sharpitect
dotnet build Sharpitect.sln
```

Run the CLI directly:

```bash
dotnet run --project src/Sharpitect.CLI -- --help
```

Or install as a local tool for development:

```bash
dotnet pack src/Sharpitect.CLI -o ./nupkg
dotnet tool install --global --add-source ./nupkg Sharpitect
```

---

## CLI Usage

### Analyzing a Solution

Before using navigation commands, analyze your solution to build the declaration graph:

```bash
# Analyze the solution in the current directory
sharpitect analyze

# Analyze a specific solution file
sharpitect analyze path/to/MySolution.sln

# Specify a custom output database path
sharpitect analyze --output ./my-graph.db
```

The analysis creates a SQLite database at `.sharpitect/graph.db` containing all declarations and relationships found in your codebase.

**Output example:**

```
Analyzing solution: C:\src\MySolution\MySolution.sln
Output database: C:\src\MySolution\.sharpitect\graph.db

Analysis complete:
  Nodes: 1,234
  Edges: 5,678

Graph saved to: C:\src\MySolution\.sharpitect\graph.db
```

### Searching the Graph

Search for declarations by name:

```bash
# Search for declarations containing "Service"
sharpitect search Service

# Filter by declaration kind
sharpitect search Service --kind class
sharpitect search Get --kind method

# Use different match modes
sharpitect search Graph --match starts_with
sharpitect search Service --match exact

# Case-sensitive search
sharpitect search graphService --case-sensitive

# Limit results
sharpitect search Service --limit 10
```

**Declaration kinds:** `solution`, `project`, `namespace`, `class`, `interface`, `struct`, `record`, `enum`, `delegate`, `method`, `constructor`, `property`, `field`, `event`

**Match modes:** `contains` (default), `starts_with`, `ends_with`, `exact`

### Navigating Declarations

Get detailed information about specific declarations:

```bash
# Get node details by fully qualified ID
sharpitect node MyNamespace.MyClass

# Get children of a node (contents of a class, namespace, etc.)
sharpitect children MyNamespace.MyClass
sharpitect children MyNamespace.MyClass --kind method

# Get the ancestor chain (containment hierarchy)
sharpitect ancestors MyNamespace.MyClass.MyMethod

# Get all declarations in a source file
sharpitect file src/MyClass.cs

# Get full signature of a method or property
sharpitect signature MyNamespace.MyClass.MyMethod

# View source code for a declaration
sharpitect code MyNamespace.MyClass.MyMethod

# List all declarations of a specific kind
sharpitect list class
sharpitect list interface --scope MyNamespace
```

The `code` command displays declaration details along with the source code:

```
[Method] MyMethod
  Full name: MyNamespace.MyClass.MyMethod
  Path: src/MyClass.cs:42-55

Source code:
```
public void MyMethod()
{
    // method implementation
}
```
```

### Exploring Relationships

Understand how code elements relate to each other:

```bash
# Get all relationships for a node
sharpitect relationships MyNamespace.MyClass

# Filter by direction
sharpitect relationships MyClass --direction outgoing
sharpitect relationships MyClass --direction incoming

# Filter by relationship kind
sharpitect relationships MyClass --kind implements

# Find callers of a method (what calls this method)
sharpitect callers MyNamespace.MyClass.MyMethod
sharpitect callers MyNamespace.MyClass.MyMethod --depth 2

# Find callees of a method (what this method calls)
sharpitect callees MyNamespace.MyClass.MyMethod

# Get inheritance hierarchy
sharpitect inheritance MyNamespace.MyClass
sharpitect inheritance IMyInterface --direction descendants

# Find all usages of a type, method, or property
sharpitect usages MyNamespace.MyClass
sharpitect usages MyClass.MyMethod --kind call
```

**Relationship kinds:** `calls`, `inherits`, `implements`, `references`, `uses`, `constructs`, `contains`, `overrides`

**Usage kinds:** `call`, `type_reference`, `inheritance`, `instantiation`

### Project Dependencies

Explore project-level dependencies:

```bash
# Get dependencies of a project
sharpitect dependencies MyProject

# Include transitive dependencies
sharpitect dependencies MyProject --transitive

# Get projects that depend on a project
sharpitect dependents MyProject
sharpitect dependents MyProject --transitive
```

### Common Options

Most navigation commands support:

| Option | Description |
|--------|-------------|
| `-d, --database <path>` | Path to the SQLite database (defaults to `.sharpitect/graph.db`) |
| `-l, --limit <n>` | Maximum number of results (default: 50) |

---

## MCP Server

The MCP (Model Context Protocol) server enables AI assistants like Claude to navigate your codebase through structured tools.

### Starting the Server

Start the MCP server with a pre-analyzed database:

```bash
sharpitect serve .sharpitect/graph.db
```

The server communicates over stdio using the MCP protocol.

### Claude Code Integration

Add Sharpitect to your Claude Code MCP configuration. Create or edit `.claude/settings.json` in your project:

```json
{
  "mcpServers": {
    "sharpitect": {
      "command": "sharpitect",
      "args": ["serve", ".sharpitect/graph.db"]
    }
  }
}
```

Or configure globally in your user settings at `~/.claude/settings.json`:

```json
{
  "mcpServers": {
    "sharpitect": {
      "command": "sharpitect",
      "args": ["serve", "/path/to/your/project/.sharpitect/graph.db"]
    }
  }
}
```

After configuration, restart Claude Code to load the MCP server.

### Available Tools

The MCP server provides these tools for AI-assisted code navigation:

| Tool | Description |
|------|-------------|
| `SearchDeclarations` | Search for declarations by name with filters |
| `GetNode` | Get detailed information about a declaration |
| `GetChildren` | Get contents of a class, namespace, or project |
| `GetAncestors` | Get containment hierarchy path |
| `GetRelationships` | Get relationships (calls, inherits, etc.) |
| `GetCallers` | Find methods that call a specific method |
| `GetCallees` | Find methods called by a specific method |
| `GetInheritance` | Get inheritance hierarchy |
| `ListByKind` | List all declarations of a kind |
| `GetDependencies` | Get project dependencies |
| `GetDependents` | Get projects that depend on a project |
| `GetFileDeclarations` | Get declarations in a source file |
| `GetUsages` | Find all usages of a type or method |
| `GetSignature` | Get full signature information |
| `GetCode` | Get declaration details and source code |

All tools support `json` (default) or `text` output formats via the `format` parameter.

For detailed tool documentation including parameters and examples, see [MCP Server Reference](mcp-server.md).

---

## Development Setup

### Building the Solution

```bash
# Clone the repository
git clone https://github.com/your-org/sharpitect.git
cd sharpitect

# Build all projects
dotnet build Sharpitect.sln

# Run tests
dotnet test Sharpitect.sln
```

### Project Structure

| Project | Description |
|---------|-------------|
| `Sharpitect.CLI` | Command-line tool entry point |
| `Sharpitect.MCP` | MCP server implementation |
| `Sharpitect.Analysis` | Code analysis engine using Roslyn |
| `Sharpitect.Attributes` | Attributes for annotating code |
| `Sharpitect.Analysis.Test` | Unit tests |

### Running During Development

Run the CLI without installing:

```bash
# Run help
dotnet run --project src/Sharpitect.CLI -- --help

# Analyze a solution
dotnet run --project src/Sharpitect.CLI -- analyze ../other-project

# Start MCP server
dotnet run --project src/Sharpitect.CLI -- serve .sharpitect/graph.db
```

### Creating a Local Tool Package

Build and install as a local tool:

```bash
# Create the NuGet package
dotnet pack src/Sharpitect.CLI -c Release -o ./nupkg

# Install from local package
dotnet tool install --global --add-source ./nupkg Sharpitect.Tool

# Or update if already installed
dotnet tool update --global --add-source ./nupkg Sharpitect.Tool
```

### Running Tests

```bash
# Run all tests
dotnet test Sharpitect.sln

# Run specific tests
dotnet test test/Sharpitect.Analysis.Test --filter "FullyQualifiedName~GraphSearchServiceTests"

# Run with verbose output
dotnet test Sharpitect.sln --logger "console;verbosity=detailed"
```

---

## Next Steps

- See [MCP Server Reference](mcp-server.md) for detailed tool documentation
- See [C4 Quick Start](c4/quick-start-c4.md) for generating C4 architecture diagrams
- See [Full Example](c4/full-example-c4.md) for a complete walkthrough
