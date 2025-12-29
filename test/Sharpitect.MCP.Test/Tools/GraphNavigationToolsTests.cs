using ModelContextProtocol.Protocol;
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
    private ToolResultBuilder _resultBuilder = null!;

    [SetUp]
    public void SetUp()
    {
        _navigationService = Substitute.For<IGraphNavigationService>();
        _resultBuilder = new ToolResultBuilder(new TextOutputFormatter());
    }

    /// <summary>
    /// Gets the text content from a CallToolResult.
    /// </summary>
    private static string GetTextContent(CallToolResult result)
    {
        var textBlock = result.Content?.OfType<TextContentBlock>().FirstOrDefault();
        return textBlock?.Text ?? string.Empty;
    }

    #region SearchDeclarations Tests

    [Test]
    public async Task SearchDeclarations_ReturnsCallToolResult_WithTextAndStructuredContent()
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
            _resultBuilder,
            "Test");

        // Check text content is human-readable
        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("[Class]"));
        Assert.That(textContent, Does.Contain("TestClass"));

        // Check structured content is present
        Assert.That(result.StructuredContent, Is.Not.Null);
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
            _resultBuilder,
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
            _resultBuilder,
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
            _resultBuilder,
            "Namespace.TestClass");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("[Class]"));
        Assert.That(textContent, Does.Contain("TestClass"));
        Assert.That(result.StructuredContent, Is.Not.Null);
    }

    [Test]
    public async Task GetNode_ReturnsError_WhenNotFound()
    {
        _navigationService.GetNodeAsync("missing", Arg.Any<CancellationToken>())
            .Returns((NodeDetail?)null);

        var result = await GraphNavigationTools.GetNode(
            _navigationService,
            _resultBuilder,
            "missing");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("NOT_FOUND"));
        Assert.That(result.IsError, Is.True);
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
            _resultBuilder,
            "parent-id");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("parent-id"));
        Assert.That(textContent, Does.Contain("ChildMethod"));
        Assert.That(result.StructuredContent, Is.Not.Null);
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
            _resultBuilder,
            "class-id");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("OUTGOING"));
        Assert.That(textContent, Does.Contain("INCOMING"));
        Assert.That(textContent, Does.Contain("Implements"));
        Assert.That(result.StructuredContent, Is.Not.Null);
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
            _resultBuilder,
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
            _resultBuilder,
            "class");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("TestClass"));
        Assert.That(result.StructuredContent, Is.Not.Null);
    }

    [Test]
    public async Task ListByKind_ReturnsError_ForInvalidKind()
    {
        var result = await GraphNavigationTools.ListByKind(
            _navigationService,
            _resultBuilder,
            "invalid_kind");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("INVALID_PARAMETER"));
        Assert.That(result.IsError, Is.True);
    }

    #endregion

    #region Text Output Tests

    [Test]
    public async Task GetAncestors_ReturnsTextContent()
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
            _resultBuilder,
            "method-id");

        var textContent = GetTextContent(result);
        Assert.That(textContent, Does.Contain("Solution: MySolution"));
        Assert.That(textContent, Does.Contain("Project: MyProject"));
        Assert.That(result.StructuredContent, Is.Not.Null);
    }

    #endregion
}