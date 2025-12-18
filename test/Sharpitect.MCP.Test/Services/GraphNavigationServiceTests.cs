using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;
using Sharpitect.MCP.Services;
using NSubstitute;

namespace Sharpitect.MCP.Test.Services;

[TestFixture]
public class GraphNavigationServiceTests
{
    private IGraphRepository _repository = null!;
    private GraphNavigationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IGraphRepository>();
        _service = new GraphNavigationService(_repository);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_repository is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphNavigationService(null!));
    }

    #endregion

    #region GetNode Tests

    [Test]
    public async Task GetNodeAsync_WithValidId_ReturnsNodeDetail()
    {
        var node = CreateTestNode("test-id", "TestClass", DeclarationKind.Class, "Namespace.TestClass");
        _repository.GetNodeAsync("test-id").Returns(node);

        var result = await _service.GetNodeAsync("test-id");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("test-id"));
        Assert.That(result.Name, Is.EqualTo("TestClass"));
        Assert.That(result.Kind, Is.EqualTo("Class"));
    }

    [Test]
    public async Task GetNodeAsync_WithNonExistentId_ReturnsNull()
    {
        _repository.GetNodeAsync("missing").Returns((DeclarationNode?)null);

        var result = await _service.GetNodeAsync("missing");

        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetChildren Tests

    [Test]
    public async Task GetChildrenAsync_ReturnsContainedNodes()
    {
        var parentNode = CreateTestNode("parent-id", "Parent", DeclarationKind.Class, "Parent");
        var childNode1 = CreateTestNode("child-1", "Method1", DeclarationKind.Method, "Parent.Method1");
        var childNode2 = CreateTestNode("child-2", "Method2", DeclarationKind.Method, "Parent.Method2");

        _repository.GetNodeAsync("parent-id").Returns(parentNode);
        _repository.GetOutgoingEdgesAsync("parent-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("parent-id", "child-1", RelationshipKind.Contains),
            CreateEdge("parent-id", "child-2", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("child-1").Returns(childNode1);
        _repository.GetNodeAsync("child-2").Returns(childNode2);

        var result = await _service.GetChildrenAsync("parent-id");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ParentId, Is.EqualTo("parent-id"));
        Assert.That(result.Children, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetChildrenAsync_WithKindFilter_ReturnsOnlyMatchingKind()
    {
        var parentNode = CreateTestNode("parent-id", "Parent", DeclarationKind.Class, "Parent");
        var methodNode = CreateTestNode("method-id", "Method1", DeclarationKind.Method, "Parent.Method1");
        var propertyNode = CreateTestNode("prop-id", "Prop1", DeclarationKind.Property, "Parent.Prop1");

        _repository.GetNodeAsync("parent-id").Returns(parentNode);
        _repository.GetOutgoingEdgesAsync("parent-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("parent-id", "method-id", RelationshipKind.Contains),
            CreateEdge("parent-id", "prop-id", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("method-id").Returns(methodNode);
        _repository.GetNodeAsync("prop-id").Returns(propertyNode);

        var result = await _service.GetChildrenAsync("parent-id", kindFilter: DeclarationKind.Method);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Children, Has.Count.EqualTo(1));
        Assert.That(result.Children[0].Kind, Is.EqualTo("Method"));
    }

    [Test]
    public async Task GetChildrenAsync_WithNonExistentParent_ReturnsNull()
    {
        _repository.GetNodeAsync("missing").Returns((DeclarationNode?)null);

        var result = await _service.GetChildrenAsync("missing");

        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetAncestors Tests

    [Test]
    public async Task GetAncestorsAsync_ReturnsContainmentHierarchy()
    {
        var methodNode = CreateTestNode("method-id", "Method", DeclarationKind.Method, "NS.Class.Method");
        var classNode = CreateTestNode("class-id", "Class", DeclarationKind.Class, "NS.Class");
        var nsNode = CreateTestNode("ns-id", "NS", DeclarationKind.Namespace, "NS");

        _repository.GetNodeAsync("method-id").Returns(methodNode);
        _repository.GetIncomingEdgesAsync("method-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("class-id", "method-id", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("class-id").Returns(classNode);
        _repository.GetIncomingEdgesAsync("class-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("ns-id", "class-id", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("ns-id").Returns(nsNode);
        _repository.GetIncomingEdgesAsync("ns-id").Returns(new List<RelationshipEdge>());

        var result = await _service.GetAncestorsAsync("method-id");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Ancestors, Has.Count.EqualTo(2));
        Assert.That(result.Ancestors[0].Name, Is.EqualTo("NS"));
        Assert.That(result.Ancestors[1].Name, Is.EqualTo("Class"));
    }

    #endregion

    #region GetRelationships Tests

    [Test]
    public async Task GetRelationshipsAsync_ReturnsOutgoingAndIncoming()
    {
        var classNode = CreateTestNode("class-id", "MyClass", DeclarationKind.Class, "MyClass");
        var interfaceNode = CreateTestNode("interface-id", "IService", DeclarationKind.Interface, "IService");
        var testNode = CreateTestNode("test-id", "MyClassTests", DeclarationKind.Class, "MyClassTests");

        _repository.GetNodeAsync("class-id").Returns(classNode);
        _repository.GetOutgoingEdgesAsync("class-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("class-id", "interface-id", RelationshipKind.Implements)
        });
        _repository.GetIncomingEdgesAsync("class-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("test-id", "class-id", RelationshipKind.References)
        });
        _repository.GetNodeAsync("interface-id").Returns(interfaceNode);
        _repository.GetNodeAsync("test-id").Returns(testNode);

        var result = await _service.GetRelationshipsAsync("class-id");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Outgoing, Has.Count.EqualTo(1));
        Assert.That(result.Incoming, Has.Count.EqualTo(1));
        Assert.That(result.Outgoing[0].Kind, Is.EqualTo("Implements"));
        Assert.That(result.Incoming[0].Kind, Is.EqualTo("References"));
    }

    [Test]
    public async Task GetRelationshipsAsync_WithOutgoingDirection_ReturnsOnlyOutgoing()
    {
        var classNode = CreateTestNode("class-id", "MyClass", DeclarationKind.Class, "MyClass");
        var interfaceNode = CreateTestNode("interface-id", "IService", DeclarationKind.Interface, "IService");

        _repository.GetNodeAsync("class-id").Returns(classNode);
        _repository.GetOutgoingEdgesAsync("class-id").Returns(new List<RelationshipEdge>
        {
            CreateEdge("class-id", "interface-id", RelationshipKind.Implements)
        });
        _repository.GetNodeAsync("interface-id").Returns(interfaceNode);

        var result = await _service.GetRelationshipsAsync("class-id", RelationshipDirection.Outgoing);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Outgoing, Has.Count.EqualTo(1));
        Assert.That(result.Incoming, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(
        string id,
        string name,
        DeclarationKind kind,
        string fullyQualifiedName,
        string? filePath = null,
        int startLine = 0)
    {
        return new DeclarationNode
        {
            Id = id,
            Name = name,
            FullyQualifiedName = fullyQualifiedName,
            Kind = kind,
            FilePath = filePath ?? "test.cs",
            StartLine = startLine,
            StartColumn = 0,
            EndLine = startLine,
            EndColumn = 0,
            C4Level = C4Level.Code
        };
    }

    private static RelationshipEdge CreateEdge(string sourceId, string targetId, RelationshipKind kind)
    {
        return new RelationshipEdge
        {
            Id = $"{sourceId}-{targetId}-{kind}",
            SourceId = sourceId,
            TargetId = targetId,
            Kind = kind
        };
    }

    #endregion
}
