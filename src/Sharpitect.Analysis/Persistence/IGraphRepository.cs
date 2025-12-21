using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Persistence;

/// <summary>
/// Repository interface for persisting and querying the declaration graph.
/// </summary>
public interface IGraphRepository : IAsyncDisposable
{
    /// <summary>
    /// Initializes the repository (creates tables if needed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a declaration node.
    /// </summary>
    /// <param name="node">The node to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveNodeAsync(DeclarationNode node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple declaration nodes in a batch.
    /// </summary>
    /// <param name="nodes">The nodes to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveNodesAsync(IEnumerable<DeclarationNode> nodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a relationship edge.
    /// </summary>
    /// <param name="edge">The edge to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple edges in a batch.
    /// </summary>
    /// <param name="edges">The edges to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveEdgesAsync(IEnumerable<RelationshipEdge> edges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all nodes in the graph. 
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<DeclarationNode>> GetAllNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a node by ID.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node if found, otherwise null.</returns>
    Task<DeclarationNode?> GetNodeAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a node by fully qualified name.
    /// </summary>
    /// <param name="fullyQualifiedName">The fully qualified name of the node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node if found, otherwise null.</returns>
    Task<DeclarationNode?> GetNodeByFullyQualifiedNameAsync(string fullyQualifiedName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nodes by kind.
    /// </summary>
    /// <param name="kind">The declaration kind to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All nodes of the specified kind.</returns>
    Task<IReadOnlyList<DeclarationNode>> GetNodesByKindAsync(DeclarationKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nodes by file path.
    /// </summary>
    /// <param name="filePath">The file path to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All nodes in the specified file.</returns>
    Task<IReadOnlyList<DeclarationNode>> GetNodesByFileAsync(string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all outgoing edges from a node.
    /// </summary>
    /// <param name="nodeId">The source node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All edges originating from the node.</returns>
    Task<IReadOnlyList<RelationshipEdge>> GetOutgoingEdgesAsync(string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all incoming edges to a node.
    /// </summary>
    /// <param name="nodeId">The target node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All edges pointing to the node.</returns>
    Task<IReadOnlyList<RelationshipEdge>> GetIncomingEdgesAsync(string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets edges by kind.
    /// </summary>
    /// <param name="kind">The relationship kind to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All edges of the specified kind.</returns>
    Task<IReadOnlyList<RelationshipEdge>> GetEdgesByKindAsync(RelationshipKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a node by ID.
    /// </summary>
    /// <param name="id">The node ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteNodeAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple nodes by ID.
    /// </summary>
    /// <param name="ids">The node IDs to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteNodesAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all nodes in a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteNodesByFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all edges originating from a specific file.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteEdgesBySourceFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all edges originating from a specific file.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All edges with the specified source file path.</returns>
    Task<IReadOnlyList<RelationshipEdge>> GetEdgesBySourceFileAsync(string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all data from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the entire graph into memory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete declaration graph.</returns>
    Task<DeclarationGraph> LoadGraphAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of nodes in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of nodes.</returns>
    Task<int> GetNodeCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of edges in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of edges.</returns>
    Task<int> GetEdgeCountAsync(CancellationToken cancellationToken = default);
}