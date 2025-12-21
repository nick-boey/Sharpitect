using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Graph;

[TestFixture]
public class DeclarationGraphDeleteTests
{
    #region RemoveNode Tests

    [Test]
    public void RemoveNode_ShouldRemoveExistingNode()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("node2", DeclarationKind.Class));

        var result = graph.RemoveNode("node1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.ContainsNode("node1"), Is.False);
            Assert.That(graph.ContainsNode("node2"), Is.True);
        });
    }

    [Test]
    public void RemoveNode_NonExistentNode_ShouldReturnFalse()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));

        var result = graph.RemoveNode("nonexistent");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(graph.NodeCount, Is.EqualTo(1));
        });
    }

    #endregion

    #region RemoveNodes Tests

    [Test]
    public void RemoveNodes_ShouldRemoveMultipleNodes()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("node2", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("node3", DeclarationKind.Class));

        graph.RemoveNodes(["node1", "node2"]);

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.ContainsNode("node3"), Is.True);
        });
    }

    [Test]
    public void RemoveNodes_WithEmptyCollection_ShouldDoNothing()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));

        graph.RemoveNodes([]);

        Assert.That(graph.NodeCount, Is.EqualTo(1));
    }

    #endregion

    #region RemoveNodesByFile Tests

    [Test]
    public void RemoveNodesByFile_ShouldRemoveAllNodesInFile()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class, "file1.cs"));
        graph.AddNode(CreateTestNode("node2", DeclarationKind.Method, "file1.cs"));
        graph.AddNode(CreateTestNode("node3", DeclarationKind.Class, "file2.cs"));

        graph.RemoveNodesByFile("file1.cs");

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.ContainsNode("node3"), Is.True);
        });
    }

    [Test]
    public void RemoveNodesByFile_NonExistentFile_ShouldDoNothing()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));

        graph.RemoveNodesByFile("nonexistent.cs");

        Assert.That(graph.NodeCount, Is.EqualTo(1));
    }

    #endregion

    #region RemoveEdge Tests

    [Test]
    public void RemoveEdge_ShouldRemoveExistingEdge()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge2", "b", "c", RelationshipKind.Calls));

        var result = graph.RemoveEdge("edge1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RemoveEdge_NonExistentEdge_ShouldReturnFalse()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls));

        var result = graph.RemoveEdge("nonexistent");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
        });
    }

    #endregion

    #region RemoveEdgesBySourceFile Tests

    [Test]
    public void RemoveEdgesBySourceFile_ShouldRemoveEdgesFromFile()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls, "file1.cs"));
        graph.AddEdge(CreateTestEdge("edge2", "a", "c", RelationshipKind.Calls, "file1.cs"));
        graph.AddEdge(CreateTestEdge("edge3", "b", "c", RelationshipKind.Calls, "file2.cs"));

        graph.RemoveEdgesBySourceFile("file1.cs");

        Assert.Multiple(() =>
        {
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.Edges[0].Id, Is.EqualTo("edge3"));
        });
    }

    [Test]
    public void RemoveEdgesBySourceFile_NonExistentFile_ShouldDoNothing()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls, "file1.cs"));

        graph.RemoveEdgesBySourceFile("nonexistent.cs");

        Assert.That(graph.EdgeCount, Is.EqualTo(1));
    }

    #endregion

    #region RemoveEdgesByNodeId Tests

    [Test]
    public void RemoveEdgesByNodeId_ShouldRemoveIncomingEdges()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "other", "target", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge2", "another", "target", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge3", "other", "different", RelationshipKind.Calls));

        graph.RemoveEdgesByNodeId("target");

        Assert.Multiple(() =>
        {
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.Edges[0].Id, Is.EqualTo("edge3"));
        });
    }

    [Test]
    public void RemoveEdgesByNodeId_ShouldRemoveOutgoingEdges()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "source", "target1", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge2", "source", "target2", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge3", "other", "target1", RelationshipKind.Calls));

        graph.RemoveEdgesByNodeId("source");

        Assert.Multiple(() =>
        {
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.Edges[0].Id, Is.EqualTo("edge3"));
        });
    }

    [Test]
    public void RemoveEdgesByNodeId_ShouldRemoveBothIncomingAndOutgoing()
    {
        var graph = new DeclarationGraph();
        // Edges involving 'node'
        graph.AddEdge(CreateTestEdge("edge1", "node", "target", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("edge2", "source", "node", RelationshipKind.Calls));
        // Edges not involving 'node'
        graph.AddEdge(CreateTestEdge("edge3", "other", "another", RelationshipKind.Calls));

        graph.RemoveEdgesByNodeId("node");

        Assert.Multiple(() =>
        {
            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.Edges[0].Id, Is.EqualTo("edge3"));
        });
    }

    [Test]
    public void RemoveEdgesByNodeId_NonExistentNode_ShouldDoNothing()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls));

        graph.RemoveEdgesByNodeId("nonexistent");

        Assert.That(graph.EdgeCount, Is.EqualTo(1));
    }

    #endregion

    #region GetNodesByFile Tests

    [Test]
    public void GetNodesByFile_ShouldReturnNodesInFile()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class, "file1.cs"));
        graph.AddNode(CreateTestNode("node2", DeclarationKind.Method, "file1.cs"));
        graph.AddNode(CreateTestNode("node3", DeclarationKind.Class, "file2.cs"));

        var nodes = graph.GetNodesByFile("file1.cs").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Count.EqualTo(2));
            Assert.That(nodes.All(n => n.FilePath == "file1.cs"), Is.True);
        });
    }

    [Test]
    public void GetNodesByFile_NonExistentFile_ShouldReturnEmpty()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("node1", DeclarationKind.Class));

        var nodes = graph.GetNodesByFile("nonexistent.cs").ToList();

        Assert.That(nodes, Is.Empty);
    }

    #endregion

    #region GetEdgesBySourceFile Tests

    [Test]
    public void GetEdgesBySourceFile_ShouldReturnEdgesFromFile()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls, "file1.cs"));
        graph.AddEdge(CreateTestEdge("edge2", "a", "c", RelationshipKind.Calls, "file1.cs"));
        graph.AddEdge(CreateTestEdge("edge3", "b", "c", RelationshipKind.Calls, "file2.cs"));

        var edges = graph.GetEdgesBySourceFile("file1.cs").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(edges, Has.Count.EqualTo(2));
            Assert.That(edges.All(e => e.SourceFilePath == "file1.cs"), Is.True);
        });
    }

    [Test]
    public void GetEdgesBySourceFile_NonExistentFile_ShouldReturnEmpty()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("edge1", "a", "b", RelationshipKind.Calls, "file1.cs"));

        var edges = graph.GetEdgesBySourceFile("nonexistent.cs").ToList();

        Assert.That(edges, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(string name, DeclarationKind kind,
        string filePath = "test.cs")
    {
        return new DeclarationNode
        {
            Id = name,
            Name = name,
            Kind = kind,
            FilePath = filePath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1
        };
    }

    private static RelationshipEdge CreateTestEdge(string id, string sourceId, string targetId,
        RelationshipKind kind, string? sourceFilePath = null)
    {
        return new RelationshipEdge
        {
            Id = id,
            SourceId = sourceId,
            TargetId = targetId,
            Kind = kind,
            SourceFilePath = sourceFilePath
        };
    }

    #endregion
}
