namespace Sharpitect.Analysis.Graph;

/// <summary>
/// The root container for the declaration graph.
/// </summary>
public sealed class DeclarationGraph
{
    private readonly Dictionary<string, DeclarationNode> _nodes = new();
    private readonly List<RelationshipEdge> _edges = [];

    /// <summary>
    /// Gets all nodes in the graph indexed by ID.
    /// </summary>
    public IReadOnlyDictionary<string, DeclarationNode> Nodes => _nodes;

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IReadOnlyList<RelationshipEdge> Edges => _edges;

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(DeclarationNode node) => _nodes[node.Id] = node;

    /// <summary>
    /// Adds multiple nodes to the graph.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    public void AddNodes(IEnumerable<DeclarationNode> nodes)
    {
        foreach (var node in nodes)
        {
            _nodes[node.Id] = node;
        }
    }

    /// <summary>
    /// Adds an edge to the graph.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    public void AddEdge(RelationshipEdge edge) => _edges.Add(edge);

    /// <summary>
    /// Adds multiple edges to the graph.
    /// </summary>
    /// <param name="edges">The edges to add.</param>
    public void AddEdges(IEnumerable<RelationshipEdge> edges) => _edges.AddRange(edges);

    /// <summary>
    /// Gets a node by ID.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>The node if found, otherwise null.</returns>
    public DeclarationNode? GetNode(string id) => _nodes.GetValueOrDefault(id);

    /// <summary>
    /// Checks if a node exists in the graph.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>True if the node exists.</returns>
    public bool ContainsNode(string id) => _nodes.ContainsKey(id);

    /// <summary>
    /// Gets all outgoing edges from a node.
    /// </summary>
    /// <param name="nodeId">The source node ID.</param>
    /// <returns>All edges originating from the node.</returns>
    public IEnumerable<RelationshipEdge> GetOutgoingEdges(string nodeId) =>
        _edges.Where(e => e.SourceId == nodeId);

    /// <summary>
    /// Gets all incoming edges to a node.
    /// </summary>
    /// <param name="nodeId">The target node ID.</param>
    /// <returns>All edges pointing to the node.</returns>
    public IEnumerable<RelationshipEdge> GetIncomingEdges(string nodeId) =>
        _edges.Where(e => e.TargetId == nodeId);

    /// <summary>
    /// Gets all nodes of a specific kind.
    /// </summary>
    /// <param name="kind">The declaration kind to filter by.</param>
    /// <returns>All nodes of the specified kind.</returns>
    public IEnumerable<DeclarationNode> GetNodesByKind(DeclarationKind kind) =>
        _nodes.Values.Where(n => n.Kind == kind);

    /// <summary>
    /// Gets all edges of a specific kind.
    /// </summary>
    /// <param name="kind">The relationship kind to filter by.</param>
    /// <returns>All edges of the specified kind.</returns>
    public IEnumerable<RelationshipEdge> GetEdgesByKind(RelationshipKind kind) =>
        _edges.Where(e => e.Kind == kind);

    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the total number of edges in the graph.
    /// </summary>
    public int EdgeCount => _edges.Count;

    /// <summary>
    /// Clears all nodes and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
    }
}