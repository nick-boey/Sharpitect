using Sharpitect.MCP.Formatting;

namespace Sharpitect.MCP.Test.Formatting;

[TestFixture]
public class OutputFormatterFactoryTests
{
    private OutputFormatterFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new OutputFormatterFactory();
    }

    [Test]
    public void GetFormatter_WithJson_ReturnsJsonFormatter()
    {
        var formatter = _factory.GetFormatter("json");

        Assert.That(formatter, Is.InstanceOf<JsonOutputFormatter>());
    }

    [Test]
    public void GetFormatter_WithText_ReturnsTextFormatter()
    {
        var formatter = _factory.GetFormatter("text");

        Assert.That(formatter, Is.InstanceOf<TextOutputFormatter>());
    }

    [Test]
    public void GetFormatter_WithNull_DefaultsToJson()
    {
        var formatter = _factory.GetFormatter(null);

        Assert.That(formatter, Is.InstanceOf<JsonOutputFormatter>());
    }

    [Test]
    public void GetFormatter_WithEmptyString_DefaultsToJson()
    {
        var formatter = _factory.GetFormatter("");

        Assert.That(formatter, Is.InstanceOf<JsonOutputFormatter>());
    }

    [Test]
    public void GetFormatter_IsCaseInsensitive()
    {
        var jsonFormatter = _factory.GetFormatter("JSON");
        var textFormatter = _factory.GetFormatter("TEXT");

        Assert.Multiple(() =>
        {
            Assert.That(jsonFormatter, Is.InstanceOf<JsonOutputFormatter>());
            Assert.That(textFormatter, Is.InstanceOf<TextOutputFormatter>());
        });
    }

    [Test]
    public void GetFormatter_WithUnknownFormat_DefaultsToJson()
    {
        var formatter = _factory.GetFormatter("unknown");

        Assert.That(formatter, Is.InstanceOf<JsonOutputFormatter>());
    }
}
