using System.Text.Json;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Test.Formatting;

[TestFixture]
public class JsonOutputFormatterTests
{
    private JsonOutputFormatter _formatter = null!;

    [SetUp]
    public void SetUp()
    {
        _formatter = new JsonOutputFormatter();
    }

    [Test]
    public void FormatName_ReturnsJson()
    {
        Assert.That(_formatter.FormatName, Is.EqualTo("json"));
    }

    [Test]
    public void Format_NodeSummary_ReturnsValidJson()
    {
        var node = new NodeSummary(
            "Namespace.TestClass",
            "TestClass",
            "Class",
            "Code",
            "src/Test.cs",
            42,
            80);

        var result = _formatter.Format(node);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
        Assert.That(result, Does.Contain("\"id\":\"Namespace.TestClass\""));
        Assert.That(result, Does.Contain("\"name\":\"TestClass\""));
    }

    [Test]
    public void Format_SearchResults_ReturnsValidJson()
    {
        var searchResults = new SearchResults(
            new List<NodeSummary>
            {
                new("id1", "Class1", "Class", "Code", "test.cs", 10, 50),
                new("id2", "Class2", "Class", "Code", "test2.cs", 20, 60)
            },
            TotalCount: 2,
            Truncated: false);

        var result = _formatter.Format(searchResults);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
        Assert.That(result, Does.Contain("\"total_count\":2"));
        Assert.That(result, Does.Contain("\"truncated\":false"));
    }

    [Test]
    public void Format_ErrorResponse_ReturnsValidJson()
    {
        var error = ErrorResponse.NotFound("Node not found");

        var result = _formatter.Format(error);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
        Assert.That(result, Does.Contain("\"error\":true"));
        Assert.That(result, Does.Contain("\"error_code\":\"NOT_FOUND\""));
    }

    [Test]
    public void Format_UsesSnakeCaseNaming()
    {
        var node = new NodeSummary("id", "Test", "Class", "Code", "test.cs", 1, 10);

        var result = _formatter.Format(node);

        Assert.That(result, Does.Contain("\"line_number\":"));
        Assert.That(result, Does.Contain("\"file_path\":"));
        Assert.That(result, Does.Not.Contain("\"lineNumber\":"));
        Assert.That(result, Does.Not.Contain("\"filePath\":"));
    }

    [Test]
    public void Format_OmitsNullValues()
    {
        var node = new NodeSummary("id", "Test", "Class", null, null, null, null);

        var result = _formatter.Format(node);

        Assert.That(result, Does.Not.Contain("\"c4_level\":null"));
        Assert.That(result, Does.Not.Contain("\"file_path\":null"));
    }
}