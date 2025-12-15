using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Model;
using Sharpitect.Analysis.Model.Code;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class SolutionAnalyzerTests
{
    [Test]
    public void Analyze_BuildsSystemFromConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
              description: "A test system"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.Systems, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(model.Systems[0].Name, Is.EqualTo("Test System"));
            Assert.That(model.Systems[0].Description, Is.EqualTo("A test system"));
        });
    }

    [Test]
    public void Analyze_BuildsPeopleFromConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"

            people:
              - name: Customer
                description: "Online shopper"
              - name: Admin
                description: "System administrator"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.People, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(model.People[0].Name, Is.EqualTo("Customer"));
            Assert.That(model.People[0].Description, Is.EqualTo("Online shopper"));
            Assert.That(model.People[1].Name, Is.EqualTo("Admin"));
        });
    }

    [Test]
    public void Analyze_BuildsExternalSystemsFromConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"

            externalSystems:
              - name: PaymentGateway
                description: "Stripe payment processor"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.ExternalSystems, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(model.ExternalSystems[0].Name, Is.EqualTo("PaymentGateway"));
            Assert.That(model.ExternalSystems[0].Description, Is.EqualTo("Stripe payment processor"));
        });
    }

    [Test]
    public void Analyze_BuildsExternalContainersFromConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"

            externalContainers:
              - name: Database
                technology: PostgreSQL
                description: "Main database"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.ExternalContainers, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(model.ExternalContainers[0].Name, Is.EqualTo("Database"));
            Assert.That(model.ExternalContainers[0].Technology, Is.EqualTo("PostgreSQL"));
            Assert.That(model.ExternalContainers[0].Description, Is.EqualTo("Main database"));
        });
    }

    [Test]
    public void Analyze_BuildsContainerFromProject()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");
        sourceProvider.AddYaml("Project/Project.csproj.c4",
            """
            container:
              name: "Web API"
              description: "RESTful API"
              technology: "ASP.NET Core 8"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.Systems[0].Containers, Has.Count.EqualTo(1));
        var container = model.Systems[0].Containers[0];
        Assert.Multiple(() =>
        {
            Assert.That(container.Name, Is.EqualTo("Web API"));
            Assert.That(container.Description, Is.EqualTo("RESTful API"));
            Assert.That(container.Technology, Is.EqualTo("ASP.NET Core 8"));
        });
    }

    [Test]
    public void Analyze_BuildsComponentFromAttribute()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/IOrderService.cs");
        sourceProvider.AddSource("Project/IOrderService.cs",
            """
            using Sharpitect.Attributes;

            namespace Project.Services;

            [Component("Order Service", Description = "Handles orders")]
            public interface IOrderService
            {
                void CreateOrder();
            }
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        var container = model.Systems[0].Containers[0];
        Assert.That(container.Components, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(container.Components[0].Name, Is.EqualTo("Order Service"));
            Assert.That(container.Components[0].Description, Is.EqualTo("Handles orders"));
        });
    }

    [Test]
    public void Analyze_MapsClassToComponentViaInterface()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/OrderService.cs");
        sourceProvider.AddSource("Project/OrderService.cs",
            """
            using Sharpitect.Attributes;

            namespace Project.Services;

            [Component("Order Service")]
            public interface IOrderService
            {
                void CreateOrder();
            }

            public class OrderService : IOrderService
            {
                public void CreateOrder() { }
            }
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        var component = model.Systems[0].Containers[0].Components[0];
        Assert.That(component.CodeElements, Has.Count.EqualTo(1));
        Assert.That(component.CodeElements[0].Name, Is.EqualTo("OrderService"));
    }

    [Test]
    public void Analyze_BuildsUserActionRelationship()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"

            people:
              - name: Customer
                description: "Online shopper"
            """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/IOrderService.cs");
        sourceProvider.AddSource("Project/IOrderService.cs",
            """
            using Sharpitect.Attributes;

            namespace Project.Services;

            [Component("Order Service")]
            public interface IOrderService
            {
                [UserAction("Customer", "places an order")]
                void CreateOrder();
            }
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.Relationships, Has.Count.EqualTo(1));
        var relationship = model.Relationships[0];
        Assert.Multiple(() =>
        {
            Assert.That(relationship.Source.Name, Is.EqualTo("Customer"));
            Assert.That(relationship.Destination.Name, Is.EqualTo("Order Service"));
            Assert.That(relationship.Description, Is.EqualTo("places an order"));
        });
    }

    [Test]
    public void Analyze_BuildsComponentFromNamespaceMapping()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");
        sourceProvider.AddYaml("Project/Project.csproj.c4",
            """
            container:
              name: "Web API"

            components:
              - namespace: Project.Services
                name: "Business Services"
                description: "Core business logic"
            """);
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/OrderService.cs");
        sourceProvider.AddSource("Project/OrderService.cs", """

                                                            namespace Project.Services;

                                                            public class OrderService
                                                            {
                                                                public void CreateOrder() { }
                                                            }

                                                            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        var component = model.Systems[0].Containers[0].Components[0];
        Assert.Multiple(() =>
        {
            Assert.That(component.Name, Is.EqualTo("Business Services"));
            Assert.That(component.CodeElements, Has.Count.EqualTo(1));
        });
        Assert.That(component.CodeElements[0].Name, Is.EqualTo("OrderService"));
    }

    [Test]
    public void Analyze_BuildsCompleteModel_FromInMemorySources()
    {
        var sourceProvider = new InMemorySourceProvider();

        // Add solution-level config
        sourceProvider.AddYaml("Solution.sln.c4", """

                                                  system:
                                                    name: "ShopEasy"
                                                    description: "E-commerce platform"

                                                  people:
                                                    - name: Customer
                                                      description: "End user who purchases products"
                                                    - name: Admin
                                                      description: "Internal user who manages the system"

                                                  externalSystems:
                                                    - name: PaymentGateway
                                                      description: "Third-party payment processor"

                                                  externalContainers:
                                                    - name: Database
                                                      technology: PostgreSQL
                                                      description: "Primary relational data store"

                                                  relationships:
                                                    - start: "Payment Service"
                                                      action: "processes payment"
                                                      end: "PaymentGateway"
                                                    - start: "Data Access"
                                                      action: "stores data"
                                                      end: "Database"

                                                  """);

        // Add API project
        sourceProvider.AddProject("Solution.sln", "Api/Api.csproj");
        sourceProvider.SetProjectAsExecutable("Api/Api.csproj");
        sourceProvider.AddYaml("Api/Api.csproj.c4", """

                                                    container:
                                                      name: "Web API"
                                                      description: "RESTful API serving the web and mobile frontends"
                                                      technology: "ASP.NET Core 8"

                                                    """);
        sourceProvider.AddSourceFile("Api/Api.csproj", "Api/Services/IOrderService.cs");
        sourceProvider.AddSource("Api/Services/IOrderService.cs", """

                                                                  using Sharpitect.Attributes;

                                                                  namespace Api.Services;

                                                                  [Component("Order Service", Description = "Handles order creation and management")]
                                                                  public interface IOrderService
                                                                  {
                                                                      [UserAction("Customer", "places an order")]
                                                                      Order CreateOrder(OrderRequest request);

                                                                      [UserAction("Admin", "cancels customer order")]
                                                                      void CancelOrder(int orderId);
                                                                  }

                                                                  public class Order
                                                                  {
                                                                      public int Id { get; set; }
                                                                      public string Status { get; set; }
                                                                      public decimal Total { get; set; }
                                                                  }

                                                                  public class OrderRequest
                                                                  {
                                                                      public int ProductId { get; set; }
                                                                      public int Quantity { get; set; }
                                                                  }

                                                                  public class OrderService : IOrderService
                                                                  {
                                                                      public Order CreateOrder(OrderRequest request)
                                                                      {
                                                                          return new Order();
                                                                      }

                                                                      public void CancelOrder(int orderId) { }
                                                                  }

                                                                  """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        // Verify system
        Assert.That(model.Systems, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(model.Systems[0].Name, Is.EqualTo("ShopEasy"));

            // Verify people
            Assert.That(model.People, Has.Count.EqualTo(2));
        });
        Assert.That(model.People.Select(p => p.Name), Contains.Item("Customer"));
        Assert.Multiple(() =>
        {
            Assert.That(model.People.Select(p => p.Name), Contains.Item("Admin"));

            // Verify external systems
            Assert.That(model.ExternalSystems, Has.Count.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(model.ExternalSystems[0].Name, Is.EqualTo("PaymentGateway"));

            // Verify external containers
            Assert.That(model.ExternalContainers, Has.Count.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(model.ExternalContainers[0].Name, Is.EqualTo("Database"));
            Assert.That(model.ExternalContainers[0].Technology, Is.EqualTo("PostgreSQL"));
        });

        // Verify container
        var container = model.Systems[0].Containers[0];
        Assert.Multiple(() =>
        {
            Assert.That(container.Name, Is.EqualTo("Web API"));
            Assert.That(container.Technology, Is.EqualTo("ASP.NET Core 8"));

            // Verify component
            Assert.That(container.Components, Has.Count.EqualTo(1));
        });
        var component = container.Components[0];
        Assert.Multiple(() =>
        {
            Assert.That(component.Name, Is.EqualTo("Order Service"));
            Assert.That(component.Description, Is.EqualTo("Handles order creation and management"));

            // Verify code elements (should have OrderService class)
            Assert.That(component.CodeElements, Has.Count.EqualTo(1));
        });
        var classCode = component.CodeElements[0] as ClassCode;
        Assert.That(classCode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(classCode!.Name, Is.EqualTo("OrderService"));

            // Verify relationships
            Assert.That(model.Relationships, Has.Count.EqualTo(2));
        });
        var customerRelationship = model.Relationships.First(r => r.Source.Name == "Customer");
        Assert.Multiple(() =>
        {
            Assert.That(customerRelationship.Description, Is.EqualTo("places an order"));
            Assert.That(customerRelationship.Destination.Name, Is.EqualTo("Order Service"));
        });

        var adminRelationship = model.Relationships.First(r => r.Source.Name == "Admin");
        Assert.That(adminRelationship.Description, Is.EqualTo("cancels customer order"));
    }

    [Test]
    public void Analyze_HandlesMultipleProjects()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);

        // Add first project
        sourceProvider.AddProject("Solution.sln", "Api/Api.csproj");
        sourceProvider.SetProjectAsExecutable("Api/Api.csproj");
        sourceProvider.AddYaml("Api/Api.csproj.c4",
            """
            container:
              name: "API"
            """);
        sourceProvider.AddSourceFile("Api/Api.csproj", "Api/Service.cs");
        sourceProvider.AddSource("Api/Service.cs",
            """
            using Sharpitect.Attributes;

            [Component("API Service")]
            public interface IApiService { }
            """);

        // Add second project
        sourceProvider.AddProject("Solution.sln", "Worker/Worker.csproj");
        sourceProvider.SetProjectAsExecutable("Worker/Worker.csproj");
        sourceProvider.AddYaml("Worker/Worker.csproj.c4",
            """
            container:
              name: "Worker"
            """);
        sourceProvider.AddSourceFile("Worker/Worker.csproj", "Worker/Service.cs");
        sourceProvider.AddSource("Worker/Service.cs",
            """
            using Sharpitect.Attributes;

            [Component("Worker Service")]
            public interface IWorkerService { }
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.Systems[0].Containers, Has.Count.EqualTo(2));
        Assert.That(model.Systems[0].Containers.Select(c => c.Name), Contains.Item("API"));
        Assert.That(model.Systems[0].Containers.Select(c => c.Name), Contains.Item("Worker"));
    }

    [Test]
    public void Analyze_HandlesNoConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        // No YAML config added

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        Assert.That(model.Systems, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(model.Systems[0].Name, Is.EqualTo("Solution"));
            Assert.That(model.People, Is.Empty);
            Assert.That(model.ExternalSystems, Is.Empty);
            Assert.That(model.ExternalContainers, Is.Empty);
        });
    }

    [Test]
    public void Analyze_HandlesMultipleSourceFilesInProject()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4", """

                                                  system:
                                                    name: "Test System"

                                                  """);
        sourceProvider.AddProject("Solution.sln", "Project/Project.csproj");
        sourceProvider.SetProjectAsExecutable("Project/Project.csproj");

        // Add multiple source files
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/IOrderService.cs");
        sourceProvider.AddSourceFile("Project/Project.csproj", "Project/IInventoryService.cs");

        sourceProvider.AddSource("Project/IOrderService.cs", """

                                                             using Sharpitect.Attributes;

                                                             [Component("Order Service")]
                                                             public interface IOrderService { }

                                                             """);
        sourceProvider.AddSource("Project/IInventoryService.cs", """

                                                                 using Sharpitect.Attributes;

                                                                 [Component("Inventory Service")]
                                                                 public interface IInventoryService { }

                                                                 """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        var container = model.Systems[0].Containers[0];
        Assert.That(container.Components, Has.Count.EqualTo(2));
        Assert.That(container.Components.Select(c => c.Name), Contains.Item("Order Service"));
        Assert.That(container.Components.Select(c => c.Name), Contains.Item("Inventory Service"));
    }

    [Test]
    public void Analyze_ContainerUsesProjectNameWhenNoConfig()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4", """

                                                  system:
                                                    name: "Test System"

                                                  """);
        sourceProvider.AddProject("Solution.sln", "MyProject/MyProject.csproj");
        sourceProvider.SetProjectAsExecutable("MyProject/MyProject.csproj");
        // No container config

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        var container = model.Systems[0].Containers[0];
        Assert.That(container.Name, Is.EqualTo("MyProject"));
    }

    [Test]
    public void Analyze_ExcludesLibraryProjects()
    {
        var sourceProvider = new InMemorySourceProvider();
        sourceProvider.AddYaml("Solution.sln.c4",
            """
            system:
              name: "Test System"
            """);

        // Add an executable project
        sourceProvider.AddProject("Solution.sln", "Api/Api.csproj");
        sourceProvider.SetProjectAsExecutable("Api/Api.csproj");
        sourceProvider.AddYaml("Api/Api.csproj.c4",
            """
            container:
              name: "API"
            """);

        // Add a library project (not executable)
        sourceProvider.AddProject("Solution.sln", "Core/Core.csproj");
        // Note: NOT calling SetProjectAsExecutable for Core
        sourceProvider.AddYaml("Core/Core.csproj.c4",
            """
            container:
              name: "Core Library"
            """);

        var analyzer = new SolutionAnalyzer(sourceProvider);
        var model = analyzer.Analyze("Solution.sln");

        // Only the executable project should be a container
        Assert.That(model.Systems[0].Containers, Has.Count.EqualTo(1));
        Assert.That(model.Systems[0].Containers[0].Name, Is.EqualTo("API"));
    }
}