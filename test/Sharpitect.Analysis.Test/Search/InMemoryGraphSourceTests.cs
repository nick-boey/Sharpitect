using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;

namespace Sharpitect.Analysis.Test.Search;

[TestFixture]
public class InMemoryGraphSourceTests
{
    private DeclarationGraph _graph = null!;
    private InMemoryGraphSource _source = null!;

    [SetUp]
    public void SetUp()
    {
        _graph = new DeclarationGraph();
        _source = new InMemoryGraphSource(_graph);
    }

    #region GetAllNodesAsync Tests

    [Test]
    public async Task GetAllNodesAsync_ReturnsAllNodes()
    {
        _graph.AddNode(CreateTestNode("1", "Class1", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "Class2", DeclarationKind.Class));

        var nodes = await _source.GetAllNodesAsync();

        Assert.That(nodes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllNodesAsync_EmptyGraph_ReturnsEmpty()
    {
        var nodes = await _source.GetAllNodesAsync();

        Assert.That(nodes, Is.Empty);
    }

    #endregion

    #region GetNodesByKindsAsync Tests

    [Test]
    public async Task GetNodesByKindsAsync_SingleKind_ReturnsMatchingNodes()
    {
        _graph.AddNode(CreateTestNode("1", "Class1", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "Method1", DeclarationKind.Method));

        var nodes = await _source.GetNodesByKindsAsync([DeclarationKind.Class]);

        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Kind, Is.EqualTo(DeclarationKind.Class));
    }

    [Test]
    public async Task GetNodesByKindsAsync_MultipleKinds_ReturnsAllMatchingNodes()
    {
        _graph.AddNode(CreateTestNode("1", "Class1", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "Interface1", DeclarationKind.Interface));
        _graph.AddNode(CreateTestNode("3", "Method1", DeclarationKind.Method));

        var nodes = await _source.GetNodesByKindsAsync(
            [DeclarationKind.Class, DeclarationKind.Interface]);

        Assert.That(nodes, Has.Count.EqualTo(2));
        Assert.That(nodes.All(n => n.Kind is DeclarationKind.Class or DeclarationKind.Interface), Is.True);
    }

    [Test]
    public async Task GetNodesByKindsAsync_NoMatchingKinds_ReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "Class1", DeclarationKind.Class));

        var nodes = await _source.GetNodesByKindsAsync([DeclarationKind.Method]);

        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public async Task GetNodesByKindsAsync_EmptyKinds_ReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "Class1", DeclarationKind.Class));

        var nodes = await _source.GetNodesByKindsAsync([]);

        Assert.That(nodes, Is.Empty);
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_NullGraph_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InMemoryGraphSource(null!));
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(string id, string name, DeclarationKind kind)
    {
        return new DeclarationNode
        {
            Id = $"Test.{name}",
            Name = name,
            Kind = kind,
            FilePath = "test.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1
        };
    }

    #endregion
}