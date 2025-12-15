using Sharpitect.Analysis.Model;
using Sharpitect.Analysis.Model.Code;
using Sharpitect.Analysis.Output;

namespace Sharpitect.Analysis.Test.Output;

[TestFixture]
public class DebugPrinterTests
{
    private DebugPrinter _printer = null!;
    private StringWriter _writer = null!;

    [SetUp]
    public void Setup()
    {
        _printer = new DebugPrinter();
        _writer = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _writer.Dispose();
    }

    #region Empty Model Tests

    [Test]
    public void Write_EmptyModel_PrintsOnlyHeader()
    {
        var model = new ArchitectureModel();

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Is.EqualTo("Architecture Model" + Environment.NewLine));
    }

    #endregion

    #region Person Tests

    [Test]
    public void Write_SinglePerson_PrintsPersonNode()
    {
        var model = new ArchitectureModel();
        model.AddPerson(new Person("Admin User"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Person] Admin User"));
        Assert.That(output, Does.Contain("└── "));
    }

    [Test]
    public void Write_MultiplePeople_PrintsAllPeople()
    {
        var model = new ArchitectureModel();
        model.AddPerson(new Person("Admin"));
        model.AddPerson(new Person("Customer"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Person] Admin"));
        Assert.That(output, Does.Contain("[Person] Customer"));
    }

    #endregion

    #region SoftwareSystem Tests

    [Test]
    public void Write_SingleSystem_PrintsSystemNode()
    {
        var model = new ArchitectureModel();
        model.AddSystem(new SoftwareSystem("E-Commerce Platform"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[System] E-Commerce Platform"));
    }

    [Test]
    public void Write_SystemWithContainer_PrintsHierarchy()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("E-Commerce");
        system.AddContainer(new Container("Web API", "REST API", "ASP.NET Core"));
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[System] E-Commerce"));
        Assert.That(output, Does.Contain("[Container] Web API (ASP.NET Core)"));
    }

    #endregion

    #region Container Tests

    [Test]
    public void Write_ContainerWithTechnology_IncludesTechnology()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        system.AddContainer(new Container("Database", null, "PostgreSQL"));
        model.AddSystem(system);

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[Container] Database (PostgreSQL)"));
    }

    [Test]
    public void Write_ContainerWithoutTechnology_OmitsTechnology()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        system.AddContainer(new Container("Service"));
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Container] Service"));
        Assert.That(output, Does.Not.Contain("[Container] Service ("));
    }

    #endregion

    #region Component Tests

    [Test]
    public void Write_ComponentInContainer_PrintsNestedHierarchy()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("Web API");
        container.AddComponent(new Component("OrderService", "Handles orders"));
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Component] OrderService"));
    }

    #endregion

    #region Code Element Tests

    [Test]
    public void Write_ClassWithNamespace_IncludesNamespace()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        component.AddCodeElement(new ClassCode("OrderProcessor", "MyApp.Orders"));
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[Class] OrderProcessor (MyApp.Orders)"));
    }

    [Test]
    public void Write_ClassWithoutNamespace_OmitsNamespace()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        component.AddCodeElement(new ClassCode("OrderProcessor"));
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Class] OrderProcessor"));
        Assert.That(output, Does.Not.Contain("[Class] OrderProcessor ("));
    }

    [Test]
    public void Write_MethodWithReturnType_IncludesReturnType()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        var classCode = new ClassCode("Processor");
        classCode.AddMethod(new MethodCode("Process", "bool"));
        component.AddCodeElement(classCode);
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[Method] Process(): bool"));
    }

    [Test]
    public void Write_MethodWithoutReturnType_OmitsReturnType()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        var classCode = new ClassCode("Processor");
        classCode.AddMethod(new MethodCode("Process"));
        component.AddCodeElement(classCode);
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Method] Process()"));
        Assert.That(output, Does.Not.Contain("[Method] Process():"));
    }

    [Test]
    public void Write_PropertyWithType_IncludesType()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        var classCode = new ClassCode("Order");
        classCode.AddProperty(new PropertyCode("Id", "int"));
        component.AddCodeElement(classCode);
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[Property] Id: int"));
    }

    [Test]
    public void Write_PropertyWithoutType_OmitsType()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("API");
        var component = new Component("Services");
        var classCode = new ClassCode("Order");
        classCode.AddProperty(new PropertyCode("Id"));
        component.AddCodeElement(classCode);
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[Property] Id"));
        Assert.That(output, Does.Not.Contain("[Property] Id:"));
    }

    #endregion

    #region External Element Tests

    [Test]
    public void Write_ExternalSystem_PrintsExternalSystemNode()
    {
        var model = new ArchitectureModel();
        model.AddExternalSystem(new ExternalSystem("Payment Gateway"));

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[External System] Payment Gateway"));
    }

    [Test]
    public void Write_ExternalContainerWithTechnology_IncludesTechnology()
    {
        var model = new ArchitectureModel();
        model.AddExternalContainer(new ExternalContainer("Cache", "Redis"));

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("[External Container] Cache (Redis)"));
    }

    [Test]
    public void Write_ExternalContainerWithoutTechnology_OmitsTechnology()
    {
        var model = new ArchitectureModel();
        model.AddExternalContainer(new ExternalContainer("Cache"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("[External Container] Cache"));
        Assert.That(output, Does.Not.Contain("[External Container] Cache ("));
    }

    #endregion

    #region Relationship Tests

    [Test]
    public void Write_RelationshipFromPerson_PrintsRelationshipUnderPerson()
    {
        var model = new ArchitectureModel();
        var person = new Person("Admin");
        var system = new SoftwareSystem("CMS");
        model.AddPerson(person);
        model.AddSystem(system);
        model.AddRelationship(new Relationship(person, system, "manages content"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("-> CMS: manages content"));
    }

    [Test]
    public void Write_RelationshipWithTechnology_IncludesTechnology()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("API");
        var db = new ExternalContainer("Database", "PostgreSQL");
        model.AddSystem(system);
        model.AddExternalContainer(db);
        model.AddRelationship(new Relationship(system, db, "stores data", "SQL"));

        _printer.Write(model, _writer);

        Assert.That(_writer.ToString(), Does.Contain("-> Database: stores data [SQL]"));
    }

    [Test]
    public void Write_RelationshipWithoutTechnology_OmitsTechnology()
    {
        var model = new ArchitectureModel();
        var person = new Person("User");
        var system = new SoftwareSystem("App");
        model.AddPerson(person);
        model.AddSystem(system);
        model.AddRelationship(new Relationship(person, system, "uses"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("-> App: uses"));
        Assert.That(output, Does.Not.Contain("-> App: uses ["));
    }

    [Test]
    public void Write_MultipleRelationshipsFromSameSource_PrintsAllRelationships()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("API");
        var db = new ExternalContainer("Database");
        var cache = new ExternalContainer("Cache");
        model.AddSystem(system);
        model.AddExternalContainer(db);
        model.AddExternalContainer(cache);
        model.AddRelationship(new Relationship(system, db, "reads data"));
        model.AddRelationship(new Relationship(system, cache, "caches data"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.That(output, Does.Contain("-> Database: reads data"));
        Assert.That(output, Does.Contain("-> Cache: caches data"));
    }

    #endregion

    #region Tree Structure Tests

    [Test]
    public void Write_MultipleTopLevelElements_UsesCorrectBranchCharacters()
    {
        var model = new ArchitectureModel();
        model.AddPerson(new Person("User1"));
        model.AddPerson(new Person("User2"));
        model.AddSystem(new SoftwareSystem("System"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        // First two should use ├── , last should use └──
        Assert.That(output, Does.Contain("├── [Person] User1"));
        Assert.That(output, Does.Contain("├── [Person] User2"));
        Assert.That(output, Does.Contain("└── [System] System"));
    }

    [Test]
    public void Write_DeepHierarchy_PrintsCorrectIndentation()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container = new Container("Container");
        var component = new Component("Component");
        var classCode = new ClassCode("MyClass");
        classCode.AddMethod(new MethodCode("MyMethod"));
        component.AddCodeElement(classCode);
        container.AddComponent(component);
        system.AddContainer(container);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var lines = _writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Multiple(() =>
        {
            Assert.That(lines[0], Is.EqualTo("Architecture Model"));
            Assert.That(lines[1], Does.Contain("[System] System"));
            Assert.That(lines[2], Does.Contain("[Container] Container"));
            Assert.That(lines[3], Does.Contain("[Component] Component"));
            Assert.That(lines[4], Does.Contain("[Class] MyClass"));
            Assert.That(lines[5], Does.Contain("[Method] MyMethod()"));
        });
    }

    [Test]
    public void Write_SiblingElements_UsesVerticalBarForContinuation()
    {
        var model = new ArchitectureModel();
        var system = new SoftwareSystem("System");
        var container1 = new Container("Container1");
        var container2 = new Container("Container2");
        container1.AddComponent(new Component("Comp1"));
        system.AddContainer(container1);
        system.AddContainer(container2);
        model.AddSystem(system);

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        // Container1's child should have │ continuation because Container2 follows
        Assert.That(output, Does.Contain("│"));
    }

    #endregion

    #region Full Model Tests

    [Test]
    public void Write_CompleteModel_ProducesExpectedOutput()
    {
        var model = new ArchitectureModel();

        var admin = new Person("Admin");
        model.AddPerson(admin);

        var system = new SoftwareSystem("E-Commerce");
        var webApi = new Container("Web API", null, "ASP.NET Core");
        var orderComponent = new Component("OrderService");
        var orderClass = new ClassCode("OrderProcessor", "ECommerce.Orders");
        orderClass.AddMethod(new MethodCode("ProcessOrder", "bool"));
        orderClass.AddProperty(new PropertyCode("OrderId", "int"));
        orderComponent.AddCodeElement(orderClass);
        webApi.AddComponent(orderComponent);
        system.AddContainer(webApi);
        model.AddSystem(system);

        var db = new ExternalContainer("Database", "PostgreSQL");
        model.AddExternalContainer(db);

        model.AddRelationship(new Relationship(admin, system, "manages orders"));
        model.AddRelationship(new Relationship(webApi, db, "stores orders", "SQL"));

        _printer.Write(model, _writer);

        var output = _writer.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Architecture Model"));
            Assert.That(output, Does.Contain("[Person] Admin"));
            Assert.That(output, Does.Contain("-> E-Commerce: manages orders"));
            Assert.That(output, Does.Contain("[System] E-Commerce"));
            Assert.That(output, Does.Contain("[Container] Web API (ASP.NET Core)"));
            Assert.That(output, Does.Contain("-> Database: stores orders [SQL]"));
            Assert.That(output, Does.Contain("[Component] OrderService"));
            Assert.That(output, Does.Contain("[Class] OrderProcessor (ECommerce.Orders)"));
            Assert.That(output, Does.Contain("[Method] ProcessOrder(): bool"));
            Assert.That(output, Does.Contain("[Property] OrderId: int"));
            Assert.That(output, Does.Contain("[External Container] Database (PostgreSQL)"));
        });
    }

    #endregion
}
