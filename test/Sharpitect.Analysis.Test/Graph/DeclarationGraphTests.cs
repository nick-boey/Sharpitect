using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Graph;

[TestFixture]
public class DeclarationGraphTests
{
    [Test]
    public void AddNode_ShouldAddNodeToGraph()
    {
        var graph = new DeclarationGraph();
        var node = CreateTestNode("TestClass", DeclarationKind.Class);

        graph.AddNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.GetNode("TestClass"), Is.EqualTo(node));
        });
    }

    [Test]
    public void AddNode_ShouldReplaceExistingNodeWithSameId()
    {
        var graph = new DeclarationGraph();
        // Both nodes use the same Id (FQN) to test replacement
        var node1 = CreateTestNodeWithId("MyNamespace.MyClass", "OldName", DeclarationKind.Class);
        var node2 = CreateTestNodeWithId("MyNamespace.MyClass", "NewName", DeclarationKind.Class);

        graph.AddNode(node1);
        graph.AddNode(node2);

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.GetNode("MyNamespace.MyClass")?.Name, Is.EqualTo("NewName"));
        });
    }

    [Test]
    public void AddNodes_ShouldAddMultipleNodes()
    {
        var graph = new DeclarationGraph();
        var nodes = new[]
        {
            CreateTestNode("Class1", DeclarationKind.Class),
            CreateTestNode("Class2", DeclarationKind.Class),
            CreateTestNode("Method1", DeclarationKind.Method)
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
        graph.AddNode(CreateTestNode("Class1", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("Class2", DeclarationKind.Class));
        graph.AddNode(CreateTestNode("Method1", DeclarationKind.Method));
        graph.AddNode(CreateTestNode("Prop1", DeclarationKind.Property));

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
        graph.AddNode(CreateTestNode("Test", DeclarationKind.Class));

        Assert.Multiple(() =>
        {
            Assert.That(graph.ContainsNode("Test"), Is.True);
            Assert.That(graph.ContainsNode("non-existent"), Is.False);
        });
    }

    [Test]
    public void Clear_ShouldRemoveAllNodesAndEdges()
    {
        var graph = new DeclarationGraph();
        graph.AddNode(CreateTestNode("Test", DeclarationKind.Class));
        graph.AddEdge(CreateTestEdge("e1", "a", "b", RelationshipKind.Calls));

        graph.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(graph.NodeCount, Is.EqualTo(0));
            Assert.That(graph.EdgeCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void Nodes_ShouldReturnEmptyDictionaryWhenNoNodes()
    {
        var graph = new DeclarationGraph();

        Assert.That(graph.Nodes, Is.Empty);
    }

    [Test]
    public void Nodes_ShouldReturnAllAddedNodes()
    {
        var graph = new DeclarationGraph();
        var node1 = CreateTestNode("Class1", DeclarationKind.Class);
        var node2 = CreateTestNode("Interface1", DeclarationKind.Interface);
        var node3 = CreateTestNode("Method1", DeclarationKind.Method);
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);

        var nodes = graph.Nodes;

        Assert.That(nodes, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(nodes.ContainsKey("Class1"), Is.True);
            Assert.That(nodes.ContainsKey("Interface1"), Is.True);
            Assert.That(nodes.ContainsKey("Method1"), Is.True);
            Assert.That(nodes["Class1"], Is.EqualTo(node1));
            Assert.That(nodes["Interface1"], Is.EqualTo(node2));
            Assert.That(nodes["Method1"], Is.EqualTo(node3));
        });
    }

    [Test]
    public void Nodes_ValuesContainsAllNodes()
    {
        var graph = new DeclarationGraph();
        var node1 = CreateTestNode("Class1", DeclarationKind.Class);
        var node2 = CreateTestNode("Class2", DeclarationKind.Class);
        graph.AddNode(node1);
        graph.AddNode(node2);

        var allNodes = graph.Nodes.Values.ToList();

        Assert.That(allNodes, Has.Count.EqualTo(2));
        Assert.That(allNodes, Does.Contain(node1));
        Assert.That(allNodes, Does.Contain(node2));
    }

    private static DeclarationNode CreateTestNode(string name, DeclarationKind kind)
    {
        return new DeclarationNode
        {
            Id = name,  // Id is now the fully qualified name
            Name = name,
            Kind = kind,
            FilePath = "test.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1
        };
    }

    private static DeclarationNode CreateTestNodeWithId(string id, string name, DeclarationKind kind)
    {
        return new DeclarationNode
        {
            Id = id,
            Name = name,
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