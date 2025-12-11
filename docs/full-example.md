# Full Example: ShopEasy E-Commerce Platform

This example demonstrates Sharpitect configuration for a complete e-commerce system called "ShopEasy".

## Solution Structure

```
ShopEasy/
├── ShopEasy.sln
├── ShopEasy.sln.c4                    # System configuration
├── src/
│   ├── ShopEasy.WebApi/
│   │   ├── ShopEasy.WebApi.csproj
│   │   ├── ShopEasy.WebApi.csproj.c4  # Container configuration
│   │   └── Controllers/
│   ├── ShopEasy.Worker/
│   │   ├── ShopEasy.Worker.csproj
│   │   ├── ShopEasy.Worker.csproj.c4  # Container configuration
│   │   └── Jobs/
│   └── ShopEasy.Core/
│       ├── ShopEasy.Core.csproj       # Shared library (not a container)
│       ├── Interfaces/
│       └── Services/
└── tests/
    └── ShopEasy.Tests/
```

## System Level: ShopEasy.sln.c4

```yaml
# ShopEasy.sln.c4
# Defines the system context: people, external systems, and infrastructure

system:
  name: "ShopEasy"
  description: "E-commerce platform for online retail sales"

people:
  - name: Customer
    description: "Online shopper who browses and purchases products"
  - name: Admin
    description: "Internal staff who manage products, orders, and inventory"
  - name: Support
    description: "Customer service representative handling inquiries"

externalSystems:
  - name: PaymentGateway
    description: "Stripe - processes credit card and digital wallet payments"
  - name: EmailService
    description: "SendGrid - sends transactional and marketing emails"
  - name: ShippingProvider
    description: "FedEx/UPS API - calculates rates and creates shipping labels"
  - name: AnalyticsPlatform
    description: "Google Analytics - tracks user behavior and conversions"

externalContainers:
  - name: Database
    technology: PostgreSQL 15
    description: "Primary relational database storing orders, products, and users"
  - name: Cache
    technology: Redis 7
    description: "Session storage and frequently accessed data caching"
  - name: SearchIndex
    technology: Elasticsearch 8
    description: "Full-text product search and filtering"
  - name: MessageQueue
    technology: RabbitMQ
    description: "Async message broker for background job processing"
  - name: FileStorage
    technology: AWS S3
    description: "Product images and user uploads"

# Relationship name registry
# All [Action] attributes must use one of these exact names
relationships:
  # Payment related
  - "processes payment"
  - "refunds payment"

  # Email related
  - "sends order confirmation"
  - "sends shipping notification"
  - "sends password reset"
  - "sends marketing email"

  # Shipping related
  - "calculates shipping rates"
  - "creates shipping label"
  - "tracks shipment"

  # Data storage
  - "stores data"
  - "reads data"
  - "caches data"
  - "invalidates cache"

  # Search
  - "indexes product"
  - "searches products"

  # Messaging
  - "publishes message"
  - "consumes message"

  # Files
  - "uploads file"
  - "downloads file"

  # Analytics
  - "tracks event"
```

## Container Level: Web API

### ShopEasy.WebApi.csproj.c4

```yaml
# ShopEasy.WebApi.csproj.c4
# Configuration for the main API container

container:
  name: "Web API"
  description: "RESTful API serving web and mobile clients"
  technology: "ASP.NET Core 8, C#"

# Components defined by namespace
# These supplement [Component] attributes in code
components:
  - namespace: ShopEasy.WebApi.Controllers
    name: "API Controllers"
    description: "HTTP endpoints and request handling"
```

## Container Level: Background Worker

### ShopEasy.Worker.csproj.c4

```yaml
# ShopEasy.Worker.csproj.c4
# Configuration for the background processing container

container:
  name: "Background Worker"
  description: "Processes async jobs from the message queue"
  technology: ".NET 8 Worker Service"

components:
  - namespace: ShopEasy.Worker.Jobs
    name: "Job Handlers"
    description: "Message consumers and background task processors"
```

## Component Definitions

### Order Service

