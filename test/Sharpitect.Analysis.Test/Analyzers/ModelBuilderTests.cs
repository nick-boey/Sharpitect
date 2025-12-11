using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Configuration;
using Sharpitect.Analysis.Model;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class ModelBuilderTests
{
    private ModelBuilder _builder = null!;

    [SetUp]
    public void Setup()
    {
        _builder = new ModelBuilder();
    }

    #region BuildSystem Tests

    [Test]
    public void BuildSystem_WithConfig_UsesConfigNameAndDescription()
    {
        var config = new SystemConfiguration
        {
            System = new SystemDefinition
            {
                Name = "ShopEasy",
                Description = "E-commerce platform"
            }
        };

        var system = _builder.BuildSystem(config);

        Assert.That(system.Name, Is.EqualTo("ShopEasy"));
        Assert.That(system.Description, Is.EqualTo("E-commerce platform"));
    }

    [Test]
    public void BuildSystem_WithNullConfig_UsesDefaultName()
    {
        var system = _builder.BuildSystem(null);

        Assert.That(system.Name, Is.EqualTo("Unnamed System"));
        Assert.That(system.Description, Is.Null);
    }

    [Test]
    public void BuildSystem_WithNullSystemDefinition_UsesDefaultName()
    {
        var config = new SystemConfiguration { System = null };

        var system = _builder.BuildSystem(config);

        Assert.That(system.Name, Is.EqualTo("Unnamed System"));
    }

    #endregion

    #region BuildContainer Tests

    [Test]
    public void BuildContainer_WithConfig_UsesConfigValues()
    {
        var config = new ContainerConfiguration
        {
            Container = new ContainerDefinition
            {
                Name = "Web API",
                Description = "RESTful API",
                Technology = "ASP.NET Core 8"
            }
        };

        var container = _builder.BuildContainer(config, "Project/Project.csproj");

        Assert.That(container.Name, Is.EqualTo("Web API"));
        Assert.That(container.Description, Is.EqualTo("RESTful API"));
        Assert.That(container.Technology, Is.EqualTo("ASP.NET Core 8"));
    }

    [Test]
    public void BuildContainer_WithNullConfig_UsesProjectName()
    {
        var container = _builder.BuildContainer(null, "MyProject/MyProject.csproj");

        Assert.That(container.Name, Is.EqualTo("MyProject"));
        Assert.That(container.Description, Is.Null);
        Assert.That(container.Technology, Is.Null);
    }

    [Test]
    public void BuildContainer_WithNullContainerDefinition_UsesProjectName()
    {
        var config = new ContainerConfiguration { Container = null };

        var container = _builder.BuildContainer(config, "Services/OrderService.csproj");

        Assert.That(container.Name, Is.EqualTo("OrderService"));
    }

    #endregion

    #region BuildComponents Tests

    [Test]
    public void BuildComponents_CreatesComponentFromAttribute()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service",
                ComponentDescription = "Manages orders"
            }
        };

        _builder.BuildComponents(container, types, null);

        Assert.That(container.Components, Has.Count.EqualTo(1));
        Assert.That(container.Components[0].Name, Is.EqualTo("Order Service"));
        Assert.That(container.Components[0].Description, Is.EqualTo("Manages orders"));
    }

    [Test]
    public void BuildComponents_CreatesComponentFromNamespaceMapping()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "OrderService",
                Namespace = "MyApp.Services",
                IsClass = true
            }
        };
        var namespaceComponents = new List<ComponentDefinition>
        {
            new()
            {
                Namespace = "MyApp.Services",
                Name = "Business Services",
                Description = "Core business logic"
            }
        };

        _builder.BuildComponents(container, types, namespaceComponents);

        Assert.That(container.Components, Has.Count.EqualTo(1));
        Assert.That(container.Components[0].Name, Is.EqualTo("Business Services"));
        Assert.That(container.Components[0].Description, Is.EqualTo("Core business logic"));
    }

    [Test]
    public void BuildComponents_MapsClassToComponentViaInterfaceImplementation()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service"
            },
            new()
            {
                Name = "OrderService",
                IsClass = true,
                BaseTypes = new List<string> { "IOrderService" },
                Methods = { new MethodAnalysisResult { Name = "CreateOrder", ReturnType = "void" } },
                Properties = { new PropertyAnalysisResult { Name = "Status", Type = "string" } }
            }
        };

        _builder.BuildComponents(container, types, null);

        Assert.That(container.Components, Has.Count.EqualTo(1));
        var component = container.Components[0];
        Assert.That(component.CodeElements, Has.Count.EqualTo(1));

        var classCode = component.CodeElements[0] as ClassCode;
        Assert.That(classCode, Is.Not.Null);
        Assert.That(classCode!.Name, Is.EqualTo("OrderService"));
        Assert.That(classCode.Members, Has.Count.EqualTo(2)); // 1 method + 1 property
    }

    [Test]
    public void BuildComponents_MapsClassToComponentViaNamespace()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "OrderService",
                Namespace = "MyApp.Services.Orders",
                IsClass = true
            }
        };
        var namespaceComponents = new List<ComponentDefinition>
        {
            new()
            {
                Namespace = "MyApp.Services",
                Name = "Business Services"
            }
        };

        _builder.BuildComponents(container, types, namespaceComponents);

        var component = container.Components[0];
        Assert.That(component.CodeElements, Has.Count.EqualTo(1));
        Assert.That(component.CodeElements[0].Name, Is.EqualTo("OrderService"));
    }

    [Test]
    public void BuildComponents_AttributeTakesPriorityOverNamespace()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "OrderService",
                Namespace = "MyApp.Services",
                IsClass = true,
                ComponentName = "Order Service" // Has attribute
            }
        };
        var namespaceComponents = new List<ComponentDefinition>
        {
            new()
            {
                Namespace = "MyApp.Services",
                Name = "Business Services" // Would match by namespace
            }
        };

        _builder.BuildComponents(container, types, namespaceComponents);

        // Should create both components, but class goes to "Order Service" (attribute priority)
        Assert.That(container.Components.Count, Is.EqualTo(2));
        var orderComponent = container.Components.First(c => c.Name == "Order Service");
        Assert.That(orderComponent.CodeElements, Has.Count.EqualTo(1));
        Assert.That(orderComponent.CodeElements[0].Name, Is.EqualTo("OrderService"));

        var businessComponent = container.Components.First(c => c.Name == "Business Services");
        Assert.That(businessComponent.CodeElements, Is.Empty);
    }

    [Test]
    public void BuildComponents_DoesNotDuplicateComponents()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service"
            },
            new()
            {
                Name = "IOrderValidator",
                IsInterface = true,
                ComponentName = "Order Service" // Same component name
            }
        };

        _builder.BuildComponents(container, types, null);

        Assert.That(container.Components, Has.Count.EqualTo(1));
        Assert.That(container.Components[0].Name, Is.EqualTo("Order Service"));
    }

    [Test]
    public void BuildComponents_MapsMultipleClassesToSameComponent()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service"
            },
            new()
            {
                Name = "OrderService",
                IsClass = true,
                BaseTypes = new List<string> { "IOrderService" }
            },
            new()
            {
                Name = "OrderValidator",
                IsClass = true,
                BaseTypes = new List<string> { "IOrderService" }
            }
        };

        _builder.BuildComponents(container, types, null);

        var component = container.Components[0];
        Assert.That(component.CodeElements, Has.Count.EqualTo(2));
        Assert.That(component.CodeElements.Select(c => c.Name), Contains.Item("OrderService"));
        Assert.That(component.CodeElements.Select(c => c.Name), Contains.Item("OrderValidator"));
    }

    [Test]
    public void BuildComponents_IgnoresClassWithNoMatchingComponent()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "OrphanClass",
                Namespace = "MyApp.Other",
                IsClass = true
            }
        };

        _builder.BuildComponents(container, types, null);

        Assert.That(container.Components, Is.Empty);
    }

    [Test]
    public void BuildComponents_IncludesMethodsAndProperties()
    {
        var container = new Container("Test Container");
        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service"
            },
            new()
            {
                Name = "OrderService",
                Namespace = "MyApp.Services",
                IsClass = true,
                BaseTypes = new List<string> { "IOrderService" },
                Methods =
                {
                    new MethodAnalysisResult { Name = "CreateOrder", ReturnType = "Order" },
                    new MethodAnalysisResult { Name = "GetOrder", ReturnType = "Order" }
                },
                Properties =
                {
                    new PropertyAnalysisResult { Name = "Status", Type = "string" },
                    new PropertyAnalysisResult { Name = "Id", Type = "int" }
                }
            }
        };

        _builder.BuildComponents(container, types, null);

        var classCode = container.Components[0].CodeElements[0] as ClassCode;
        Assert.That(classCode, Is.Not.Null);
        Assert.That(classCode!.Namespace, Is.EqualTo("MyApp.Services"));
        Assert.That(classCode.Members, Has.Count.EqualTo(4)); // 2 methods + 2 properties

        var methods = classCode.Members.OfType<MethodCode>().ToList();
        Assert.That(methods, Has.Count.EqualTo(2));
        Assert.That(methods[0].Name, Is.EqualTo("CreateOrder"));
        Assert.That(methods[0].ReturnType, Is.EqualTo("Order"));

        var properties = classCode.Members.OfType<PropertyCode>().ToList();
        Assert.That(properties, Has.Count.EqualTo(2));
        Assert.That(properties[0].Name, Is.EqualTo("Status"));
        Assert.That(properties[0].Type, Is.EqualTo("string"));
    }

    #endregion

    #region BuildRelationships Tests

    [Test]
    public void BuildRelationships_CreatesUserActionRelationship()
    {
        var model = new ArchitectureModel();
        var component = new Component("Order Service");
        var person = new Person("Customer", "Online shopper");
        model.AddPerson(person);

        var componentMap = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase)
        {
            { "Order Service", component }
        };
        var peopleMap = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase)
        {
            { "Customer", person }
        };

        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service",
                Methods =
                {
                    new MethodAnalysisResult
                    {
                        Name = "CreateOrder",
                        UserActionPerson = "Customer",
                        UserActionDescription = "places an order"
                    }
                }
            }
        };

        _builder.BuildRelationships(model, types, componentMap, peopleMap);

        Assert.That(model.Relationships, Has.Count.EqualTo(1));
        var relationship = model.Relationships[0];
        Assert.That(relationship.Source, Is.SameAs(person));
        Assert.That(relationship.Destination, Is.SameAs(component));
        Assert.That(relationship.Description, Is.EqualTo("places an order"));
    }

    [Test]
    public void BuildRelationships_IgnoresUnknownPerson()
    {
        var model = new ArchitectureModel();
        var component = new Component("Order Service");

        var componentMap = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase)
        {
            { "Order Service", component }
        };
        var peopleMap = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase);

        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "IOrderService",
                IsInterface = true,
                ComponentName = "Order Service",
                Methods =
                {
                    new MethodAnalysisResult
                    {
                        Name = "CreateOrder",
                        UserActionPerson = "Unknown", // Person not in map
                        UserActionDescription = "places an order"
                    }
                }
            }
        };

        _builder.BuildRelationships(model, types, componentMap, peopleMap);

        Assert.That(model.Relationships, Is.Empty);
    }

    [Test]
    public void BuildRelationships_IgnoresTypeWithNoComponent()
    {
        var model = new ArchitectureModel();
        var person = new Person("Customer");
        model.AddPerson(person);

        var componentMap = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
        var peopleMap = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase)
        {
            { "Customer", person }
        };

        var types = new List<TypeAnalysisResult>
        {
            new()
            {
                Name = "SomeClass",
                IsClass = true,
                // No ComponentName
                Methods =
                {
                    new MethodAnalysisResult
                    {
                        Name = "DoSomething",
                        UserActionPerson = "Customer",
                        UserActionDescription = "does something"
                    }
                }
            }
        };

        _builder.BuildRelationships(model, types, componentMap, peopleMap);

        Assert.That(model.Relationships, Is.Empty);
    }

    #endregion
}
