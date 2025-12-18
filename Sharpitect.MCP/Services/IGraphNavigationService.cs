using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Service for navigating the declaration graph.
/// </summary>
public interface IGraphNavigationService
{
    /// <summary>
    /// Searches for declarations matching the query.
    /// </summary>
    Task<SearchResults> SearchAsync(
        string query,
        SearchMatchMode matchMode = SearchMatchMode.Contains,
        IReadOnlyCollection<DeclarationKind>? kindFilter = null,
        bool caseSensitive = false,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a node by ID.
    /// </summary>
    Task<NodeDetail?> GetNodeAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children (contained declarations) of a node.
    /// </summary>
    Task<ChildrenResult?> GetChildrenAsync(
        string parentId,
        DeclarationKind? kindFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the ancestor chain (containment hierarchy) of a node.
    /// </summary>
    Task<AncestorsResult?> GetAncestorsAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets relationships for a node.
    /// </summary>
    Task<RelationshipsResult?> GetRelationshipsAsync(
        string nodeId,
        RelationshipDirection direction = RelationshipDirection.Both,
        RelationshipKind? kindFilter = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets callers of a method or property.
    /// </summary>
    Task<CallersResult?> GetCallersAsync(
        string nodeId,
        int depth = 1,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets callees of a method or property.
    /// </summary>
    Task<CalleesResult?> GetCalleesAsync(
        string nodeId,
        int depth = 1,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the inheritance hierarchy for a class or interface.
    /// </summary>
    Task<InheritanceResult?> GetInheritanceAsync(
        string nodeId,
        InheritanceDirection direction = InheritanceDirection.Both,
        int depth = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all declarations of a specific kind.
    /// </summary>
    Task<ListByKindResult> ListByKindAsync(
        DeclarationKind kind,
        string? scopeId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets project dependencies.
    /// </summary>
    Task<DependenciesResult?> GetDependenciesAsync(
        string projectId,
        bool includeTransitive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects that depend on a given project.
    /// </summary>
    Task<DependentsResult?> GetDependentsAsync(
        string projectId,
        bool includeTransitive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all declarations in a source file.
    /// </summary>
    Task<FileDeclarationsResult?> GetFileDeclarationsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all usages of a declaration.
    /// </summary>
    Task<UsagesResult?> GetUsagesAsync(
        string nodeId,
        UsageKind? usageKindFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets signature information for a method, property, or type.
    /// </summary>
    Task<SignatureResult?> GetSignatureAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the node details and source code for a declaration.
    /// </summary>
    Task<CodeResult?> GetCodeAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the containment tree starting from a node or from solution roots.
    /// </summary>
    Task<TreeResult> GetTreeAsync(
        string? rootId = null,
        DeclarationKind? kindFilter = null,
        int maxDepth = 2,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Direction for relationship queries.
/// </summary>
public enum RelationshipDirection
{
    Outgoing,
    Incoming,
    Both
}

/// <summary>
/// Direction for inheritance queries.
/// </summary>
public enum InheritanceDirection
{
    Ancestors,
    Descendants,
    Both
}

/// <summary>
/// Kind of usage to filter.
/// </summary>
public enum UsageKind
{
    All,
    Call,
    TypeReference,
    Inheritance,
    Instantiation
}
