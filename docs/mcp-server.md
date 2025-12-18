# Sharpitect MCP Server

This document describes the MCP (Model Context Protocol) tools provided by Sharpitect for navigating .NET codebases.

## Overview

The Sharpitect MCP server exposes the declaration graph through a set of tools that enable efficient codebase navigation
without reading individual files. All tools support two output formats:

- **JSON** (default): Structured output for programmatic use
- **Text**: Human-readable output for debugging

Set the `format` parameter to `json` or `text` to control output format.

---

## Tools

### 1. `search_declarations`

Search for declarations by name with optional filters.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `query` | string | Yes | Search text |
| `match_mode` | string | No | `contains` (default), `starts_with`, `ends_with`, `exact` |
| `kind` | string | No | Filter by kind: `class`, `interface`, `method`, `property`, `namespace`, `project`, etc. |
| `case_sensitive` | boolean | No | Case-sensitive search (default: false) |
| `limit` | integer | No | Max results (default: 50) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "query": "GraphSearch",
  "match_mode": "contains",
  "kind": "class",
  "limit": 10
}
```

**Example JSON Output:**

```json
{
  "results": [
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService",
      "name": "GraphSearchService",
      "kind": "Class",
      "c4_level": "Code",
      "file_path": "src/Sharpitect.Analysis/Search/GraphSearchService.cs",
      "line_number": 12
    },
    {
      "id": "Sharpitect.Analysis.Search.IGraphSearchService",
      "name": "IGraphSearchService",
      "kind": "Interface",
      "c4_level": "Code",
      "file_path": "src/Sharpitect.Analysis/Search/IGraphSearchService.cs",
      "line_number": 5
    }
  ],
  "total_count": 2,
  "truncated": false
}
```

**Example Text Output:**

```
Search results for "GraphSearch" (2 matches):

[Class] GraphSearchService
  Path: src/Sharpitect.Analysis/Search/GraphSearchService.cs:12
  Full ID: Sharpitect.Analysis.Search.GraphSearchService

[Interface] IGraphSearchService
  Path: src/Sharpitect.Analysis/Search/IGraphSearchService.cs:5
  Full ID: Sharpitect.Analysis.Search.IGraphSearchService
```

---

### 2. `get_node`

Get detailed information about a specific declaration node.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Full node ID (e.g., `Namespace.ClassName.MethodName`) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService"
}
```

**Example JSON Output:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService",
  "name": "GraphSearchService",
  "kind": "Class",
  "c4_level": "Code",
  "file_path": "src/Sharpitect.Analysis/Search/GraphSearchService.cs",
  "line_number": 12,
  "modifiers": [
    "public",
    "sealed"
  ],
  "documentation": "Default implementation of IGraphSearchService for querying the declaration graph."
}
```

**Example Text Output:**

```
[Class] GraphSearchService (public sealed)
  File: src/Sharpitect.Analysis/Search/GraphSearchService.cs:12
  Full ID: Sharpitect.Analysis.Search.GraphSearchService
  C4 Level: Code

  Documentation:
    Default implementation of IGraphSearchService for querying the declaration graph.
```

---

### 3. `get_children`

Get the immediate children (contents) of a declaration.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Parent node ID |
| `kind` | string | No | Filter children by kind |
| `limit` | integer | No | Max results (default: 100) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService",
  "kind": "method"
}
```

**Example JSON Output:**

```json
{
  "parent_id": "Sharpitect.Analysis.Search.GraphSearchService",
  "children": [
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
      "name": "Search",
      "kind": "Method",
      "signature": "IEnumerable<DeclarationNode> Search(SearchQuery query)",
      "line_number": 28
    },
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService.SearchAsync",
      "name": "SearchAsync",
      "kind": "Method",
      "signature": "Task<IEnumerable<DeclarationNode>> SearchAsync(SearchQuery query, CancellationToken ct)",
      "line_number": 45
    }
  ],
  "total_count": 2,
  "truncated": false
}
```

