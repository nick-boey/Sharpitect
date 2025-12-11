using Sharpitect.Analysis.Analyzers;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class CodeAnalyzerTests
{
    private CodeAnalyzer _analyzer = null!;

    [SetUp]
    public void Setup()
    {
        _analyzer = new CodeAnalyzer();
    }

    [Test]
    public void AnalyzeSource_FindsInterface()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService
{
    void CreateOrder();
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types, Has.Count.EqualTo(1));
        var type = result.Types[0];
        Assert.That(type.Name, Is.EqualTo("IOrderService"));
        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.IsClass, Is.False);
    }

    [Test]
    public void AnalyzeSource_FindsClass()
    {
        var source = @"
namespace MyApp.Services;

public class OrderService
{
    public void CreateOrder() { }
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types, Has.Count.EqualTo(1));
        var type = result.Types[0];
        Assert.That(type.Name, Is.EqualTo("OrderService"));
        Assert.That(type.IsInterface, Is.False);
        Assert.That(type.IsClass, Is.True);
    }

    [Test]
    public void AnalyzeSource_FindsComponentAttribute_OnInterface()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Order Service"", Description = ""Manages orders"")]
public interface IOrderService
{
    void CreateOrder();
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types, Has.Count.EqualTo(1));
        var type = result.Types[0];
        Assert.That(type.Name, Is.EqualTo("IOrderService"));
        Assert.That(type.IsInterface, Is.True);
        Assert.That(type.ComponentName, Is.EqualTo("Order Service"));
        Assert.That(type.ComponentDescription, Is.EqualTo("Manages orders"));
    }

    [Test]
    public void AnalyzeSource_FindsComponentAttribute_WithoutDescription()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Inventory Service"")]
public interface IInventoryService
{
    void CheckStock();
}";

        var result = _analyzer.AnalyzeSource(source);

        var type = result.Types[0];
        Assert.That(type.ComponentName, Is.EqualTo("Inventory Service"));
        Assert.That(type.ComponentDescription, Is.Null);
    }

    [Test]
    public void AnalyzeSource_FindsComponentAttribute_OnClass()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Order Service"")]
public class OrderService
{
    public void CreateOrder() { }
}";

        var result = _analyzer.AnalyzeSource(source);

        var type = result.Types[0];
        Assert.That(type.IsClass, Is.True);
        Assert.That(type.ComponentName, Is.EqualTo("Order Service"));
    }

    [Test]
    public void AnalyzeSource_FindsUserActionAttribute_OnMethod()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Order Service"")]
public interface IOrderService
{
    [UserAction(""Customer"", ""places an order"")]
    void CreateOrder();
}";

        var result = _analyzer.AnalyzeSource(source);

        var method = result.Types[0].Methods[0];
        Assert.That(method.Name, Is.EqualTo("CreateOrder"));
        Assert.That(method.UserActionPerson, Is.EqualTo("Customer"));
        Assert.That(method.UserActionDescription, Is.EqualTo("places an order"));
    }

    [Test]
    public void AnalyzeSource_FindsActionAttribute_OnMethod()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Inventory Service"")]
public interface IInventoryService
{
    [Action(""reserves inventory"")]
    void ReserveStock(int productId, int quantity);
}";

        var result = _analyzer.AnalyzeSource(source);

        var method = result.Types[0].Methods[0];
        Assert.That(method.Name, Is.EqualTo("ReserveStock"));
        Assert.That(method.ActionName, Is.EqualTo("reserves inventory"));
    }

    [Test]
    public void AnalyzeSource_FindsMethodReturnType()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService
{
    Order GetOrder(int id);
    void CreateOrder();
    Task<Order> GetOrderAsync(int id);
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Methods, Has.Count.EqualTo(3));
        Assert.That(result.Types[0].Methods[0].ReturnType, Is.EqualTo("Order"));
        Assert.That(result.Types[0].Methods[1].ReturnType, Is.EqualTo("void"));
        Assert.That(result.Types[0].Methods[2].ReturnType, Is.EqualTo("Task<Order>"));
    }

    [Test]
    public void AnalyzeSource_FindsProperties()
    {
        var source = @"
namespace MyApp.Models;

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Properties, Has.Count.EqualTo(3));
        Assert.That(result.Types[0].Properties[0].Name, Is.EqualTo("Id"));
        Assert.That(result.Types[0].Properties[0].Type, Is.EqualTo("int"));
        Assert.That(result.Types[0].Properties[1].Name, Is.EqualTo("Status"));
        Assert.That(result.Types[0].Properties[1].Type, Is.EqualTo("string"));
        Assert.That(result.Types[0].Properties[2].Name, Is.EqualTo("TotalAmount"));
        Assert.That(result.Types[0].Properties[2].Type, Is.EqualTo("decimal"));
    }

    [Test]
    public void AnalyzeSource_DetectsClassImplementingInterface()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService { }

