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
        var node = CreateTestNode("TestClass", DeclarationKind.Class, "Namespace.TestClass");
        _repository.GetNodeAsync("Namespace.TestClass").Returns(node);

        var result = await _service.GetNodeAsync("Namespace.TestClass");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("Namespace.TestClass"));
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
        var parentNode = CreateTestNode("Parent", DeclarationKind.Class, "Parent");
        var childNode1 = CreateTestNode("Method1", DeclarationKind.Method, "Parent.Method1");
        var childNode2 = CreateTestNode("Method2", DeclarationKind.Method, "Parent.Method2");

        _repository.GetNodeAsync("Parent").Returns(parentNode);
        _repository.GetOutgoingEdgesAsync("Parent").Returns(new List<RelationshipEdge>
        {
            CreateEdge("Parent", "Parent.Method1", RelationshipKind.Contains),
            CreateEdge("Parent", "Parent.Method2", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("Parent.Method1").Returns(childNode1);
        _repository.GetNodeAsync("Parent.Method2").Returns(childNode2);

        var result = await _service.GetChildrenAsync("Parent");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ParentId, Is.EqualTo("Parent"));
        Assert.That(result.Children, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetChildrenAsync_WithKindFilter_ReturnsOnlyMatchingKind()
    {
        var parentNode = CreateTestNode("Parent", DeclarationKind.Class, "Parent");
        var methodNode = CreateTestNode("Method1", DeclarationKind.Method, "Parent.Method1");
        var propertyNode = CreateTestNode("Prop1", DeclarationKind.Property, "Parent.Prop1");

        _repository.GetNodeAsync("Parent").Returns(parentNode);
        _repository.GetOutgoingEdgesAsync("Parent").Returns(new List<RelationshipEdge>
        {
            CreateEdge("Parent", "Parent.Method1", RelationshipKind.Contains),
            CreateEdge("Parent", "Parent.Prop1", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("Parent.Method1").Returns(methodNode);
        _repository.GetNodeAsync("Parent.Prop1").Returns(propertyNode);

        var result = await _service.GetChildrenAsync("Parent", kindFilter: DeclarationKind.Method);

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
        var methodNode = CreateTestNode("Method", DeclarationKind.Method, "NS.Class.Method");
        var classNode = CreateTestNode("Class", DeclarationKind.Class, "NS.Class");
        var nsNode = CreateTestNode("NS", DeclarationKind.Namespace, "NS");

        _repository.GetNodeAsync("NS.Class.Method").Returns(methodNode);
        _repository.GetIncomingEdgesAsync("NS.Class.Method").Returns(new List<RelationshipEdge>
        {
            CreateEdge("NS.Class", "NS.Class.Method", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("NS.Class").Returns(classNode);
        _repository.GetIncomingEdgesAsync("NS.Class").Returns(new List<RelationshipEdge>
        {
            CreateEdge("NS", "NS.Class", RelationshipKind.Contains)
        });
        _repository.GetNodeAsync("NS").Returns(nsNode);
        _repository.GetIncomingEdgesAsync("NS").Returns(new List<RelationshipEdge>());

        var result = await _service.GetAncestorsAsync("NS.Class.Method");

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
        var classNode = CreateTestNode("MyClass", DeclarationKind.Class, "MyClass");
        var interfaceNode = CreateTestNode("IService", DeclarationKind.Interface, "IService");
        var testNode = CreateTestNode("MyClassTests", DeclarationKind.Class, "MyClassTests");

        _repository.GetNodeAsync("MyClass").Returns(classNode);
        _repository.GetOutgoingEdgesAsync("MyClass").Returns(new List<RelationshipEdge>
        {
            CreateEdge("MyClass", "IService", RelationshipKind.Implements)
        });
        _repository.GetIncomingEdgesAsync("MyClass").Returns(new List<RelationshipEdge>
        {
            CreateEdge("MyClassTests", "MyClass", RelationshipKind.References)
        });
        _repository.GetNodeAsync("IService").Returns(interfaceNode);
        _repository.GetNodeAsync("MyClassTests").Returns(testNode);

        var result = await _service.GetRelationshipsAsync("MyClass");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Outgoing, Has.Count.EqualTo(1));
        Assert.That(result.Incoming, Has.Count.EqualTo(1));
        Assert.That(result.Outgoing[0].Kind, Is.EqualTo("Implements"));
        Assert.That(result.Incoming[0].Kind, Is.EqualTo("References"));
    }

    [Test]
    public async Task GetRelationshipsAsync_WithOutgoingDirection_ReturnsOnlyOutgoing()
    {
        var classNode = CreateTestNode("MyClass", DeclarationKind.Class, "MyClass");
        var interfaceNode = CreateTestNode("IService", DeclarationKind.Interface, "IService");

        _repository.GetNodeAsync("MyClass").Returns(classNode);
        _repository.GetOutgoingEdgesAsync("MyClass").Returns(new List<RelationshipEdge>
        {
            CreateEdge("MyClass", "IService", RelationshipKind.Implements)
        });
        _repository.GetNodeAsync("IService").Returns(interfaceNode);

        var result = await _service.GetRelationshipsAsync("MyClass", RelationshipDirection.Outgoing);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Outgoing, Has.Count.EqualTo(1));
        Assert.That(result.Incoming, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(
        string name,
        DeclarationKind kind,
        string fullyQualifiedName,
        string? filePath = null,
        int startLine = 0)
    {
        // Id is the fully qualified name
        return new DeclarationNode
        {
            Id = fullyQualifiedName,
            Name = name,
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