**Example Text Output:**

```
Children of GraphSearchService (2 methods):

  [Method] Search(SearchQuery query) -> IEnumerable<DeclarationNode>
    Line: 28

  [Method] SearchAsync(SearchQuery query, CancellationToken ct) -> Task<IEnumerable<DeclarationNode>>
    Line: 45
```

---

### 4. `get_ancestors`

Get the containment hierarchy path from root to a node.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Node ID |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService.Search"
}
```

**Example JSON Output:**

```json
{
  "node_id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "ancestors": [
    {
      "id": "Sharpitect.sln",
      "name": "Sharpitect",
      "kind": "Solution"
    },
    {
      "id": "Sharpitect.Analysis",
      "name": "Sharpitect.Analysis",
      "kind": "Project"
    },
    {
      "id": "Sharpitect.Analysis.Search",
      "name": "Search",
      "kind": "Namespace"
    },
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService",
      "name": "GraphSearchService",
      "kind": "Class"
    }
  ]
}
```

**Example Text Output:**

```
Ancestry of Search method:

  Solution: Sharpitect
    └─ Project: Sharpitect.Analysis
        └─ Namespace: Search
            └─ Class: GraphSearchService
                └─ Method: Search  <-- target
```

---

### 5. `get_relationships`

Get relationships for a node (what it calls, what calls it, inheritance, etc.).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Node ID |
| `direction` | string | No | `outgoing`, `incoming`, or `both` (default) |
| `relationship_kind` | string | No | Filter: `calls`, `inherits`, `implements`, `references`, `uses` |
| `limit` | integer | No | Max results per direction (default: 50) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService",
  "direction": "both"
}
```

**Example JSON Output:**

```json
{
  "node_id": "Sharpitect.Analysis.Search.GraphSearchService",
  "outgoing": [
    {
      "kind": "Implements",
      "target_id": "Sharpitect.Analysis.Search.IGraphSearchService",
      "target_name": "IGraphSearchService",
      "target_kind": "Interface"
    },
    {
      "kind": "Uses",
      "target_id": "Sharpitect.Analysis.Search.ISearchableGraphSource",
      "target_name": "ISearchableGraphSource",
      "target_kind": "Interface"
    }
  ],
  "incoming": [
    {
      "kind": "References",
      "source_id": "Sharpitect.Analysis.Test.Search.GraphSearchServiceTests",
      "source_name": "GraphSearchServiceTests",
      "source_kind": "Class"
    }
  ]
}
```

**Example Text Output:**

```
Relationships for GraphSearchService:

OUTGOING:
  ──[Implements]──> IGraphSearchService (Interface)
  ──[Uses]──> ISearchableGraphSource (Interface)

INCOMING:
  <──[References]── GraphSearchServiceTests (Class)
```

---

### 6. `get_callers`

Get all methods/properties that call a specific method or property.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Method or property ID |
| `depth` | integer | No | Traversal depth (default: 1, max: 5) |
| `limit` | integer | No | Max results (default: 50) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "depth": 2
}
```

**Example JSON Output:**

```json
{
  "target_id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "callers": [
    {
      "id": "Sharpitect.Tool.Commands.SearchCommand.Execute",
      "name": "Execute",
      "kind": "Method",
      "file_path": "src/Sharpitect.Tool/Commands/SearchCommand.cs",
      "line_number": 34,
      "depth": 1
    },
    {
      "id": "Sharpitect.Tool.Program.Main",
      "name": "Main",
      "kind": "Method",
      "file_path": "src/Sharpitect.Tool/Program.cs",
      "line_number": 15,
      "depth": 2
    }
  ],
  "total_count": 2,
  "max_depth_reached": false
}
```

**Example Text Output:**

```
Callers of GraphSearchService.Search (depth: 2):