```csharp
// src/ShopEasy.Core/Interfaces/IOrderService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

/// <summary>
/// Handles order lifecycle from creation to fulfillment.
/// </summary>
[Component("Order Service", Description = "Manages order creation, updates, and fulfillment workflow")]
public interface IOrderService
{
    /// <summary>
    /// Creates a new order from the customer's cart.
    /// </summary>
    [UserAction("Customer", "places order")]
    Task<Order> CreateOrderAsync(CreateOrderRequest request);

    /// <summary>
    /// Retrieves order details for display.
    /// </summary>
    [UserAction("Customer", "views order history")]
    Task<Order> GetOrderAsync(int orderId);

    /// <summary>
    /// Cancels an order before it ships.
    /// </summary>
    [UserAction("Customer", "cancels order")]
    Task CancelOrderAsync(int orderId);

    /// <summary>
    /// Admin updates order status during fulfillment.
    /// </summary>
    [UserAction("Admin", "updates order status")]
    Task UpdateOrderStatusAsync(int orderId, OrderStatus status);

    /// <summary>
    /// Admin views all orders with filtering.
    /// </summary>
    [UserAction("Admin", "manages orders")]
    Task<PagedResult<Order>> GetOrdersAsync(OrderFilter filter);
}
```

### Order Service Implementation

```csharp
// src/ShopEasy.Core/Services/OrderService.cs

namespace ShopEasy.Core.Services;

/// <summary>
/// Implementation of order management.
/// Automatically grouped into "Order Service" component because it implements IOrderService.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IInventoryService _inventory;
    private readonly IPaymentService _payment;
    private readonly INotificationService _notifications;
    private readonly IOrderRepository _repository;

    public OrderService(
        IInventoryService inventory,
        IPaymentService payment,
        INotificationService notifications,
        IOrderRepository repository)
    {
        _inventory = inventory;
        _payment = payment;
        _notifications = notifications;
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Check inventory - creates relationship to Inventory Service
        foreach (var item in request.Items)
        {
            var available = await _inventory.CheckStockAsync(item.ProductId, item.Quantity);
            if (!available)
                throw new InsufficientStockException(item.ProductId);
        }

        // Process payment - creates relationship to Payment Service
        var paymentResult = await _payment.ProcessPaymentAsync(new PaymentRequest
        {
            Amount = request.Total,
            CustomerId = request.CustomerId,
            PaymentMethod = request.PaymentMethod
        });

        // Create the order
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items,
            PaymentId = paymentResult.TransactionId,
            Status = OrderStatus.Confirmed
        };

        await _repository.SaveAsync(order);

        // Reserve inventory
        foreach (var item in request.Items)
        {
            await _inventory.ReserveStockAsync(item.ProductId, item.Quantity);
        }

        // Send confirmation - creates relationship to Notification Service
        await _notifications.SendOrderConfirmationAsync(order);

        return order;
    }

    // ... other methods
}
```

### Inventory Service

```csharp
// src/ShopEasy.Core/Interfaces/IInventoryService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Inventory Service", Description = "Tracks product stock levels and reservations")]
public interface IInventoryService
{
    [Action("checks product availability")]
    Task<bool> CheckStockAsync(int productId, int quantity);

    [Action("reserves inventory")]
    Task ReserveStockAsync(int productId, int quantity);

    [Action("releases inventory")]
    Task ReleaseStockAsync(int productId, int quantity);

    [UserAction("Admin", "updates stock levels")]
    Task UpdateStockAsync(int productId, int newQuantity);

    [UserAction("Admin", "views inventory report")]
    Task<InventoryReport> GetInventoryReportAsync();
}
```

### Payment Service

```csharp
// src/ShopEasy.Core/Interfaces/IPaymentService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Payment Service", Description = "Handles payment processing and refunds")]
public interface IPaymentService
{
    /// <summary>
    /// Processes a payment through the external payment gateway.
    /// </summary>
    [Action("processes payment")]
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);

    /// <summary>
    /// Issues a refund for a previous payment.
    /// </summary>
    [Action("refunds payment")]
    Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount);
}
```

### Payment Service Implementation (External System Integration)

