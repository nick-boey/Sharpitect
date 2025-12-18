# Sharpitect

Sharpitect generates C4 architecture diagrams from annotated C# codebases. It analyzes your code using Roslyn and builds a declaration graph that can be queried and visualized.

## Features

- **Code Analysis** - Extracts declarations (classes, interfaces, methods, properties, etc.) and their relationships using Roslyn
- **Declaration Graph** - Builds a queryable graph of code elements with containment and reference relationships
- **Graph Search** - Search for declarations by name with multiple match modes and kind filters
- **SQLite Persistence** - Persist and query the declaration graph from a SQLite database
- **C4 Model Mapping** - Maps code elements to C4 architecture model levels

## C4 Conventions

Sharpitect maps C# constructs to C4 model elements following these conventions:

### Systems

- Systems are solutions (.sln files)

### Containers

- Containers are projects that generate an executable

### Components

- Components cannot be nested, but they can be grouped by namespaces or projects
- Components may be projects, namespaces, or groups
- Use the `[Component]` attribute to mark types as C4 components

### Code

- Code elements are automatically extracted class diagrams including classes, interfaces, methods, and properties

## Usage

### Analyzing a Solution

```csharp
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Persistence;

// Create repository
var repository = new SqliteGraphRepository("architecture.db");
await repository.InitializeAsync();

// Analyze solution
var analyzer = new GraphSolutionAnalyzer(repository);
await analyzer.AnalyzeAsync("path/to/solution.sln");
```

### Searching the Graph

```csharp
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;

// Create search service with in-memory source
var graph = new DeclarationGraph();
var source = new InMemoryGraphSource(graph);
var searchService = new GraphSearchService(source);

// Search for classes containing "Service"
var query = new SearchQuery
{
    SearchText = "Service",
    MatchMode = SearchMatchMode.Contains,
    KindFilter = [DeclarationKind.Class]
};

var results = await searchService.SearchAsync(query);
```

### Search Options

- **SearchText** - The text to search for in Name and FullyQualifiedName
- **MatchMode** - How to match: `Contains`, `StartsWith`, `EndsWith`, or `Exact`
- **KindFilter** - Optional filter by declaration kind (Class, Interface, Method, etc.)
- **CaseSensitive** - Whether the search is case-sensitive (default: false)

## Project Structure

- **Sharpitect.Tool** - Command-line tool entry point
- **Sharpitect.Analysis** - Analysis engine with graph, persistence, search, and analyzers
- **Sharpitect.Attributes** - Attributes for annotating code (targets .NET Framework 4.8 for broad compatibility)
- **Sharpitect.Analysis.Test** - Unit tests

## Building

```bash
dotnet build Sharpitect.sln
```

## Testing

```bash
dotnet test Sharpitect.sln
```

## License

See LICENSE file for details.