Depth 1:
  [Method] SearchCommand.Execute
    File: src/Sharpitect.Tool/Commands/SearchCommand.cs:34

Depth 2:
  [Method] Program.Main
    File: src/Sharpitect.Tool/Program.cs:15
```

---

### 7. `get_callees`

Get all methods/properties called by a specific method or property.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Method or property ID |
| `depth` | integer | No | Traversal depth (default: 1, max: 5) |
| `limit` | integer | No | Max results (default: 50) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "depth": 1
}
```

**Example JSON Output:**

```json
{
  "source_id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "callees": [
    {
      "id": "Sharpitect.Analysis.Search.ISearchableGraphSource.GetNodes",
      "name": "GetNodes",
      "kind": "Method",
      "file_path": "src/Sharpitect.Analysis/Search/ISearchableGraphSource.cs",
      "line_number": 12,
      "depth": 1
    },
    {
      "id": "Sharpitect.Analysis.Search.SearchQuery.Matches",
      "name": "Matches",
      "kind": "Method",
      "file_path": "src/Sharpitect.Analysis/Search/SearchQuery.cs",
      "line_number": 28,
      "depth": 1
    }
  ],
  "total_count": 2,
  "max_depth_reached": false
}
```

**Example Text Output:**

```
Callees of GraphSearchService.Search (depth: 1):

Depth 1:
  [Method] ISearchableGraphSource.GetNodes
    File: src/Sharpitect.Analysis/Search/ISearchableGraphSource.cs:12

  [Method] SearchQuery.Matches
    File: src/Sharpitect.Analysis/Search/SearchQuery.cs:28
```

---

### 8. `get_inheritance`

Get the inheritance hierarchy for a class or interface.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Class or interface ID |
| `direction` | string | No | `ancestors` (base types), `descendants` (derived types), or `both` (default) |
| `depth` | integer | No | Traversal depth (default: 10) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.IGraphSearchService",
  "direction": "descendants"
}
```

**Example JSON Output:**

```json
{
  "node_id": "Sharpitect.Analysis.Search.IGraphSearchService",
  "ancestors": [],
  "descendants": [
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService",
      "name": "GraphSearchService",
      "kind": "Class",
      "relationship": "Implements",
      "depth": 1
    }
  ]
}
```

**Example Text Output:**

```
Inheritance hierarchy for IGraphSearchService:

BASE TYPES: (none)

DERIVED TYPES:
  └─ [Class] GraphSearchService (implements)
```

---

### 9. `list_by_kind`

List all declarations of a specific kind within a scope.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `kind` | string | Yes | Declaration kind: `class`, `interface`, `enum`, `struct`, `method`, `property`, `namespace`,
`project` |
| `scope` | string | No | Limit to scope (project or namespace ID). If omitted, searches entire solution |
| `limit` | integer | No | Max results (default: 100) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "kind": "interface",
  "scope": "Sharpitect.Analysis"
}
```

**Example JSON Output:**

```json
{
  "kind": "Interface",
  "scope": "Sharpitect.Analysis",
  "results": [
    {
      "id": "Sharpitect.Analysis.Persistence.IGraphRepository",
      "name": "IGraphRepository",
      "file_path": "src/Sharpitect.Analysis/Persistence/IGraphRepository.cs",
      "line_number": 8
    },
    {
      "id": "Sharpitect.Analysis.Search.IGraphSearchService",
      "name": "IGraphSearchService",
      "file_path": "src/Sharpitect.Analysis/Search/IGraphSearchService.cs",
      "line_number": 5
    },
    {
      "id": "Sharpitect.Analysis.Search.ISearchableGraphSource",
      "name": "ISearchableGraphSource",
      "file_path": "src/Sharpitect.Analysis/Search/ISearchableGraphSource.cs",
      "line_number": 7
    }
  ],
  "total_count": 3,
  "truncated": false
}
```

**Example Text Output:**

