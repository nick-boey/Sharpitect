using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Graph;

[TestFixture]
public class DeclarationGraphTests
{
    [Test]
    public void AddNode_ShouldAddNodeToGraph()
    {
        var graph = new DeclarationGraph();
        var node = CreateTestNode("test-id", "TestClass", DeclarationKind.Class);

        graph.AddNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.GetNode("test-id"), Is.EqualTo(node));
        });
    }

    [Test]
    public void AddNode_ShouldReplaceExistingNodeWithSameId()
    {
        var graph = new DeclarationGraph();
        var node1 = CreateTestNode("test-id", "OldName", DeclarationKind.Class);
        var node2 = CreateTestNode("test-id", "NewName", DeclarationKind.Class);

        graph.AddNode(node1);
        graph.AddNode(node2);

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.GetNode("test-id")?.Name, Is.EqualTo("NewName"));
        });
    }

    [Test]
    public void AddNodes_ShouldAddMultipleNodes()
    {
        var graph = new DeclarationGraph();
        var nodes = new[]
        {
            CreateTestNode("id1", "Class1", DeclarationKind.Class),
            CreateTestNode("id2", "Class2", DeclarationKind.Class),
            CreateTestNode("id3", "Method1", DeclarationKind.Method)
        };

        graph.AddNodes(nodes);

        Assert.That(graph.NodeCount, Is.EqualTo(3));
    }

    [Test]
    public void AddEdge_ShouldAddEdgeToGraph()
    {
        var graph = new DeclarationGraph();
        var edge = CreateTestEdge("edge-1", "source", "target", RelationshipKind.Contains);

        graph.AddEdge(edge);

        Assert.That(graph.EdgeCount, Is.EqualTo(1));
    }

    [Test]
    public void GetNodesByKind_ShouldReturnFilteredNodes()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("c1", "Class1", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("c2", "Class2", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("m1", "Method1", DeclarationKind.Method));
        graph.AddNode(CreateTestNode("p1", "Prop1", DeclarationKind.Property));

        var classes = graph.GetNodesByKind(DeclarationKind.Class).ToList();

        Assert.That(classes, Has.Count.EqualTo(2));
        Assert.That(classes.All(n => n.Kind == DeclarationKind.Class), Is.True);
    }

    [Test]
    public void GetEdgesByKind_ShouldReturnFilteredEdges()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("e1", "a", "b", RelationshipKind.Contains));
        graph.AddEdge(CreateTestEdge("e2", "b", "c", RelationshipKind.Contains));
        graph.AddEdge(CreateTestEdge("e3", "a", "c", RelationshipKind.Calls));

        var containsEdges = graph.GetEdgesByKind(RelationshipKind.Contains).ToList();

        Assert.That(containsEdges, Has.Count.EqualTo(2));
        Assert.That(containsEdges.All(e => e.Kind == RelationshipKind.Contains), Is.True);
    }

    [Test]
    public void GetOutgoingEdges_ShouldReturnEdgesFromNode()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("e1", "source", "target1", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("e2", "source", "target2", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("e3", "other", "target1", RelationshipKind.Calls));

        var outgoing = graph.GetOutgoingEdges("source").ToList();

        Assert.That(outgoing, Has.Count.EqualTo(2));
        Assert.That(outgoing.All(e => e.SourceId == "source"), Is.True);
    }

    [Test]
    public void GetIncomingEdges_ShouldReturnEdgesToNode()
    {
        var graph = new DeclarationGraph();
        graph.AddEdge(CreateTestEdge("e1", "source1", "target", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("e2", "source2", "target", RelationshipKind.Calls));
        graph.AddEdge(CreateTestEdge("e3", "source1", "other", RelationshipKind.Calls));

        var incoming = graph.GetIncomingEdges("target").ToList();

        Assert.That(incoming, Has.Count.EqualTo(2));
        Assert.That(incoming.All(e => e.TargetId == "target"), Is.True);
    }

    [Test]
    public void ContainsNode_ShouldReturnTrueForExistingNode()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("test-id", "Test", DeclarationKind.Class));

        Assert.Multiple(() =>
        {
            Assert.That(graph.ContainsNode("test-id"), Is.True);
            Assert.That(graph.ContainsNode("non-existent"), Is.False);
        });
    }

    [Test]
    public void Clear_ShouldRemoveAllNodesAndEdges()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("n1", "Test", DeclarationKind.Class));
        graph.AddEdge(CreateTestEdge("e1", "a", "b", RelationshipKind.Calls));

        graph.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(0));
            Assert.That(graph.EdgeCount, Is.EqualTo(0));
        });
    }

    private static DeclarationNode CreateTestNode(string id, string name, DeclarationKind kind)
    {
        return new DeclarationNode
        {
            Id = id,
            Name = name,
            FullyQualifiedName = $"Test.{name}",
            Kind = kind,
            FilePath = "test.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1
        };
    }

    private static RelationshipEdge CreateTestEdge(string id, string sourceId, string targetId, RelationshipKind kind)
    {
        return new RelationshipEdge
        {
            Id = id,
            SourceId = sourceId,
            TargetId = targetId,
            Kind = kind
        };
    }
}
