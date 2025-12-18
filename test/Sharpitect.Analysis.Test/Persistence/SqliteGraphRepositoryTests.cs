using Microsoft.Data.Sqlite;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Test.Persistence;

[TestFixture]
public class SqliteGraphRepositoryTests
{
    private string _testDbPath = null!;
    private SqliteGraphRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"sharpitect_test_{Guid.NewGuid()}.db");
        _repository = new SqliteGraphRepository(_testDbPath);
        await _repository.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _repository.DisposeAsync();

        // Clear the connection pool to release all resources (required on Windows)
        SqliteConnection.ClearAllPools();

        // Give a brief moment for resources to be released
        await Task.Delay(50);

        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch (IOException)
            {
                // Ignore file deletion failures in tests
            }
        }
    }

    [Test]
    public async Task SaveNodeAsync_ShouldPersistNode()
    {
        var node = CreateTestNode("test-id", "TestClass", DeclarationKind.Class);

        await _repository.SaveNodeAsync(node);

        var retrieved = await _repository.GetNodeAsync("test-id");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("TestClass"));
        Assert.That(retrieved.Kind, Is.EqualTo(DeclarationKind.Class));
    }

    [Test]
    public async Task SaveNodesAsync_ShouldPersistMultipleNodes()
    {
        var nodes = new[]
        {
            CreateTestNode("id1", "Class1", DeclarationKind.Class),
            CreateTestNode("id2", "Class2", DeclarationKind.Class),
            CreateTestNode("id3", "Method1", DeclarationKind.Method)
        };

        await _repository.SaveNodesAsync(nodes);

        var count = await _repository.GetNodeCountAsync();
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task SaveEdgeAsync_ShouldPersistEdge()
    {
        var node1 = CreateTestNode("source", "Source", DeclarationKind.Class);
        var node2 = CreateTestNode("target", "Target", DeclarationKind.Method);
        await _repository.SaveNodesAsync(new[] { node1, node2 });

        var edge = CreateTestEdge("edge-1", "source", "target", RelationshipKind.Contains);
        await _repository.SaveEdgeAsync(edge);

        var count = await _repository.GetEdgeCountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetNodesByKindAsync_ShouldReturnFilteredNodes()
    {
        var nodes = new[]
        {
            CreateTestNode("c1", "Class1", DeclarationKind.Class),
            CreateTestNode("c2", "Class2", DeclarationKind.Class),
            CreateTestNode("m1", "Method1", DeclarationKind.Method)
        };
        await _repository.SaveNodesAsync(nodes);

        var classes = await _repository.GetNodesByKindAsync(DeclarationKind.Class);

        Assert.That(classes, Has.Count.EqualTo(2));
        Assert.That(classes.All(n => n.Kind == DeclarationKind.Class), Is.True);
    }

    [Test]
    public async Task GetNodesByFileAsync_ShouldReturnNodesInFile()
    {
        var nodes = new[]
        {
            CreateTestNode("n1", "Class1", DeclarationKind.Class, "file1.cs"),
            CreateTestNode("n2", "Class2", DeclarationKind.Class, "file1.cs"),
            CreateTestNode("n3", "Class3", DeclarationKind.Class, "file2.cs")
        };
        await _repository.SaveNodesAsync(nodes);

        var nodesInFile1 = await _repository.GetNodesByFileAsync("file1.cs");

        Assert.That(nodesInFile1, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetOutgoingEdgesAsync_ShouldReturnEdgesFromNode()
    {
        await SetupNodesAndEdges();

        var outgoing = await _repository.GetOutgoingEdgesAsync("class1");

        Assert.That(outgoing, Has.Count.EqualTo(2));
        Assert.That(outgoing.All(e => e.SourceId == "class1"), Is.True);
    }

    [Test]
    public async Task GetIncomingEdgesAsync_ShouldReturnEdgesToNode()
    {
        await SetupNodesAndEdges();

        var incoming = await _repository.GetIncomingEdgesAsync("method1");

        Assert.That(incoming, Has.Count.EqualTo(1));
        Assert.That(incoming.All(e => e.TargetId == "method1"), Is.True);
    }

    [Test]
    public async Task GetEdgesByKindAsync_ShouldReturnFilteredEdges()
    {
        await SetupNodesAndEdges();

        var containsEdges = await _repository.GetEdgesByKindAsync(RelationshipKind.Contains);

        Assert.That(containsEdges, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task LoadGraphAsync_ShouldReturnCompleteGraph()
    {
        await SetupNodesAndEdges();

        var graph = await _repository.LoadGraphAsync();

        Assert.That(graph.NodeCount, Is.EqualTo(3));
        Assert.That(graph.EdgeCount, Is.EqualTo(3));
    }

    [Test]
    public async Task ClearAsync_ShouldRemoveAllData()
    {
        await SetupNodesAndEdges();

        await _repository.ClearAsync();

        var nodeCount = await _repository.GetNodeCountAsync();
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(nodeCount, Is.EqualTo(0));
        Assert.That(edgeCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveNodeAsync_ShouldPreserveC4Annotations()
    {
        var node = new DeclarationNode
        {
            Id = "test-id",
            Name = "TestComponent",
            FullyQualifiedName = "Test.TestComponent",
            Kind = DeclarationKind.Class,
            FilePath = "test.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = 10,
            EndColumn = 1,
            C4Level = C4Level.Component,
            C4Description = "A test component"
        };

        await _repository.SaveNodeAsync(node);

        var retrieved = await _repository.GetNodeAsync("test-id");
        Assert.That(retrieved!.C4Level, Is.EqualTo(C4Level.Component));
        Assert.That(retrieved.C4Description, Is.EqualTo("A test component"));
    }

    private async Task SetupNodesAndEdges()
    {
        var nodes = new[]
        {
            CreateTestNode("class1", "Class1", DeclarationKind.Class),
            CreateTestNode("method1", "Method1", DeclarationKind.Method),
            CreateTestNode("method2", "Method2", DeclarationKind.Method)
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "class1", "method1", RelationshipKind.Contains),
            CreateTestEdge("e2", "class1", "method2", RelationshipKind.Contains),
            CreateTestEdge("e3", "method1", "method2", RelationshipKind.Calls)
        };
        await _repository.SaveEdgesAsync(edges);
    }

    private static DeclarationNode CreateTestNode(string id, string name, DeclarationKind kind, string filePath = "test.cs")
    {
        return new DeclarationNode
        {
            Id = id,
            Name = name,
            FullyQualifiedName = $"Test.{name}",
            Kind = kind,
            FilePath = filePath,
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
