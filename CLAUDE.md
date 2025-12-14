# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build Sharpitect.sln

# Run all tests
dotnet test Sharpitect.sln

# Run a single test
dotnet test test/Sharpitect.Analysis.Test --filter "FullyQualifiedName~TestName"
```

## Project Architecture

Sharpitect generates C4 architecture diagrams from annotated C# codebases.

### Projects

- **Sharpitect.Tool** (.NET 8) - Command-line tool entry point
- **Sharpitect.Analysis** (.NET 8) - Analysis engine for extracting C4 model information from code
- **Sharpitect.Attributes** (.NET Framework 4.8) - Attributes for annotating code (e.g., `[UserAction]`, `[ModelComponent]`). Targets .NET Framework for broad compatibility with analyzed codebases
- **Sharpitect.Analysis.Test** (.NET 8, NUnit) - Unit tests for the analysis engine

### C4 Model Mapping Conventions

The tool maps C# constructs to C4 model elements:

- **Systems** = Solutions (.sln files)
- **Containers** = Executable projects
- **Components** = Projects, namespaces, or groups (cannot be nested, but can be grouped)
- **Code** = Classes, methods, and properties extracted via Roslyn analysis

### Model Structure

The `Sharpitect.Analysis.Model` namespace contains the C4 hierarchy:

- `ArchitectureModel` - Root container for the entire model
- `SoftwareSystem` - Top-level system containing containers
- `Container` - Deployable unit containing components
- `Component` - Logical grouping containing code elements
- `Model/Code/` - Code-level elements:
  - `ICode` - Base interface for code elements
  - `ClassCode` - Represents a class with methods and properties
  - `MethodCode` - Represents a method
  - `PropertyCode` - Represents a property

### Analysis Pipeline

1. `CodeAnalyzer` - Uses Roslyn to parse C# source and extract type information (classes, interfaces, methods, properties, attributes)
2. `ModelBuilder` - Constructs the C4 model from analysis results and YAML configuration
