using Microsoft.Data.Sqlite;
using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;
using Sharpitect.Analysis.Search;

namespace Sharpitect.Analysis.Test.Search;

[TestFixture]
public class RepositoryGraphSourceTests
{
    private string _testDbPath = null!;
    private SqliteGraphRepository _repository = null!;
    private RepositoryGraphSource _source = null!;

    [SetUp]
    public async Task SetUp()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"search_test_{Guid.NewGuid()}.db");
        _repository = new SqliteGraphRepository(_testDbPath);
        await _repository.InitializeAsync();
        _source = new RepositoryGraphSource(_repository);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _repository.DisposeAsync();
        SqliteConnection.ClearAllPools();
        await Task.Delay(50);

        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); }
            catch (IOException) { /* ignore cleanup failures */ }
        }
    }

    #region GetAllNodesAsync Tests

    [Test]
    public async Task GetAllNodesAsync_ReturnsAllNodes()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "Class1", DeclarationKind.Class),
            CreateTestNode("2", "Class2", DeclarationKind.Class)
        ]);

        var nodes = await _source.GetAllNodesAsync();

        Assert.That(nodes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllNodesAsync_EmptyRepository_ReturnsEmpty()
    {
        var nodes = await _source.GetAllNodesAsync();

        Assert.That(nodes, Is.Empty);
    }

    #endregion

    #region GetNodesByKindsAsync Tests

    [Test]
    public async Task GetNodesByKindsAsync_SingleKind_ReturnsMatchingNodes()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "Class1", DeclarationKind.Class),
            CreateTestNode("2", "Interface1", DeclarationKind.Interface),
            CreateTestNode("3", "Method1", DeclarationKind.Method)
        ]);

        var nodes = await _source.GetNodesByKindsAsync([DeclarationKind.Class]);

        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Kind, Is.EqualTo(DeclarationKind.Class));
    }

    [Test]
    public async Task GetNodesByKindsAsync_MultipleKinds_ReturnsAllMatchingNodes()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "Class1", DeclarationKind.Class),
            CreateTestNode("2", "Interface1", DeclarationKind.Interface),
            CreateTestNode("3", "Method1", DeclarationKind.Method)
        ]);

        var nodes = await _source.GetNodesByKindsAsync(
            [DeclarationKind.Class, DeclarationKind.Interface]);

        Assert.That(nodes, Has.Count.EqualTo(2));
        Assert.That(nodes.All(n => n.Kind is DeclarationKind.Class or DeclarationKind.Interface), Is.True);
    }

    [Test]
    public async Task GetNodesByKindsAsync_NoMatchingKinds_ReturnsEmpty()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "Class1", DeclarationKind.Class)
        ]);

        var nodes = await _source.GetNodesByKindsAsync([DeclarationKind.Method]);

        Assert.That(nodes, Is.Empty);
    }

    [Test]
    public async Task GetNodesByKindsAsync_EmptyKinds_ReturnsEmpty()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "Class1", DeclarationKind.Class)
        ]);

        var nodes = await _source.GetNodesByKindsAsync([]);

        Assert.That(nodes, Is.Empty);
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RepositoryGraphSource(null!));
    }

    #endregion

    #region Integration with GraphSearchService

    [Test]
    public async Task IntegrationWithSearchService_ReturnsExpectedResults()
    {
        await _repository.SaveNodesAsync([
            CreateTestNode("1", "OrderService", DeclarationKind.Class),
            CreateTestNode("2", "CustomerService", DeclarationKind.Class),
            CreateTestNode("3", "GetOrder", DeclarationKind.Method)
        ]);

        var service = new GraphSearchService(_source);
        var query = new SearchQuery
        {
            SearchText = "Service",
            KindFilter = [DeclarationKind.Class]
        };

        var results = await service.SearchAsync(query);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(n => n.Name), Is.EquivalentTo(new[] { "OrderService", "CustomerService" }));
    }

    #endregion

    #region Helper Methods

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

    #endregion
}
