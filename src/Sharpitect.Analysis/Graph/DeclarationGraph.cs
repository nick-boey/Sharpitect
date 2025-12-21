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

    /// <summary>
    /// Removes a node by ID.
    /// </summary>
    /// <param name="id">The node ID to remove.</param>
    /// <returns>True if the node was removed, false if it didn't exist.</returns>
    public bool RemoveNode(string id) => _nodes.Remove(id);

    /// <summary>
    /// Removes multiple nodes by ID.
    /// </summary>
    /// <param name="ids">The node IDs to remove.</param>
    public void RemoveNodes(IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            _nodes.Remove(id);
        }
    }

    /// <summary>
    /// Removes all nodes in a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public void RemoveNodesByFile(string filePath)
    {
        var nodesToRemove = _nodes.Values
            .Where(n => n.FilePath == filePath)
            .Select(n => n.Id)
            .ToList();

        foreach (var id in nodesToRemove)
        {
            _nodes.Remove(id);
        }
    }

    /// <summary>
    /// Removes an edge by ID.
    /// </summary>
    /// <param name="id">The edge ID to remove.</param>
    /// <returns>True if the edge was removed, false if it didn't exist.</returns>
    public bool RemoveEdge(string id)
    {
        var index = _edges.FindIndex(e => e.Id == id);
        if (index < 0)
        {
            return false;
        }

        _edges.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes all edges originating from a specific file.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    public void RemoveEdgesBySourceFile(string filePath)
    {
        _edges.RemoveAll(e => e.SourceFilePath == filePath);
    }

    /// <summary>
    /// Removes all edges that reference a node (both incoming and outgoing).
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    public void RemoveEdgesByNodeId(string nodeId)
    {
        _edges.RemoveAll(e => e.SourceId == nodeId || e.TargetId == nodeId);
    }

    /// <summary>
    /// Gets all nodes in a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>All nodes in the file.</returns>
    public IEnumerable<DeclarationNode> GetNodesByFile(string filePath) =>
        _nodes.Values.Where(n => n.FilePath == filePath);

    /// <summary>
    /// Gets all edges originating from a specific file.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <returns>All edges with the specified source file path.</returns>
    public IEnumerable<RelationshipEdge> GetEdgesBySourceFile(string filePath) =>
        _edges.Where(e => e.SourceFilePath == filePath);
}