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
- **Sharpitect.Attributes** (.NET Framework 4.8) - Attributes for annotating code (e.g., `[Component]`). Targets .NET Framework for broad compatibility with analyzed codebases
- **Sharpitect.Analysis.Test** (.NET 8, NUnit) - Unit tests for the analysis engine

### C4 Model Mapping Conventions

The tool maps C# constructs to C4 model elements:

- **Systems** = Solutions (.sln files)
- **Containers** = Executable projects
- **Components** = Projects, namespaces, or groups (cannot be nested, but can be grouped)
- **Code** = Classes, methods, and properties extracted via Roslyn analysis

### Key Namespaces

#### `Sharpitect.Analysis.Graph`

The declaration graph structure for representing code elements:

- `DeclarationNode` - Represents a code declaration (class, method, property, etc.)
- `DeclarationGraph` - In-memory graph of all declarations
- `RelationshipEdge` - Represents relationships between nodes (Contains, Calls, Inherits, etc.)
- `DeclarationKind` - Enum of declaration types (Class, Interface, Method, Property, etc.)
- `RelationshipKind` - Enum of relationship types (Contains, Calls, Inherits, Implements, etc.)
- `C4Level` - Enum for C4 model levels (None, System, Container, Component, Code)

#### `Sharpitect.Analysis.Persistence`

Persistence layer for the declaration graph:

- `IGraphRepository` - Interface for graph persistence operations
- `SqliteGraphRepository` - SQLite implementation of the repository

#### `Sharpitect.Analysis.Search`

Search service for querying the declaration graph:

- `IGraphSearchService` - Interface for searching nodes
- `GraphSearchService` - Default search implementation
- `SearchQuery` - Query parameters (SearchText, MatchMode, KindFilter, CaseSensitive)
- `SearchMatchMode` - Match modes (Contains, StartsWith, EndsWith, Exact)
- `ISearchableGraphSource` - Abstraction for searchable data sources
- `InMemoryGraphSource` - Adapter for DeclarationGraph
- `RepositoryGraphSource` - Adapter for IGraphRepository

#### `Sharpitect.Analysis.Analyzers`

Code analysis using Roslyn:

- `DeclarationVisitor` - Extracts declaration nodes and containment edges from syntax trees
- `ReferenceVisitor` - Extracts relationship edges (calls, references, inheritance)
- `SemanticProjectAnalyzer` - Two-pass analysis of a project
- `GraphSolutionAnalyzer` - Orchestrates analysis of an entire solution

### Analysis Pipeline

1. `GraphSolutionAnalyzer` - Opens solution, creates Solution/Project nodes
2. `SemanticProjectAnalyzer` - For each project:
   - **First pass**: `DeclarationVisitor` extracts declarations and containment edges
   - **Second pass**: `ReferenceVisitor` extracts references and relationships
3. Results are persisted via `IGraphRepository`
4. `GraphSearchService` can query the graph by text and kind filters