```csharp
// src/ShopEasy.Core/Services/StripePaymentService.cs

namespace ShopEasy.Core.Services;

/// <summary>
/// Payment service implementation using Stripe.
/// The [Action] attributes on the interface methods indicate
/// these operations interact with the external PaymentGateway system.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly IStripeClient _stripeClient;

    public StripePaymentService(IStripeClient stripeClient)
    {
        _stripeClient = stripeClient;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // This call goes to the external PaymentGateway system
        var charge = await _stripeClient.CreateChargeAsync(new ChargeCreateOptions
        {
            Amount = (long)(request.Amount * 100),
            Currency = "usd",
            Customer = request.CustomerId
        });

        return new PaymentResult
        {
            Success = charge.Status == "succeeded",
            TransactionId = charge.Id
        };
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        var refund = await _stripeClient.CreateRefundAsync(new RefundCreateOptions
        {
            Charge = transactionId,
            Amount = (long)(amount * 100)
        });

        return new RefundResult
        {
            Success = refund.Status == "succeeded",
            RefundId = refund.Id
        };
    }
}
```

### Notification Service

```csharp
// src/ShopEasy.Core/Interfaces/INotificationService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Notification Service", Description = "Sends emails and push notifications to users")]
public interface INotificationService
{
    [Action("sends order confirmation")]
    Task SendOrderConfirmationAsync(Order order);

    [Action("sends shipping notification")]
    Task SendShippingNotificationAsync(Order order, TrackingInfo tracking);

    [Action("sends password reset")]
    Task SendPasswordResetAsync(string email, string resetToken);
}
```

### Product Service

```csharp
// src/ShopEasy.Core/Interfaces/IProductService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Product Catalog", Description = "Manages product information and search")]
public interface IProductService
{
    [UserAction("Customer", "browses products")]
    Task<PagedResult<Product>> GetProductsAsync(ProductFilter filter);

    [UserAction("Customer", "views product details")]
    Task<ProductDetail> GetProductAsync(int productId);

    [Action("searches products")]
    Task<SearchResult<Product>> SearchProductsAsync(string query);

    [UserAction("Admin", "creates product")]
    Task<Product> CreateProductAsync(CreateProductRequest request);

    [UserAction("Admin", "updates product")]
    Task UpdateProductAsync(int productId, UpdateProductRequest request);
}
```

### Data Access Components

```csharp
// src/ShopEasy.Core/Interfaces/IOrderRepository.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Data Access", Description = "Repository layer for database operations")]
public interface IOrderRepository
{
    [Action("stores data")]
    Task SaveAsync(Order order);

    [Action("reads data")]
    Task<Order> GetByIdAsync(int orderId);

    [Action("reads data")]
    Task<List<Order>> GetByCustomerIdAsync(int customerId);
}
```

### Cache Service

```csharp
// src/ShopEasy.Core/Interfaces/ICacheService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Cache Service", Description = "Manages Redis cache for performance optimization")]
public interface ICacheService
{
    [Action("caches data")]
    Task SetAsync<T>(string key, T value, TimeSpan expiration);

    [Action("reads data")]
    Task<T?> GetAsync<T>(string key);

    [Action("invalidates cache")]
    Task RemoveAsync(string key);
}
```

### Background Job: Order Fulfillment

```csharp
// src/ShopEasy.Worker/Jobs/OrderFulfillmentJob.cs

using Sharpitect.Attributes;

namespace ShopEasy.Worker.Jobs;

[Component("Order Fulfillment", Description = "Processes order shipping and delivery")]
public interface IOrderFulfillmentJob
{
    [Action("consumes message")]
    Task ProcessOrderAsync(OrderCreatedMessage message);
}

public class OrderFulfillmentJob : IOrderFulfillmentJob
{
    private readonly IShippingService _shipping;
    private readonly INotificationService _notifications;
    private readonly IOrderRepository _repository;

    public OrderFulfillmentJob(
        IShippingService shipping,
        INotificationService notifications,
        IOrderRepository repository)
    {
        _shipping = shipping;
        _notifications = notifications;
        _repository = repository;
    }

    public async Task ProcessOrderAsync(OrderCreatedMessage message)
    {
        var order = await _repository.GetByIdAsync(message.OrderId);

        // Create shipping label - calls external ShippingProvider
        var tracking = await _shipping.CreateShippingLabelAsync(order);

        // Update order with tracking info
        order.TrackingNumber = tracking.TrackingNumber;
        order.Status = OrderStatus.Shipped;
        await _repository.SaveAsync(order);

        // Notify customer
        await _notifications.SendShippingNotificationAsync(order, tracking);
    }
}
```

