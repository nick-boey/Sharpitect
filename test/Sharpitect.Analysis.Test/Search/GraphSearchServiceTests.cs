using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Search;

namespace Sharpitect.Analysis.Test.Search;

[TestFixture]
public class GraphSearchServiceTests
{
    private DeclarationGraph _graph = null!;
    private InMemoryGraphSource _source = null!;
    private GraphSearchService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _graph = new DeclarationGraph();
        _source = new InMemoryGraphSource(_graph);
        _service = new GraphSearchService(_source);
    }

    #region Contains Match Mode Tests

    [Test]
    public async Task SearchAsync_ContainsMode_MatchesPartialName()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "CustomerService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("3", "Repository", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "Service" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(n => n.Name.Contains("Service")), Is.True);
    }

    [Test]
    public async Task SearchAsync_ContainsMode_MatchesFullyQualifiedName()
    {
        _graph.AddNode(CreateTestNode("1", "MyClass", DeclarationKind.Class, "Acme.Orders.MyClass"));

        var query = new SearchQuery { SearchText = "Orders" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("MyClass"));
    }

    [Test]
    public async Task SearchAsync_ContainsMode_NoMatchReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "NotFound" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region StartsWith Match Mode Tests

    [Test]
    public async Task SearchAsync_StartsWithMode_MatchesNamePrefix()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "ServiceBase", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Order",
            MatchMode = SearchMatchMode.StartsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("OrderService"));
    }

    [Test]
    public async Task SearchAsync_StartsWithMode_MatchesFqnPrefix()
    {
        _graph.AddNode(CreateTestNode("1", "MyClass", DeclarationKind.Class, "Acme.Orders.MyClass"));

        var query = new SearchQuery
        {
            SearchText = "Acme",
            MatchMode = SearchMatchMode.StartsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_StartsWithMode_NoMatchReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Service",
            MatchMode = SearchMatchMode.StartsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region EndsWith Match Mode Tests

    [Test]
    public async Task SearchAsync_EndsWithMode_MatchesNameSuffix()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "OrderRepository", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Service",
            MatchMode = SearchMatchMode.EndsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("OrderService"));
    }

    [Test]
    public async Task SearchAsync_EndsWithMode_MatchesFqnSuffix()
    {
        _graph.AddNode(CreateTestNode("1", "MyClass", DeclarationKind.Class, "Acme.Orders.MyClass"));

        var query = new SearchQuery
        {
            SearchText = "MyClass",
            MatchMode = SearchMatchMode.EndsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_EndsWithMode_NoMatchReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Order",
            MatchMode = SearchMatchMode.EndsWith
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region Exact Match Mode Tests

    [Test]
    public async Task SearchAsync_ExactMode_MatchesExactName()
    {
        _graph.AddNode(CreateTestNode("1", "Order", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Order",
            MatchMode = SearchMatchMode.Exact
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Order"));
    }

    [Test]
    public async Task SearchAsync_ExactMode_MatchesExactFqn()
    {
        _graph.AddNode(CreateTestNode("1", "MyClass", DeclarationKind.Class, "Acme.MyClass"));

        var query = new SearchQuery
        {
            SearchText = "Acme.MyClass",
            MatchMode = SearchMatchMode.Exact
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_ExactMode_PartialMatchReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Order",
            MatchMode = SearchMatchMode.Exact
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region Kind Filter Tests

    [Test]
    public async Task SearchAsync_WithKindFilter_ReturnsOnlyMatchingKinds()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "GetOrder", DeclarationKind.Method));
        _graph.AddNode(CreateTestNode("3", "IOrderService", DeclarationKind.Interface));

        var query = new SearchQuery
        {
            SearchText = "Order",
            KindFilter = [DeclarationKind.Class, DeclarationKind.Interface]
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(n => n.Kind is DeclarationKind.Class or DeclarationKind.Interface), Is.True);
    }

    [Test]
    public async Task SearchAsync_WithSingleKindFilter_ReturnsOnlySingleKind()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "GetOrder", DeclarationKind.Method));

        var query = new SearchQuery
        {
            SearchText = "Order",
            KindFilter = [DeclarationKind.Method]
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Kind, Is.EqualTo(DeclarationKind.Method));
    }

    [Test]
    public async Task SearchAsync_WithEmptyKindFilter_ReturnsAllKinds()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "GetOrder", DeclarationKind.Method));

        var query = new SearchQuery { SearchText = "Order" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchAsync_KindFilterNoMatch_ReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "Order",
            KindFilter = [DeclarationKind.Method]
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region Case Sensitivity Tests

    [Test]
    public async Task SearchAsync_CaseInsensitiveByDefault_MatchesAnyCase()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "orderservice" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_CaseInsensitive_MatchesMixedCase()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "ORDERSERVICE" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SearchAsync_CaseSensitive_OnlyMatchesExactCase()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "orderservice",
            CaseSensitive = true
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_CaseSensitive_MatchesCorrectCase()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery
        {
            SearchText = "OrderService",
            CaseSensitive = true
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(1));
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task SearchAsync_EmptySearchText_ReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_WhitespaceSearchText_ReturnsEmpty()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));

        var query = new SearchQuery { SearchText = "   " };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_EmptyGraph_ReturnsEmpty()
    {
        var query = new SearchQuery { SearchText = "Test" };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void SearchAsync_NullQuery_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.SearchAsync(null!));
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GraphSearchService(null!));
    }

    #endregion

    #region Combined Filters Tests

    [Test]
    public async Task SearchAsync_CombinedFilters_AppliesAllFilters()
    {
        _graph.AddNode(CreateTestNode("1", "OrderService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("2", "OrderRepository", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("3", "CustomerService", DeclarationKind.Class));
        _graph.AddNode(CreateTestNode("4", "GetOrder", DeclarationKind.Method));

        var query = new SearchQuery
        {
            SearchText = "Service",
            MatchMode = SearchMatchMode.EndsWith,
            KindFilter = [DeclarationKind.Class],
            CaseSensitive = false
        };

        var results = await _service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(n => n.Name), Is.EquivalentTo(new[] { "OrderService", "CustomerService" }));
    }

    #endregion

    #region Helper Methods

    private static DeclarationNode CreateTestNode(
        string id,
        string name,
        DeclarationKind kind,
        string? fullyQualifiedName = null)
    {
        return new DeclarationNode
        {
            Id = id,
            Name = name,
            FullyQualifiedName = fullyQualifiedName ?? $"Test.{name}",
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