public class OrderService : IOrderService
{
    public string Status { get; set; }
    public void Process() { }
}";

        var result = _analyzer.AnalyzeSource(source);

        var classType = result.Types.First(t => t.IsClass);
        Assert.That(classType.Name, Is.EqualTo("OrderService"));
        Assert.That(classType.BaseTypes, Contains.Item("IOrderService"));
        Assert.That(classType.Properties, Has.Count.EqualTo(1));
        Assert.That(classType.Methods, Has.Count.EqualTo(1));
    }

    [Test]
    public void AnalyzeSource_DetectsMultipleBaseTypes()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService { }
public interface IDisposable { }
public class BaseService { }

public class OrderService : BaseService, IOrderService, IDisposable
{
}";

        var result = _analyzer.AnalyzeSource(source);

        var classType = result.Types.First(t => t.Name == "OrderService");
        Assert.That(classType.BaseTypes, Has.Count.EqualTo(3));
        Assert.That(classType.BaseTypes, Contains.Item("BaseService"));
        Assert.That(classType.BaseTypes, Contains.Item("IOrderService"));
        Assert.That(classType.BaseTypes, Contains.Item("IDisposable"));
    }

    [Test]
    public void AnalyzeSource_HandlesFileScopedNamespace()
    {
        var source = @"
namespace MyApp.Services;

public class OrderService { }";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Namespace, Is.EqualTo("MyApp.Services"));
    }

    [Test]
    public void AnalyzeSource_HandlesTraditionalNamespace()
    {
        var source = @"
namespace MyApp.Services
{
    public class OrderService { }
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Namespace, Is.EqualTo("MyApp.Services"));
    }

    [Test]
    public void AnalyzeSource_HandlesNoNamespace()
    {
        var source = @"
public class OrderService { }";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Namespace, Is.Null);
    }

    [Test]
    public void AnalyzeSource_FindsMultipleTypes()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService
{
    void CreateOrder();
}

public class OrderService : IOrderService
{
    public void CreateOrder() { }
}

public class OrderValidator
{
    public bool Validate() { return true; }
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types, Has.Count.EqualTo(3));
        Assert.That(result.Types.Count(t => t.IsInterface), Is.EqualTo(1));
        Assert.That(result.Types.Count(t => t.IsClass), Is.EqualTo(2));
    }

    [Test]
    public void AnalyzeSource_HandlesMethodWithNoAttributes()
    {
        var source = @"
namespace MyApp.Services;

public interface IOrderService
{
    void CreateOrder();
    Order GetOrder(int id);
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Methods, Has.Count.EqualTo(2));
        Assert.That(result.Types[0].Methods[0].ActionName, Is.Null);
        Assert.That(result.Types[0].Methods[0].UserActionPerson, Is.Null);
        Assert.That(result.Types[0].Methods[0].UserActionDescription, Is.Null);
    }

    [Test]
    public void AnalyzeSource_HandlesTypeWithNoMembers()
    {
        var source = @"
namespace MyApp.Services;

public interface IEmptyService { }";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].Methods, Is.Empty);
        Assert.That(result.Types[0].Properties, Is.Empty);
    }

    [Test]
    public void AnalyzeSource_HandlesBothActionAndUserAction_OnDifferentMethods()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[Component(""Order Service"")]
public interface IOrderService
{
    [UserAction(""Customer"", ""places an order"")]
    void CreateOrder();

    [Action(""retrieves order details"")]
    Order GetOrder(int id);
}";

        var result = _analyzer.AnalyzeSource(source);

        var methods = result.Types[0].Methods;
        Assert.That(methods, Has.Count.EqualTo(2));

        var createMethod = methods.First(m => m.Name == "CreateOrder");
        Assert.That(createMethod.UserActionPerson, Is.EqualTo("Customer"));
        Assert.That(createMethod.UserActionDescription, Is.EqualTo("places an order"));
        Assert.That(createMethod.ActionName, Is.Null);

        var getMethod = methods.First(m => m.Name == "GetOrder");
        Assert.That(getMethod.ActionName, Is.EqualTo("retrieves order details"));
        Assert.That(getMethod.UserActionPerson, Is.Null);
    }

    [Test]
    public void AnalyzeSource_HandlesComponentAttributeAttribute_FullName()
    {
        var source = @"
using Sharpitect.Attributes;

namespace MyApp.Services;

[ComponentAttribute(""Order Service"")]
public interface IOrderService
{
    void CreateOrder();
}";

        var result = _analyzer.AnalyzeSource(source);

        Assert.That(result.Types[0].ComponentName, Is.EqualTo("Order Service"));
    }
}
