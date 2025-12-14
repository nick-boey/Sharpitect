using Sharpitect.Analysis.Analyzers;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class ConfigurationParserTests
{
    private ConfigurationParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new ConfigurationParser();
    }

    [Test]
    public void ParseSystemConfiguration_ParsesSystemNameAndDescription()
    {
        var yaml = """
                   system:
                     name: "ShopEasy"
                     description: "E-commerce platform"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.System, Is.Not.Null);
            Assert.That(config.System!.Name, Is.EqualTo("ShopEasy"));
            Assert.That(config.System.Description, Is.EqualTo("E-commerce platform"));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ParsesPeople()
    {
        var yaml = """
                   system:
                     name: "Test System"

                   people:
                     - name: Customer
                       description: "Online shopper"
                     - name: Admin
                       description: "System administrator"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.People, Has.Count.EqualTo(2));
            Assert.That(config.People![0].Name, Is.EqualTo("Customer"));
            Assert.That(config.People[0].Description, Is.EqualTo("Online shopper"));
            Assert.That(config.People[1].Name, Is.EqualTo("Admin"));
            Assert.That(config.People[1].Description, Is.EqualTo("System administrator"));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ParsesExternalSystems()
    {
        var yaml = """
                   system:
                     name: "Test System"

                   externalSystems:
                     - name: PaymentGateway
                       description: "Stripe payment processor"
                     - name: EmailService
                       description: "SendGrid email provider"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.ExternalSystems, Has.Count.EqualTo(2));
            Assert.That(config.ExternalSystems![0].Name, Is.EqualTo("PaymentGateway"));
            Assert.That(config.ExternalSystems[0].Description, Is.EqualTo("Stripe payment processor"));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ParsesExternalContainers()
    {
        var yaml = """
                   system:
                     name: "Test System"

                   externalContainers:
                     - name: Database
                       technology: PostgreSQL
                       description: "Main database"
                     - name: Cache
                       technology: Redis
                       description: "Session cache"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.ExternalContainers, Has.Count.EqualTo(2));
            Assert.That(config.ExternalContainers![0].Name, Is.EqualTo("Database"));
            Assert.That(config.ExternalContainers[0].Technology, Is.EqualTo("PostgreSQL"));
            Assert.That(config.ExternalContainers[0].Description, Is.EqualTo("Main database"));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ParsesRelationships()
    {
        var yaml = """
                   system:
                     name: "Test System"

                   relationships:
                     - start: "Payment Service"
                       action: "processes payment"
                       end: "PaymentGateway"
                     - start: "Data Access"
                       action: "stores data"
                       end: "Database"
                     - start: "Notification Service"
                       action: "sends notification"
                       end: "EmailService"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Relationships, Has.Count.EqualTo(3));
            Assert.That(config.Relationships![0].Start, Is.EqualTo("Payment Service"));
            Assert.That(config.Relationships[0].Action, Is.EqualTo("processes payment"));
            Assert.That(config.Relationships[0].End, Is.EqualTo("PaymentGateway"));
            Assert.That(config.Relationships[1].Start, Is.EqualTo("Data Access"));
            Assert.That(config.Relationships[1].Action, Is.EqualTo("stores data"));
            Assert.That(config.Relationships[1].End, Is.EqualTo("Database"));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ParsesFullConfig()
    {
        var yaml = """
                   system:
                     name: "ShopEasy"
                     description: "E-commerce platform"

                   people:
                     - name: Customer
                       description: "Online shopper"
                     - name: Admin
                       description: "System administrator"

                   externalSystems:
                     - name: PaymentGateway
                       description: "Stripe payment processor"

                   externalContainers:
                     - name: Database
                       technology: PostgreSQL
                       description: "Main database"

                   relationships:
                     - start: "Payment Service"
                       action: "processes payment"
                       end: "PaymentGateway"
                     - start: "Data Access"
                       action: "stores data"
                       end: "Database"
                   """;

        var config = _parser.ParseSystemConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.System!.Name, Is.EqualTo("ShopEasy"));
            Assert.That(config.System.Description, Is.EqualTo("E-commerce platform"));
            Assert.That(config.People, Has.Count.EqualTo(2));
            Assert.That(config.ExternalSystems, Has.Count.EqualTo(1));
            Assert.That(config.ExternalContainers, Has.Count.EqualTo(1));
            Assert.That(config.Relationships, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ParseSystemConfiguration_ReturnsNull_ForEmptyString()
    {
        var config = _parser.ParseSystemConfiguration("");

        Assert.That(config, Is.Null);
    }

    [Test]
    public void ParseSystemConfiguration_ReturnsNull_ForWhitespaceOnly()
    {
        var config = _parser.ParseSystemConfiguration("   \n\t  ");

        Assert.That(config, Is.Null);
    }

    [Test]
    public void ParseContainerConfiguration_ParsesContainerMetadata()
    {
        var yaml = """
                   container:
                     name: "Web API"
                     description: "RESTful API"
                     technology: "ASP.NET Core 8"
                   """;

        var config = _parser.ParseContainerConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Container, Is.Not.Null);
            Assert.That(config.Container!.Name, Is.EqualTo("Web API"));
            Assert.That(config.Container.Description, Is.EqualTo("RESTful API"));
            Assert.That(config.Container.Technology, Is.EqualTo("ASP.NET Core 8"));
        });
    }

    [Test]
    public void ParseContainerConfiguration_ParsesNamespaceComponents()
    {
        const string yaml = """
                            container:
                              name: "Web API"

                            components:
                              - namespace: MyApp.Services
                                name: "Business Services"
                                description: "Core business logic"
                              - namespace: MyApp.Data
                                name: "Data Access"
                                description: "Repository implementations"
                            """;

        var config = _parser.ParseContainerConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Components, Has.Count.EqualTo(2));
            Assert.That(config.Components![0].Namespace, Is.EqualTo("MyApp.Services"));
            Assert.That(config.Components[0].Name, Is.EqualTo("Business Services"));
            Assert.That(config.Components[0].Description, Is.EqualTo("Core business logic"));
            Assert.That(config.Components[1].Namespace, Is.EqualTo("MyApp.Data"));
            Assert.That(config.Components[1].Name, Is.EqualTo("Data Access"));
        });
    }

    [Test]
    public void ParseContainerConfiguration_ReturnsNull_ForEmptyString()
    {
        var config = _parser.ParseContainerConfiguration("");

        Assert.That(config, Is.Null);
    }

    [Test]
    public void ParseContainerConfiguration_HandlesMinimalConfig()
    {
        var yaml = """

                   container:
                     name: "Simple Container"

                   """;

        var config = _parser.ParseContainerConfiguration(yaml);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Container!.Name, Is.EqualTo("Simple Container"));
            Assert.That(config.Container.Description, Is.Null);
            Assert.That(config.Container.Technology, Is.Null);
            Assert.That(config.Components, Is.Null);
        });
    }
}