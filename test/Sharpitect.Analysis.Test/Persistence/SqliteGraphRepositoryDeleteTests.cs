using Microsoft.Data.Sqlite;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;

namespace Sharpitect.Analysis.Test.Persistence;

[TestFixture]
public class SqliteGraphRepositoryDeleteTests
{
    private string _testDbPath = null!;
    private SqliteGraphRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"sharpitect_delete_test_{Guid.NewGuid()}.db");
        _repository = new SqliteGraphRepository(_testDbPath);
        await _repository.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _repository.DisposeAsync();
        SqliteConnection.ClearAllPools();
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

    #region DeleteNodeAsync Tests

    [Test]
    public async Task DeleteNodeAsync_ShouldRemoveNode()
    {
        var node = CreateTestNode("node-1", "TestClass", DeclarationKind.Class);
        await _repository.SaveNodeAsync(node);

        await _repository.DeleteNodeAsync("node-1");

        var retrieved = await _repository.GetNodeAsync("node-1");
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public async Task DeleteNodeAsync_NonExistentNode_ShouldNotThrow()
    {
        Assert.DoesNotThrowAsync(async () => await _repository.DeleteNodeAsync("non-existent"));
    }

    [Test]
    public async Task DeleteNodeAsync_ShouldCascadeDeleteOutgoingEdges()
    {
        // Setup: source -> target with Contains edge
        var source = CreateTestNode("source", "Source", DeclarationKind.Class);
        var target = CreateTestNode("target", "Target", DeclarationKind.Method);
        await _repository.SaveNodesAsync([source, target]);

        var edge = CreateTestEdge("edge-1", "source", "target", RelationshipKind.Contains);
        await _repository.SaveEdgeAsync(edge);

        // Act: Delete the source node
        await _repository.DeleteNodeAsync("source");

        // Assert: Edge should be deleted due to CASCADE
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(edgeCount, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteNodeAsync_ShouldCascadeDeleteIncomingEdges()
    {
        // Setup: source -> target with Contains edge
        var source = CreateTestNode("source", "Source", DeclarationKind.Class);
        var target = CreateTestNode("target", "Target", DeclarationKind.Method);
        await _repository.SaveNodesAsync([source, target]);

        var edge = CreateTestEdge("edge-1", "source", "target", RelationshipKind.Contains);
        await _repository.SaveEdgeAsync(edge);

        // Act: Delete the target node
        await _repository.DeleteNodeAsync("target");

        // Assert: Edge should be deleted due to CASCADE
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(edgeCount, Is.EqualTo(0));
    }

    #endregion

    #region DeleteNodesAsync Tests

    [Test]
    public async Task DeleteNodesAsync_ShouldRemoveMultipleNodes()
    {
        var nodes = new[]
        {
            CreateTestNode("node-1", "Class1", DeclarationKind.Class),
            CreateTestNode("node-2", "Class2", DeclarationKind.Class),
            CreateTestNode("node-3", "Class3", DeclarationKind.Class)
        };
        await _repository.SaveNodesAsync(nodes);

        await _repository.DeleteNodesAsync(["node-1", "node-2"]);

        var nodeCount = await _repository.GetNodeCountAsync();
        Assert.That(nodeCount, Is.EqualTo(1));

        var remaining = await _repository.GetNodeAsync("node-3");
        Assert.That(remaining, Is.Not.Null);
    }

    [Test]
    public async Task DeleteNodesAsync_WithEmptyCollection_ShouldNotThrow()
    {
        Assert.DoesNotThrowAsync(async () => await _repository.DeleteNodesAsync([]));
    }

    [Test]
    public async Task DeleteNodesAsync_ShouldCascadeDeleteEdges()
    {
        // Setup nodes with interconnecting edges
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class),
            CreateTestNode("b", "B", DeclarationKind.Class),
            CreateTestNode("c", "C", DeclarationKind.Class)
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "a", "b", RelationshipKind.Calls),
            CreateTestEdge("e2", "b", "c", RelationshipKind.Calls),
            CreateTestEdge("e3", "a", "c", RelationshipKind.Calls)
        };
        await _repository.SaveEdgesAsync(edges);

        // Act: Delete nodes a and b
        await _repository.DeleteNodesAsync(["a", "b"]);

        // Assert: Only node c remains, no edges remain (all involved a or b)
        var nodeCount = await _repository.GetNodeCountAsync();
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(nodeCount, Is.EqualTo(1));
            Assert.That(edgeCount, Is.EqualTo(0));
        });
    }

    #endregion

    #region DeleteNodesByFileAsync Tests

    [Test]
    public async Task DeleteNodesByFileAsync_ShouldRemoveAllNodesInFile()
    {
        var nodes = new[]
        {
            CreateTestNode("n1", "Class1", DeclarationKind.Class, "file1.cs"),
            CreateTestNode("n2", "Method1", DeclarationKind.Method, "file1.cs"),
            CreateTestNode("n3", "Class2", DeclarationKind.Class, "file2.cs")
        };
        await _repository.SaveNodesAsync(nodes);

        await _repository.DeleteNodesByFileAsync("file1.cs");

        var nodeCount = await _repository.GetNodeCountAsync();
        Assert.That(nodeCount, Is.EqualTo(1));

        var remaining = await _repository.GetNodeAsync("n3");
        Assert.That(remaining, Is.Not.Null);
    }

    [Test]
    public async Task DeleteNodesByFileAsync_NonExistentFile_ShouldNotThrow()
    {
        Assert.DoesNotThrowAsync(async () => await _repository.DeleteNodesByFileAsync("nonexistent.cs"));
    }

    [Test]
    public async Task DeleteNodesByFileAsync_ShouldCascadeDeleteEdges()
    {
        // Nodes in file1.cs connected to node in file2.cs
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class, "file1.cs"),
            CreateTestNode("b", "B", DeclarationKind.Method, "file1.cs"),
            CreateTestNode("c", "C", DeclarationKind.Class, "file2.cs")
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "a", "b", RelationshipKind.Contains),
            CreateTestEdge("e2", "a", "c", RelationshipKind.Calls),
            CreateTestEdge("e3", "c", "a", RelationshipKind.References)
        };
        await _repository.SaveEdgesAsync(edges);

        await _repository.DeleteNodesByFileAsync("file1.cs");

        // All edges should be gone because they involve nodes from file1.cs
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(edgeCount, Is.EqualTo(0));
    }

    #endregion

    #region DeleteEdgesBySourceFileAsync Tests

    [Test]
    public async Task DeleteEdgesBySourceFileAsync_ShouldRemoveEdgesFromFile()
    {
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class),
            CreateTestNode("b", "B", DeclarationKind.Class)
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "a", "b", RelationshipKind.Calls, "file1.cs"),
            CreateTestEdge("e2", "a", "b", RelationshipKind.References, "file1.cs"),
            CreateTestEdge("e3", "b", "a", RelationshipKind.Calls, "file2.cs")
        };
        await _repository.SaveEdgesAsync(edges);

        await _repository.DeleteEdgesBySourceFileAsync("file1.cs");

        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(edgeCount, Is.EqualTo(1));

        var remaining = await _repository.GetOutgoingEdgesAsync("b");
        Assert.That(remaining, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DeleteEdgesBySourceFileAsync_NonExistentFile_ShouldNotThrow()
    {
        Assert.DoesNotThrowAsync(async () => await _repository.DeleteEdgesBySourceFileAsync("nonexistent.cs"));
    }

    [Test]
    public async Task DeleteEdgesBySourceFileAsync_ShouldNotAffectNodesOrOtherEdges()
    {
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class, "file1.cs"),
            CreateTestNode("b", "B", DeclarationKind.Class, "file2.cs")
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "a", "b", RelationshipKind.Calls, "file1.cs"),
            CreateTestEdge("e2", "b", "a", RelationshipKind.Calls, "file2.cs")
        };
        await _repository.SaveEdgesAsync(edges);

        await _repository.DeleteEdgesBySourceFileAsync("file1.cs");

        // Nodes should remain unchanged
        var nodeCount = await _repository.GetNodeCountAsync();
        Assert.That(nodeCount, Is.EqualTo(2));

        // Only edge from file2.cs should remain
        var edgeCount = await _repository.GetEdgeCountAsync();
        Assert.That(edgeCount, Is.EqualTo(1));
    }

    #endregion

    #region GetEdgesBySourceFileAsync Tests

    [Test]
    public async Task GetEdgesBySourceFileAsync_ShouldReturnMatchingEdges()
    {
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class),
            CreateTestNode("b", "B", DeclarationKind.Class),
            CreateTestNode("c", "C", DeclarationKind.Class)
        };
        await _repository.SaveNodesAsync(nodes);

        var edges = new[]
        {
            CreateTestEdge("e1", "a", "b", RelationshipKind.Calls, "file1.cs"),
            CreateTestEdge("e2", "a", "c", RelationshipKind.References, "file1.cs"),
            CreateTestEdge("e3", "b", "c", RelationshipKind.Calls, "file2.cs")
        };
        await _repository.SaveEdgesAsync(edges);

        var edgesFromFile1 = await _repository.GetEdgesBySourceFileAsync("file1.cs");

        Assert.That(edgesFromFile1, Has.Count.EqualTo(2));
        Assert.That(edgesFromFile1.All(e => e.SourceFilePath == "file1.cs"), Is.True);
    }

    [Test]
    public async Task GetEdgesBySourceFileAsync_NonExistentFile_ShouldReturnEmpty()
    {
        var edges = await _repository.GetEdgesBySourceFileAsync("nonexistent.cs");
        Assert.That(edges, Is.Empty);
    }

    [Test]
    public async Task GetEdgesBySourceFileAsync_ShouldPreserveAllEdgeProperties()
    {
        var nodes = new[]
        {
            CreateTestNode("a", "A", DeclarationKind.Class),
            CreateTestNode("b", "B", DeclarationKind.Class)
        };
        await _repository.SaveNodesAsync(nodes);

        var edge = new RelationshipEdge
        {
            Id = "edge-full",
            SourceId = "a",
            TargetId = "b",
            Kind = RelationshipKind.Calls,
            SourceFilePath = "test.cs",
            SourceLine = 42,
            Metadata = "{\"key\": \"value\"}"
        };
        await _repository.SaveEdgeAsync(edge);

        var edges = await _repository.GetEdgesBySourceFileAsync("test.cs");

        Assert.That(edges, Has.Count.EqualTo(1));
        var retrieved = edges[0];
        Assert.Multiple(() =>
        {
            Assert.That(retrieved.Id, Is.EqualTo("edge-full"));
            Assert.That(retrieved.SourceId, Is.EqualTo("a"));
            Assert.That(retrieved.TargetId, Is.EqualTo("b"));
            Assert.That(retrieved.Kind, Is.EqualTo(RelationshipKind.Calls));
            Assert.That(retrieved.SourceFilePath, Is.EqualTo("test.cs"));
            Assert.That(retrieved.SourceLine, Is.EqualTo(42));
            Assert.That(retrieved.Metadata, Is.EqualTo("{\"key\": \"value\"}"));
        });
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(string id, string name, DeclarationKind kind,
        string filePath = "test.cs")
    {
        return new DeclarationNode
        {
            Id = id,
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
