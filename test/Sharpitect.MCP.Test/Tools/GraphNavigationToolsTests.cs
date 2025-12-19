using NSubstitute;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;
using Sharpitect.MCP.Services;
using Sharpitect.MCP.Tools;

namespace Sharpitect.MCP.Test.Tools;

[TestFixture]
public class GraphNavigationToolsTests
{
    private IGraphNavigationService _navigationService = null!;
    private IOutputFormatterFactory _formatterFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _navigationService = Substitute.For<IGraphNavigationService>();
        _formatterFactory = new OutputFormatterFactory();
    }

    #region SearchDeclarations Tests

    [Test]
    public async Task SearchDeclarations_ReturnsJsonByDefault()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
                { new("Namespace.TestClass", "TestClass", "Class", "Code", "test.cs", 10, 50) },
            TotalCount: 1,
            Truncated: false);
        _navigationService.SearchAsync(
                Arg.Any<string>(),
                Arg.Any<SearchMatchMode>(),
                Arg.Any<IReadOnlyCollection<DeclarationKind>?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var result = await GraphNavigationTools.SearchDeclarations(
            _navigationService,
            _formatterFactory,
            "Test");

        Assert.That(result, Does.StartWith("{"));
        Assert.That(result, Does.Contain("\"results\""));
    }

    [Test]
    public async Task SearchDeclarations_ReturnsTextWhenRequested()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
                { new("Namespace.TestClass", "TestClass", "Class", "Code", "test.cs", 10, 50) },
            TotalCount: 1,
            Truncated: false);
        _navigationService.SearchAsync(
                Arg.Any<string>(),
                Arg.Any<SearchMatchMode>(),
                Arg.Any<IReadOnlyCollection<DeclarationKind>?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var result = await GraphNavigationTools.SearchDeclarations(
            _navigationService,
            _formatterFactory,
            "Test",
            format: "text");

        Assert.That(result, Does.Contain("[Class]"));
        Assert.That(result, Does.Contain("TestClass"));
    }

    [Test]
    public async Task SearchDeclarations_ParsesMatchModeCorrectly()
    {
        var searchResults = new SearchResults([], TotalCount: 0, Truncated: false);
        _navigationService.SearchAsync(
                Arg.Any<string>(),
                Arg.Any<SearchMatchMode>(),
                Arg.Any<IReadOnlyCollection<DeclarationKind>?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResults);

        await GraphNavigationTools.SearchDeclarations(
            _navigationService,
            _formatterFactory,
            "Test",
            matchMode: "starts_with");

        await _navigationService.Received(1).SearchAsync(
            "Test",
            SearchMatchMode.StartsWith,
            Arg.Any<IReadOnlyCollection<DeclarationKind>?>(),
            false,
            50,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchDeclarations_ParsesKindFilterCorrectly()
    {
        var searchResults = new SearchResults([], TotalCount: 0, Truncated: false);
        _navigationService.SearchAsync(
                Arg.Any<string>(),
                Arg.Any<SearchMatchMode>(),
                Arg.Any<IReadOnlyCollection<DeclarationKind>?>(),
                Arg.Any<bool>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResults);

        await GraphNavigationTools.SearchDeclarations(
            _navigationService,
            _formatterFactory,
            "Test",
            kind: "class");

        await _navigationService.Received(1).SearchAsync(
            "Test",
            SearchMatchMode.Contains,
            Arg.Is<IReadOnlyCollection<DeclarationKind>?>(k => k != null && k.Contains(DeclarationKind.Class)),
            false,
            50,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetNode Tests

    [Test]
    public async Task GetNode_ReturnsNodeDetail_WhenFound()
    {
        var nodeDetail = new NodeDetail(
            "Namespace.TestClass", "TestClass", "Class", "Code", "test.cs", 10, 50, null);
        _navigationService.GetNodeAsync("Namespace.TestClass", Arg.Any<CancellationToken>())
            .Returns(nodeDetail);

        var result = await GraphNavigationTools.GetNode(
            _navigationService,
            _formatterFactory,
            "Namespace.TestClass");

        Assert.That(result, Does.Contain("\"id\":\"Namespace.TestClass\""));
        Assert.That(result, Does.Contain("TestClass"));
    }

    [Test]
    public async Task GetNode_SearchByFullyQualifiedName_WhenFound()
    {
        var nodeDetail = new NodeDetail(
            "Namespace.TestClass", "TestClass", "Class", "Code", "test.cs", 10, 50, null);
        _navigationService.GetNodeAsync("Namespace.TestClass", Arg.Any<CancellationToken>())
            .Returns(nodeDetail);

        var result = await GraphNavigationTools.GetNode(
            _navigationService,
            _formatterFactory,
            "Namespace.TestClass");

        Assert.That(result, Does.Contain("\"id\":\"Namespace.TestClass\""));
        Assert.That(result, Does.Contain("TestClass"));
    }

    [Test]
    public async Task GetNode_ReturnsError_WhenNotFound()
    {
        _navigationService.GetNodeAsync("missing", Arg.Any<CancellationToken>())
            .Returns((NodeDetail?)null);

        var result = await GraphNavigationTools.GetNode(
            _navigationService,
            _formatterFactory,
            "missing");

        Assert.That(result, Does.Contain("\"error\":true"));
        Assert.That(result, Does.Contain("NOT_FOUND"));
    }

    #endregion

    #region GetChildren Tests

    [Test]
    public async Task GetChildren_ReturnsChildren_WhenFound()
    {
        var childrenResult = new ChildrenResult(
            "parent-id",
            new List<NodeSummary>
                { new("Namespace.Class.ChildMethod", "ChildMethod", "Method", "Code", "test.cs", 20, 30) },
            TotalCount: 1,
            Truncated: false);
        _navigationService.GetChildrenAsync("parent-id", Arg.Any<DeclarationKind?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(childrenResult);

        var result = await GraphNavigationTools.GetChildren(
            _navigationService,
            _formatterFactory,
            "parent-id");

        Assert.That(result, Does.Contain("\"parent_id\":\"parent-id\""));
        Assert.That(result, Does.Contain("ChildMethod"));
    }

    #endregion

    #region GetRelationships Tests

    [Test]
    public async Task GetRelationships_ReturnsRelationships_WhenFound()
    {
        var relationshipsResult = new RelationshipsResult(
            "class-id",
            new List<RelationshipInfo> { new("Implements", "interface-id", "IService", "Interface") },
            new List<IncomingRelationshipInfo> { new("References", "test-id", "TestClass", "Class") });
        _navigationService.GetRelationshipsAsync(
                "class-id",
                Arg.Any<RelationshipDirection>(),
                Arg.Any<RelationshipKind?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(relationshipsResult);

        var result = await GraphNavigationTools.GetRelationships(
            _navigationService,
            _formatterFactory,
            "class-id");

        Assert.That(result, Does.Contain("\"outgoing\""));
        Assert.That(result, Does.Contain("\"incoming\""));
        Assert.That(result, Does.Contain("Implements"));
    }

    [Test]
    public async Task GetRelationships_ParsesDirectionCorrectly()
    {
        var relationshipsResult = new RelationshipsResult(
            "class-id",
            new List<RelationshipInfo>(),
            new List<IncomingRelationshipInfo>());
        _navigationService.GetRelationshipsAsync(
                Arg.Any<string>(),
                Arg.Any<RelationshipDirection>(),
                Arg.Any<RelationshipKind?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(relationshipsResult);

        await GraphNavigationTools.GetRelationships(
            _navigationService,
            _formatterFactory,
            "class-id",
            direction: "outgoing");

        await _navigationService.Received(1).GetRelationshipsAsync(
            "class-id",
            RelationshipDirection.Outgoing,
            Arg.Any<RelationshipKind?>(),
            50,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ListByKind Tests

    [Test]
    public async Task ListByKind_ReturnsResults()
    {
        var listResult = new ListByKindResult(
            "Class",
            null,
            new List<NodeSummary>
                { new("Namespace.TestClass", "TestClass", "Class", "Code", "test.cs", 10, 50) },
            TotalCount: 1,
            Truncated: false);
        _navigationService.ListByKindAsync(
                Arg.Any<DeclarationKind>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(listResult);

        var result = await GraphNavigationTools.ListByKind(
            _navigationService,
            _formatterFactory,
            "class");

        Assert.That(result, Does.Contain("TestClass"));
    }

    [Test]
    public async Task ListByKind_ReturnsError_ForInvalidKind()
    {
        var result = await GraphNavigationTools.ListByKind(
            _navigationService,
            _formatterFactory,
            "invalid_kind");

        Assert.That(result, Does.Contain("\"error\":true"));
        Assert.That(result, Does.Contain("INVALID_PARAMETER"));
    }

    #endregion

    #region Text Output Tests

    [Test]
    public async Task GetAncestors_ReturnsTextOutput_WhenRequested()
    {
        var ancestorsResult = new AncestorsResult(
            "method-id",
            new List<NodeSummary>
            {
                new("MySolution", "MySolution", "Solution", "System", null, null, null),
                new("MySolution.MyProject", "MyProject", "Project", "Container", null, null, null)
            });
        _navigationService.GetAncestorsAsync("method-id", Arg.Any<CancellationToken>())
            .Returns(ancestorsResult);

        var result = await GraphNavigationTools.GetAncestors(
            _navigationService,
            _formatterFactory,
            "method-id",
            format: "text");

        Assert.That(result, Does.Contain("Solution: MySolution"));
        Assert.That(result, Does.Contain("Project: MyProject"));
    }

    #endregion
}