```
Interfaces in Sharpitect.Analysis (3 total):

  IGraphRepository
    src/Sharpitect.Analysis/Persistence/IGraphRepository.cs:8

  IGraphSearchService
    src/Sharpitect.Analysis/Search/IGraphSearchService.cs:5

  ISearchableGraphSource
    src/Sharpitect.Analysis/Search/ISearchableGraphSource.cs:7
```

---

### 10. `get_dependencies`

Get project-level dependencies (what a project references).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Project ID |
| `include_transitive` | boolean | No | Include transitive dependencies (default: false) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Tool",
  "include_transitive": true
}
```

**Example JSON Output:**

```json
{
  "project_id": "Sharpitect.Tool",
  "dependencies": [
    {
      "id": "Sharpitect.Analysis",
      "name": "Sharpitect.Analysis",
      "kind": "Project",
      "is_transitive": false
    },
    {
      "id": "Sharpitect.Attributes",
      "name": "Sharpitect.Attributes",
      "kind": "Project",
      "is_transitive": true,
      "via": "Sharpitect.Analysis"
    },
    {
      "id": "Microsoft.CodeAnalysis.CSharp.Workspaces",
      "name": "Microsoft.CodeAnalysis.CSharp.Workspaces",
      "kind": "Package",
      "version": "4.8.0",
      "is_transitive": false
    }
  ]
}
```

**Example Text Output:**

```
Dependencies of Sharpitect.Tool:

DIRECT:
  [Project] Sharpitect.Analysis
  [Package] Microsoft.CodeAnalysis.CSharp.Workspaces (4.8.0)

TRANSITIVE:
  [Project] Sharpitect.Attributes (via Sharpitect.Analysis)
```

---

### 11. `get_dependents`

Get projects that depend on a given project.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Project ID |
| `include_transitive` | boolean | No | Include transitive dependents (default: false) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis"
}
```

**Example JSON Output:**

```json
{
  "project_id": "Sharpitect.Analysis",
  "dependents": [
    {
      "id": "Sharpitect.Tool",
      "name": "Sharpitect.Tool",
      "kind": "Project",
      "is_transitive": false
    },
    {
      "id": "Sharpitect.Analysis.Test",
      "name": "Sharpitect.Analysis.Test",
      "kind": "Project",
      "is_transitive": false
    }
  ]
}
```

**Example Text Output:**

```
Dependents of Sharpitect.Analysis:

  [Project] Sharpitect.Tool
  [Project] Sharpitect.Analysis.Test
```

---

### 12. `get_file_declarations`

Get all declarations defined in a specific source file.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `file_path` | string | Yes | Source file path (relative or absolute) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "file_path": "src/Sharpitect.Analysis/Search/GraphSearchService.cs"
}
```

**Example JSON Output:**

```json
{
  "file_path": "src/Sharpitect.Analysis/Search/GraphSearchService.cs",
  "declarations": [
    {
      "id": "Sharpitect.Analysis.Search.GraphSearchService",
      "name": "GraphSearchService",
      "kind": "Class",
      "line_number": 12,
      "children": [
        {
          "id": "Sharpitect.Analysis.Search.GraphSearchService._source",
          "name": "_source",
          "kind": "Field",
          "line_number": 14
        },
        {
          "id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
          "name": "Search",
          "kind": "Method",
          "line_number": 28
        }
      ]
    }
  ]
}
```

**Example Text Output:**

```
Declarations in GraphSearchService.cs:

[Class] GraphSearchService (line 12)
  ├─ [Field] _source (line 14)
  ├─ [Constructor] GraphSearchService (line 18)
  ├─ [Method] Search (line 28)
  └─ [Method] SearchAsync (line 45)
```

---

### 13. `get_usages`

Find all usages of a type, method, or property across the codebase.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Declaration ID |
| `usage_kind` | string | No | Filter: `all` (default), `call`, `type_reference`, `inheritance`, `instantiation` |
| `limit` | integer | No | Max results (default: 100) |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Graph.DeclarationNode",
  "usage_kind": "all",
  "limit": 20
}
```

