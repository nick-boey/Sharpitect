using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Test.Formatting;

[TestFixture]
public class TextOutputFormatterTests
{
    private TextOutputFormatter _formatter = null!;

    [SetUp]
    public void SetUp()
    {
        _formatter = new TextOutputFormatter();
    }

    [Test]
    public void FormatName_ReturnsText()
    {
        Assert.That(_formatter.FormatName, Is.EqualTo("text"));
    }

    [Test]
    public void Format_SearchResults_ReturnsHumanReadableOutput()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
            {
                new("id1", "TestClass", "Class", "Code", "src/Test.cs", 42, 80)
            },
            TotalCount: 1,
            Truncated: false);

        var result = _formatter.Format(searchResults);

        Assert.That(result, Does.Contain("[Class] TestClass"));
        Assert.That(result, Does.Contain("src/Test.cs:42-80"));
        Assert.That(result, Does.Contain("1 match"));
    }

    [Test]
    public void Format_SearchResults_WithMultipleResults_ShowsCorrectCount()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
            {
                new("Namespace.Class1", "Class1", "Class", "Code", "test1.cs", 10, 50),
                new("Namespace.Class2", "Class2", "Class", "Code", "test2.cs", 20, 60)
            },
            TotalCount: 2,
            Truncated: false);

        var result = _formatter.Format(searchResults);

        Assert.That(result, Does.Contain("2 matches"));
    }

    [Test]
    public void Format_SearchResults_WithTruncation_IndicatesTruncation()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
            {
                new("Namespace.TestClass", "Class1", "Class", "Code", "test.cs", 10, 50)
            },
            TotalCount: 100,
            Truncated: true);

        var result = _formatter.Format(searchResults);

        Assert.That(result, Does.Contain("truncated"));
    }

    [Test]
    public void Format_NodeDetail_ReturnsDetailedOutput()
    {
        var node = new NodeDetail(
            "Namespace.TestClass",
            "TestClass",
            "Class",
            "Code",
            "src/Test.cs",
            42,
            100,
            null);

        var result = _formatter.Format(node);

        Assert.That(result, Does.Contain("[Class] TestClass"));
        Assert.That(result, Does.Contain("Namespace.TestClass"));
        Assert.That(result, Does.Contain("src/Test.cs:42-100"));
    }

    [Test]
    public void Format_ErrorResponse_ReturnsErrorFormat()
    {
        var error = ErrorResponse.NotFound("Node 'xyz' not found");

        var result = _formatter.Format(error);

        Assert.That(result, Does.Contain("ERROR"));
        Assert.That(result, Does.Contain("NOT_FOUND"));
        Assert.That(result, Does.Contain("Node 'xyz' not found"));
    }

    [Test]
    public void Format_AncestorsResult_ShowsHierarchy()
    {
        var result = new AncestorsResult(
            "method.id",
            new List<NodeSummary>
            {
                new("MySolution", "MySolution", "Solution", "System", null, null, null),
                new("MySolution.MyProject", "MyProject", "Project", "Container", null, null, null),
                new("MyNamespace", "MyNamespace", "Namespace", null, null, null, null),
                new("MyNamespace.MyClass", "MyClass", "Class", "Code", "test.cs", 10, 50)
            });

        var formatted = _formatter.Format(result);

        Assert.That(formatted, Does.Contain("Solution: MySolution"));
        Assert.That(formatted, Does.Contain("Project: MyProject"));
        Assert.That(formatted, Does.Contain("Namespace: MyNamespace"));
        Assert.That(formatted, Does.Contain("Class: MyClass"));
    }

    [Test]
    public void Format_RelationshipsResult_ShowsOutgoingAndIncoming()
    {
        var result = new RelationshipsResult(
            "class.id",
            new List<RelationshipInfo>
            {
                new("Implements", "interface.id", "IService", "Interface")
            },
            new List<IncomingRelationshipInfo>
            {
                new("References", "test.id", "TestClass", "Class")
            });

        var formatted = _formatter.Format(result);

        Assert.That(formatted, Does.Contain("OUTGOING"));
        Assert.That(formatted, Does.Contain("Implements"));
        Assert.That(formatted, Does.Contain("IService"));
        Assert.That(formatted, Does.Contain("INCOMING"));
        Assert.That(formatted, Does.Contain("References"));
        Assert.That(formatted, Does.Contain("TestClass"));
    }

    [Test]
    public void Format_NodeNotFoundResponse_ShowsMessageAndSuggestions()
    {
        var response = new NodeNotFoundResponse(
            "Node 'TestClass' was not found. Did you mean one of these?",
            new List<NodeSummary>
            {
                new("Namespace.TestClass1", "TestClass1", "Class", "Code", "test1.cs", 10, 50),
                new("Namespace.TestClass2", "TestClass2", "Class", "Code", "test2.cs", 20, 60)
            });

        var formatted = _formatter.Format(response);

        Assert.That(formatted, Does.Contain("ERROR [NOT_FOUND]"));
        Assert.That(formatted, Does.Contain("Node 'TestClass' was not found"));
        Assert.That(formatted, Does.Contain("Similar nodes:"));
        Assert.That(formatted, Does.Contain("[Class] TestClass1"));
        Assert.That(formatted, Does.Contain("Namespace.TestClass1"));
        Assert.That(formatted, Does.Contain("[Class] TestClass2"));
    }

    [Test]
    public void Format_NodeNotFoundResponse_WithNoSuggestions_ShowsOnlyMessage()
    {
        var response = new NodeNotFoundResponse(
            "Node 'xyz' was not found.",
            new List<NodeSummary>());

        var formatted = _formatter.Format(response);

        Assert.That(formatted, Does.Contain("ERROR [NOT_FOUND]"));
        Assert.That(formatted, Does.Contain("Node 'xyz' was not found"));
        Assert.That(formatted, Does.Not.Contain("Similar nodes:"));
    }
}