using System.ComponentModel;
using ModelContextProtocol.Server;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;
using Sharpitect.MCP.Services;

namespace Sharpitect.MCP.Tools;

/// <summary>
/// MCP tools for navigating .NET codebases via the declaration graph.
/// </summary>
[McpServerToolType]
public static class GraphNavigationTools
{
    /// <summary>
    /// Searches for declarations by name with optional filters.
    /// </summary>
    [McpServerTool,
     Description("Search for declarations (classes, methods, properties) by name with optional filters.")]
    public static async Task<string> SearchDeclarations(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Search text to match against declaration names")]
        string query,
        [Description("Match mode: contains, starts_with, ends_with, or exact. Defaults to contains.")]
        string? matchMode = null,
        [Description("Filter by kind: class, interface, method, property, namespace, project, etc.")]
        string? kind = null,
        [Description("Case-sensitive search. Defaults to false.")]
        bool caseSensitive = false,
        [Description("Maximum number of results. Defaults to 50.")]
        int limit = 50,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var matchModeEnum = ParseMatchMode(matchMode);
        var kindFilter = ParseKindFilter(kind);

        var result = await navigationService.SearchAsync(
            query,
            matchModeEnum,
            kindFilter,
            caseSensitive,
            limit);

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets detailed information about a specific declaration node.
    /// </summary>
    [McpServerTool, Description("Get detailed information about a specific declaration node by name.")]
    public static async Task<string> GetNode(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Fully qualified node name (e.g., Namespace.ClassName.MethodName)")]
        string name,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null,
        [Description("Maximum number of similar nodes to suggest if not found. Defaults to 5.")]
        int suggestionLimit = 5)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetNodeAsync(name);
        if (result == null)
        {
            return await FormatNotFoundWithSuggestionsAsync(
                navigationService, formatter, name, "Node", suggestionLimit);
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the children (contained declarations) of a node.
    /// </summary>
    [McpServerTool, Description("Get the immediate children (contents) of a declaration.")]
    public static async Task<string> GetChildren(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Parent node ID")] string id,
        [Description("Filter children by kind")]
        string? kind = null,
        [Description("Maximum number of results. Defaults to 100.")]
        int limit = 100,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var kindFilter = kind != null ? ParseDeclarationKind(kind) : null;

        var result = await navigationService.GetChildrenAsync(id, kindFilter, limit);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the ancestor chain (containment hierarchy) of a node.
    /// </summary>
    [McpServerTool, Description("Get the containment hierarchy path from root to a node.")]
    public static async Task<string> GetAncestors(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Node ID")] string id,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetAncestorsAsync(id);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets relationships for a node.
    /// </summary>
    [McpServerTool, Description("Get relationships for a node (what it calls, what calls it, inheritance, etc.).")]
    public static async Task<string> GetRelationships(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Node ID")] string id,
        [Description("Direction: outgoing, incoming, or both. Defaults to both.")]
        string? direction = null,
        [Description("Filter by relationship kind: calls, inherits, implements, references, uses")]
        string? relationshipKind = null,
        [Description("Maximum results per direction. Defaults to 50.")]
        int limit = 50,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var directionEnum = ParseRelationshipDirection(direction);
        var kindFilter = relationshipKind != null ? ParseRelationshipKind(relationshipKind) : null;

        var result = await navigationService.GetRelationshipsAsync(id, directionEnum, kindFilter, limit);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets all methods/properties that call a specific method or property.
    /// </summary>
    [McpServerTool, Description("Get all methods/properties that call a specific method or property.")]
    public static async Task<string> GetCallers(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Method or property ID")] string id,
        [Description("Traversal depth. Defaults to 1, max 5.")]
        int depth = 1,
        [Description("Maximum results. Defaults to 50.")]
        int limit = 50,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetCallersAsync(id, depth, limit);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets all methods/properties called by a specific method or property.
    /// </summary>
    [McpServerTool, Description("Get all methods/properties called by a specific method or property.")]
    public static async Task<string> GetCallees(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Method or property ID")] string id,
        [Description("Traversal depth. Defaults to 1, max 5.")]
        int depth = 1,
        [Description("Maximum results. Defaults to 50.")]
        int limit = 50,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetCalleesAsync(id, depth, limit);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the inheritance hierarchy for a class or interface.
    /// </summary>
    [McpServerTool, Description("Get the inheritance hierarchy for a class or interface.")]
    public static async Task<string> GetInheritance(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Class or interface ID")] string id,
        [Description("Direction: ancestors (base types), descendants (derived types), or both. Defaults to both.")]
        string? direction = null,
        [Description("Traversal depth. Defaults to 10.")]
        int depth = 10,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var directionEnum = ParseInheritanceDirection(direction);

        var result = await navigationService.GetInheritanceAsync(id, directionEnum, depth);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Lists all declarations of a specific kind within a scope.
    /// </summary>
    [McpServerTool, Description("List all declarations of a specific kind within a scope.")]
    public static async Task<string> ListByKind(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Declaration kind: class, interface, enum, struct, method, property, namespace, project")]
        string kind,
        [Description("Limit to scope (project or namespace ID). If omitted, searches entire solution.")]
        string? scope = null,
        [Description("Maximum results. Defaults to 100.")]
        int limit = 100,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var kindEnum = ParseDeclarationKind(kind);

        if (kindEnum == null)
        {
            return formatter.Format(ErrorResponse.InvalidParameter($"Invalid kind: '{kind}'"));
        }

        var result = await navigationService.ListByKindAsync(kindEnum.Value, scope, limit);
        return formatter.Format(result);
    }

    /// <summary>
    /// Gets project-level dependencies.
    /// </summary>
    [McpServerTool, Description("Get project-level dependencies (what a project references).")]
    public static async Task<string> GetDependencies(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Project ID")] string id,
        [Description("Include transitive dependencies. Defaults to false.")]
        bool includeTransitive = false,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetDependenciesAsync(id, includeTransitive);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Project with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets projects that depend on a given project.
    /// </summary>
    [McpServerTool, Description("Get projects that depend on a given project.")]
    public static async Task<string> GetDependents(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Project ID")] string id,
        [Description("Include transitive dependents. Defaults to false.")]
        bool includeTransitive = false,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetDependentsAsync(id, includeTransitive);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Project with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets all declarations in a specific source file.
    /// </summary>
    [McpServerTool, Description("Get all declarations defined in a specific source file.")]
    public static async Task<string> GetFileDeclarations(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Source file path (relative or absolute)")]
        string filePath,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetFileDeclarationsAsync(filePath);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"No declarations found in file '{filePath}'."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Finds all usages of a type, method, or property across the codebase.
    /// </summary>
    [McpServerTool, Description("Find all usages of a type, method, or property across the codebase.")]
    public static async Task<string> GetUsages(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Declaration ID")] string id,
        [Description("Filter by usage kind: all, call, type_reference, inheritance, instantiation. Defaults to all.")]
        string? usageKind = null,
        [Description("Maximum results. Defaults to 100.")]
        int limit = 100,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var usageKindFilter = ParseUsageKind(usageKind);

        var result = await navigationService.GetUsagesAsync(id, usageKindFilter, limit);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the full signature and type information for a method, property, or type.
    /// </summary>
    [McpServerTool, Description("Get the full signature and type information for a method, property, or type.")]
    public static async Task<string> GetSignature(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Declaration ID")] string id,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetSignatureAsync(id);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the source code for a declaration.
    /// </summary>
    [McpServerTool, Description("Get the source code for a declaration by reading from the source file.")]
    public static async Task<string> GetCode(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Declaration ID")] string id,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);

        var result = await navigationService.GetCodeAsync(id);
        if (result == null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{id}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    /// <summary>
    /// Gets the containment tree starting from a node or from solution roots.
    /// </summary>
    [McpServerTool,
     Description(
         "Get the containment tree showing nested structure. If no root ID provided, shows from solution level.")]
    public static async Task<string> GetTree(
        IGraphNavigationService navigationService,
        IOutputFormatterFactory formatterFactory,
        [Description("Root node ID to start from. If omitted, starts from solution roots.")]
        string? rootId = null,
        [Description(
            "Filter to only show nodes of this kind: class, interface, method, property, namespace, project, etc.")]
        string? kind = null,
        [Description("Maximum depth levels to display. Defaults to 2.")]
        int maxDepth = 2,
        [Description("Output format: json or text. Defaults to json.")]
        string? format = null)
    {
        var formatter = formatterFactory.GetFormatter(format);
        var kindFilter = kind != null ? ParseDeclarationKind(kind) : null;

        var result = await navigationService.GetTreeAsync(rootId, kindFilter, maxDepth);

        if (result.Roots.Count == 0 && rootId != null)
        {
            return formatter.Format(ErrorResponse.NotFound($"Node with ID '{rootId}' was not found in the graph."));
        }

        return formatter.Format(result);
    }

    #region Resolution Helpers

    /// <summary>
    /// Formats a not-found error with similar node suggestions.
    /// </summary>
    private static async Task<string> FormatNotFoundWithSuggestionsAsync(
        IGraphNavigationService navigationService,
        IOutputFormatter formatter,
        string identifier,
        string entityDescription,
        int suggestionLimit = 5)
    {
        var resolution = await navigationService.ResolveNodeAsync(identifier, suggestionLimit);

        if (resolution is NodeResolutionResult.NotResolved notResolved && notResolved.SimilarNodes.Count > 0)
        {
            return formatter.Format(new NodeNotFoundResponse(
                $"{entityDescription} '{identifier}' was not found. Did you mean one of these?",
                notResolved.SimilarNodes));
        }

        // No suggestions found, use simple error message
        return formatter.Format(ErrorResponse.NotFound(
            $"{entityDescription} '{identifier}' was not found in the graph."));
    }

    #endregion

    #region Parsing Helpers

    private static SearchMatchMode ParseMatchMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode))
        {
            return SearchMatchMode.Contains;
        }

        return mode.ToLowerInvariant() switch
        {
            "contains" => SearchMatchMode.Contains,
            "starts_with" or "startswith" => SearchMatchMode.StartsWith,
            "ends_with" or "endswith" => SearchMatchMode.EndsWith,
            "exact" => SearchMatchMode.Exact,
            _ => SearchMatchMode.Contains
        };
    }

    private static IReadOnlyCollection<DeclarationKind>? ParseKindFilter(string? kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        var parsedKind = ParseDeclarationKind(kind);
        return parsedKind.HasValue ? [parsedKind.Value] : null;
    }

    private static DeclarationKind? ParseDeclarationKind(string? kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        return kind.ToLowerInvariant() switch
        {
            "solution" => DeclarationKind.Solution,
            "project" => DeclarationKind.Project,
            "namespace" => DeclarationKind.Namespace,
            "class" => DeclarationKind.Class,
            "interface" => DeclarationKind.Interface,
            "struct" => DeclarationKind.Struct,
            "record" => DeclarationKind.Record,
            "enum" => DeclarationKind.Enum,
            "delegate" => DeclarationKind.Delegate,
            "method" => DeclarationKind.Method,
            "constructor" => DeclarationKind.Constructor,
            "property" => DeclarationKind.Property,
            "field" => DeclarationKind.Field,
            "event" => DeclarationKind.Event,
            "indexer" => DeclarationKind.Indexer,
            "enummember" or "enum_member" => DeclarationKind.EnumMember,
            "parameter" => DeclarationKind.Parameter,
            "typeparameter" or "type_parameter" => DeclarationKind.TypeParameter,
            "localvariable" or "local_variable" => DeclarationKind.LocalVariable,
            "localfunction" or "local_function" => DeclarationKind.LocalFunction,
            _ => null
        };
    }

    private static RelationshipDirection ParseRelationshipDirection(string? direction)
    {
        if (string.IsNullOrEmpty(direction))
        {
            return RelationshipDirection.Both;
        }

        return direction.ToLowerInvariant() switch
        {
            "outgoing" => RelationshipDirection.Outgoing,
            "incoming" => RelationshipDirection.Incoming,
            "both" => RelationshipDirection.Both,
            _ => RelationshipDirection.Both
        };
    }

    private static RelationshipKind? ParseRelationshipKind(string? kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        return kind.ToLowerInvariant() switch
        {
            "calls" => RelationshipKind.Calls,
            "inherits" => RelationshipKind.Inherits,
            "implements" => RelationshipKind.Implements,
            "references" => RelationshipKind.References,
            "uses" => RelationshipKind.Uses,
            "constructs" => RelationshipKind.Constructs,
            "contains" => RelationshipKind.Contains,
            "overrides" => RelationshipKind.Overrides,
            "dependson" or "depends_on" => RelationshipKind.DependsOn,
            "imports" => RelationshipKind.Imports,
            _ => null
        };
    }

    private static InheritanceDirection ParseInheritanceDirection(string? direction)
    {
        if (string.IsNullOrEmpty(direction))
        {
            return InheritanceDirection.Both;
        }

        return direction.ToLowerInvariant() switch
        {
            "ancestors" => InheritanceDirection.Ancestors,
            "descendants" => InheritanceDirection.Descendants,
            "both" => InheritanceDirection.Both,
            _ => InheritanceDirection.Both
        };
    }

    private static UsageKind? ParseUsageKind(string? kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return null;
        }

        return kind.ToLowerInvariant() switch
        {
            "all" => UsageKind.All,
            "call" => UsageKind.Call,
            "type_reference" or "typereference" => UsageKind.TypeReference,
            "inheritance" => UsageKind.Inheritance,
            "instantiation" => UsageKind.Instantiation,
            _ => UsageKind.All
        };
    }

    #endregion
}