**Example JSON Output:**

```json
{
  "target_id": "Sharpitect.Analysis.Graph.DeclarationNode",
  "usages": [
    {
      "location_id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
      "location_name": "Search",
      "location_kind": "Method",
      "usage_kind": "TypeReference",
      "file_path": "src/Sharpitect.Analysis/Search/GraphSearchService.cs",
      "line_number": 28
    },
    {
      "location_id": "Sharpitect.Analysis.Analyzers.DeclarationVisitor.VisitClassDeclaration",
      "location_name": "VisitClassDeclaration",
      "location_kind": "Method",
      "usage_kind": "Instantiation",
      "file_path": "src/Sharpitect.Analysis/Analyzers/DeclarationVisitor.cs",
      "line_number": 45
    }
  ],
  "total_count": 2,
  "truncated": false
}
```

**Example Text Output:**

```
Usages of DeclarationNode (2 found):

  [TypeReference] in GraphSearchService.Search
    src/Sharpitect.Analysis/Search/GraphSearchService.cs:28

  [Instantiation] in DeclarationVisitor.VisitClassDeclaration
    src/Sharpitect.Analysis/Analyzers/DeclarationVisitor.cs:45
```

---

### 14. `get_signature`

Get the full signature and type information for a method, property, or type.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Declaration ID |
| `format` | string | No | `json` (default) or `text` |

**Example Request:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService.Search"
}
```

**Example JSON Output:**

```json
{
  "id": "Sharpitect.Analysis.Search.GraphSearchService.Search",
  "name": "Search",
  "kind": "Method",
  "return_type": "IEnumerable<DeclarationNode>",
  "parameters": [
    {
      "name": "query",
      "type": "SearchQuery",
      "is_optional": false
    }
  ],
  "modifiers": [
    "public"
  ],
  "is_async": false,
  "is_static": false,
  "type_parameters": [],
  "documentation": "Searches the graph for declarations matching the specified query."
}
```

**Example Text Output:**

```
Signature of Search:

  public IEnumerable<DeclarationNode> Search(SearchQuery query)

  Parameters:
    - query: SearchQuery (required)

  Documentation:
    Searches the graph for declarations matching the specified query.
```

---

## Error Responses

All tools return consistent error responses:

**JSON Error:**

```json
{
  "error": true,
  "error_code": "NOT_FOUND",
  "message": "Node with ID 'Foo.Bar.Baz' was not found in the graph."
}
```

**Text Error:**

```
ERROR [NOT_FOUND]: Node with ID 'Foo.Bar.Baz' was not found in the graph.
```

**Error Codes:**
| Code | Description |
|------|-------------|
| `NOT_FOUND` | The requested node ID does not exist |
| `INVALID_PARAMETER` | A parameter value is invalid |
| `NOT_ANALYZED` | The solution/project has not been analyzed yet |
| `ANALYSIS_ERROR` | An error occurred during graph analysis |

---

## Typical Workflows

### Finding where a method is used

1. `search_declarations` to find the method
2. `get_callers` to find all call sites
3. `get_ancestors` on callers to understand context

### Understanding a class

1. `get_node` to get class details
2. `get_children` to see members
3. `get_relationships` to see implements/inherits
4. `get_usages` to see where it's used

### Navigating from file to code

1. `get_file_declarations` to see what's in a file
2. `get_children` on interesting types
3. `get_relationships` to explore connections

### Understanding project structure

1. `list_by_kind` with `kind: "project"` to list projects
2. `get_dependencies` on each project
3. `list_by_kind` with `kind: "namespace"` and `scope` to explore

### Tracing execution flow

1. `search_declarations` to find entry point
2. `get_callees` with depth to see what gets called
3. `get_callers` to see what triggers the entry point