### Shipping Service

```csharp
// src/ShopEasy.Core/Interfaces/IShippingService.cs

using Sharpitect.Attributes;

namespace ShopEasy.Core.Interfaces;

[Component("Shipping Service", Description = "Integrates with shipping carriers for rates and labels")]
public interface IShippingService
{
    [Action("calculates shipping rates")]
    Task<List<ShippingRate>> GetRatesAsync(ShippingRequest request);

    [Action("creates shipping label")]
    Task<TrackingInfo> CreateShippingLabelAsync(Order order);

    [Action("tracks shipment")]
    Task<ShipmentStatus> GetTrackingStatusAsync(string trackingNumber);
}
```

## Generated Diagrams

Running Sharpitect on this solution produces the following diagrams:

### System Context Diagram

Shows ShopEasy and its relationships with:

- **People**: Customer, Admin, Support
- **External Systems**: PaymentGateway, EmailService, ShippingProvider, AnalyticsPlatform

### Container Diagram

Shows the containers within ShopEasy:

- **Web API** (ASP.NET Core 8)
- **Background Worker** (.NET 8 Worker Service)
- **Database** (PostgreSQL 15)
- **Cache** (Redis 7)
- **SearchIndex** (Elasticsearch 8)
- **MessageQueue** (RabbitMQ)
- **FileStorage** (AWS S3)

### Component Diagram (Web API)

Shows components within the Web API container:

- API Controllers
- Order Service
- Inventory Service
- Payment Service
- Product Catalog
- Notification Service
- Shipping Service
- Cache Service
- Data Access

With relationships like:

- `Order Service` --"checks product availability"--> `Inventory Service`
- `Order Service` --"processes payment"--> `Payment Service`
- `Order Service` --"sends order confirmation"--> `Notification Service`
- `Customer` --"places order"--> `Order Service`
- `Admin` --"manages orders"--> `Order Service`

### Component Diagram (Background Worker)

Shows components within the Background Worker:

- Job Handlers
- Order Fulfillment

With relationships to shared components and external systems.

## Running the Analysis

```bash
# Generate all diagrams
c4sharp analyze ShopEasy.sln --output ./docs/diagrams

# Generate only the system context
c4sharp analyze ShopEasy.sln --level context --output ./docs/diagrams

# Generate PNG images
c4sharp analyze ShopEasy.sln --format png --output ./docs/diagrams

# Validate configuration without generating diagrams
c4sharp validate ShopEasy.sln
```

## Common Patterns

### Pattern: Service calls External System

When a component calls an external system (defined in `.sln.c4`), use `[Action]` with a registered relationship name:

```csharp
[Component("Payment Service")]
public interface IPaymentService
{
    [Action("processes payment")]  // Links to PaymentGateway
    Task<PaymentResult> ProcessAsync(PaymentRequest request);
}
```

### Pattern: User Entry Point

Mark public-facing methods with `[UserAction]` to show user interactions:

```csharp
[UserAction("Customer", "searches for products")]
Task<SearchResult> SearchAsync(string query);
```

### Pattern: Internal Component Communication

Method calls between components automatically create relationships. Use `[Action]` to provide meaningful names:

```csharp
// In IInventoryService
[Action("reserves inventory")]
Task ReserveStockAsync(int productId, int quantity);
```

### Pattern: Async Processing via Message Queue

For components that communicate via messages:

```csharp
// Publisher
[Action("publishes message")]
Task PublishOrderCreatedAsync(Order order);

// Consumer
[Action("consumes message")]
Task HandleOrderCreatedAsync(OrderCreatedMessage message);
```
