# C4SharpAnalyzer Quick Start Guide

C4SharpAnalyzer generates [C4 model](https://c4model.com/) architecture diagrams from annotated C# codebases. It analyzes your solution structure, project configurations, and code annotations to produce System Context, Container, Component, and Code diagrams.

## Table of Contents

- [Installation](#installation)
- [C4 Model Overview](#c4-model-overview)
- [System Level](#system-level)
- [Container Level](#container-level)
- [Component Level](#component-level)
- [Code Level](#code-level)
- [Relationships](#relationships)
- [Running the Tool](#running-the-tool)
- [Validation and Errors](#validation-and-errors)

## Installation

Add the C4SharpAnalyzer attributes package to projects you want to annotate:

```bash
dotnet add package C4SharpAnalyzer.Attributes
```

Install the CLI tool globally:

```bash
dotnet tool install -g C4SharpAnalyzer.Tool
```

## C4 Model Overview

C4SharpAnalyzer maps C# constructs to C4 model elements:

| C4 Level | C# Construct | Configuration |
|----------|--------------|---------------|
| **System** | Solution (`.sln`) | `*.sln.c4` YAML file |
| **Container** | Executable project | `*.csproj.c4` YAML or `.csproj` metadata |
| **Component** | Interface with `[C4Component]` | Attribute or namespace mapping |
| **Code** | Classes | Auto-generated class diagrams |

## System Level

Systems are defined at the solution level. Create a YAML file alongside your solution with the `.c4` extension:

```
MySolution.sln
MySolution.sln.c4    <-- System configuration
```

For SDK-style solutions using `.slnx`, use `MySolution.slnx.c4`.

### System Configuration Schema

```yaml
# MySolution.sln.c4

system:
  name: "My Application"
  description: "A brief description of what the system does"

# People who interact with the system
people:
  - name: Customer
    description: "End user who purchases products"
  - name: Admin
    description: "Internal user who manages the system"

# External systems that this system interacts with
externalSystems:
  - name: PaymentGateway
    description: "Third-party payment processor (e.g., Stripe)"
  - name: EmailService
    description: "Transactional email provider (e.g., SendGrid)"

# Containers that exist outside the codebase (databases, storage, etc.)
externalContainers:
  - name: Database
    technology: PostgreSQL
    description: "Primary relational data store"
  - name: Cache
    technology: Redis
    description: "Session and data caching"
  - name: FileStorage
    technology: Azure Blob Storage
    description: "User uploads and documents"

# Relationship name registry - all [C4Action] annotations must use these names
relationships:
  - "processes payments"
  - "sends emails"
  - "stores data"
  - "reads data"
  - "caches session"
```

### Key Points

- **People**: Define all actors that interact with your system. These names are referenced in `[C4UserAction]` attributes.
- **External Systems**: Third-party systems outside your control.
- **External Containers**: Infrastructure components (databases, caches, storage) that don't exist as projects in your solution.
- **Relationships**: A registry of allowed relationship names. Code annotations using `[C4Action]` must match these names exactly.

## Container Level

Containers are executable projects in your solution. Configure them using either:

1. A separate `*.csproj.c4` YAML file (recommended)
2. MSBuild properties in the `.csproj` file

### Option 1: YAML Configuration (Recommended)

Create a `.c4` file alongside your project file:

```
MyProject/
  MyProject.csproj
  MyProject.csproj.c4    <-- Container configuration
```

```yaml
# MyProject.csproj.c4

container:
  name: "Web API"
  description: "RESTful API serving the web and mobile frontends"
  technology: "ASP.NET Core 8"

# Optional: Define components by namespace instead of attributes
components:
  - namespace: MyProject.Services
    name: "Business Services"
    description: "Core business logic and orchestration"
  - namespace: MyProject.Data
    name: "Data Access"
    description: "Repository implementations and database access"
```

### Option 2: MSBuild Properties

Add C4 metadata directly to your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <!-- C4 Container metadata -->
    <C4ContainerName>Web API</C4ContainerName>
    <C4ContainerDescription>RESTful API serving the web and mobile frontends</C4ContainerDescription>
    <C4ContainerTechnology>ASP.NET Core 8</C4ContainerTechnology>
  </PropertyGroup>
</Project>
```

### Which Projects Are Containers?

By default, C4SharpAnalyzer treats the following as containers:

- Web applications (`Microsoft.NET.Sdk.Web`)
- Console applications with `<OutputType>Exe</OutputType>`
- Worker services

Class libraries are **not** containers unless explicitly configured.

## Component Level

Components represent logical groupings within a container. Define them using:

1. The `[C4Component]` attribute on interfaces (recommended)
2. Namespace mappings in the container's `.c4` file

### Option 1: Attribute-Based Components

Apply `[C4Component]` to interfaces that represent component boundaries:

```csharp
using C4SharpAnalyzer.Attributes;

[C4Component("Order Service", Description = "Handles order creation and management")]
public interface IOrderService
{
    Order CreateOrder(OrderRequest request);
    Order GetOrder(int orderId);
    void CancelOrder(int orderId);
}
```

**Component Grouping**: Any class that implements a `[C4Component]` interface is automatically grouped into that component:

```csharp
// This class belongs to the "Order Service" component
public class OrderService : IOrderService
{
    public Order CreateOrder(OrderRequest request) { /* ... */ }
    public Order GetOrder(int orderId) { /* ... */ }
    public void CancelOrder(int orderId) { /* ... */ }
}

// Helper classes used only by OrderService also belong to the component
internal class OrderValidator : IOrderService
{
    // ...
}
```

### Option 2: Namespace-Based Components

Define components by namespace in the container's `.c4` file:

```yaml
# MyProject.csproj.c4

components:
  - namespace: MyProject.Orders
    name: "Order Service"
    description: "Handles order creation and management"
  - namespace: MyProject.Inventory
    name: "Inventory Service"
    description: "Tracks product stock levels"
```

All classes within the specified namespace become part of that component.

### Component Rules

1. **No Nesting**: Components cannot contain other components. The tool reports an error if nesting is detected.
2. **Single Assignment**: A class can only belong to one component.
3. **Attribute Priority**: If a class implements a `[C4Component]` interface and is also in a namespace-mapped component, the attribute takes precedence.

## Code Level

The Code level consists of class diagrams that are automatically generated from your codebase. No additional configuration is required.

C4SharpAnalyzer generates class diagrams showing:

- Classes within each component
- Properties and methods
- Inheritance and implementation relationships
- Associations between classes

## Relationships

Relationships between elements are determined by analyzing method calls and property usage across component boundaries.

### Automatic Detection

When code in one component calls a method or accesses a property in another component, a relationship is automatically created:

```csharp
public class OrderService : IOrderService
{
    private readonly IInventoryService _inventory;

    public Order CreateOrder(OrderRequest request)
    {
        // This creates a relationship: Order Service -> Inventory Service
        var available = _inventory.CheckStock(request.ProductId);
        // ...
    }
}
```

The relationship name defaults to the method name (`CheckStock`).

### Custom Relationship Names with `[C4Action]`

Override the default relationship name using `[C4Action]`:

```csharp
[C4Component("Inventory Service")]
public interface IInventoryService
{
    [C4Action("checks product availability")]
    bool CheckStock(int productId);

    [C4Action("reserves inventory")]
    void ReserveStock(int productId, int quantity);
}
```

**Important**: The relationship name must match an entry in the `relationships` registry in your `.sln.c4` file. The tool reports an error if a name is used that isn't registered.

### User Interactions with `[C4UserAction]`

Mark methods that represent user entry points with `[C4UserAction]`:

```csharp
[C4Component("Order Service")]
public interface IOrderService
{
    [C4UserAction("Customer", "places an order")]
    Order CreateOrder(OrderRequest request);

    [C4UserAction("Admin", "cancels customer order")]
    void CancelOrder(int orderId);
}
```

This creates relationships from the named person to the component:

- `Customer` --"places an order"--> `Order Service`
- `Admin` --"cancels customer order"--> `Order Service`

**Important**: The person name must match an entry in the `people` section of your `.sln.c4` file.

### Relationships to External Systems

Mark methods that call external systems:

```csharp
public class PaymentProcessor : IPaymentProcessor
{
    [C4Action("processes payments")]  // Must match relationships registry
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        // Call to external payment gateway
    }
}
```

## Running the Tool

Generate diagrams from your solution:

```bash
# Generate all diagram levels
c4sharp analyze MySolution.sln

# Generate specific diagram levels
c4sharp analyze MySolution.sln --level context
c4sharp analyze MySolution.sln --level container
c4sharp analyze MySolution.sln --level component
c4sharp analyze MySolution.sln --level code

# Specify output directory
c4sharp analyze MySolution.sln --output ./diagrams

# Specify output format
c4sharp analyze MySolution.sln --format png
c4sharp analyze MySolution.sln --format svg
c4sharp analyze MySolution.sln --format puml
```

## Validation and Errors

C4SharpAnalyzer validates your configuration and annotations, reporting errors for:

### Configuration Errors

| Error | Description |
|-------|-------------|
| `C4001` | Missing `.sln.c4` file for solution |
| `C4002` | Invalid YAML syntax in configuration file |
| `C4003` | Missing required field in configuration |

### Component Errors

| Error | Description |
|-------|-------------|
| `C4101` | Nested components detected |
| `C4102` | Class assigned to multiple components |
| `C4103` | Component name conflicts with another component |

### Relationship Errors

| Error | Description |
|-------|-------------|
| `C4201` | `[C4Action]` uses unregistered relationship name |
| `C4202` | `[C4UserAction]` references undefined person |
| `C4203` | Circular dependency detected between components |

### Example Error Output

```
error C4201: Relationship name "sends notification" is not registered.
  --> src/Services/NotificationService.cs:15
   |
15 |     [C4Action("sends notification")]
   |              ^^^^^^^^^^^^^^^^^^^^
   |
  = help: Add "sends notification" to the relationships list in MySolution.sln.c4
  = note: Did you mean "sends notifications"?
```

## Next Steps

- See [Full Example](full-example.md) for a complete e-commerce system walkthrough
- Review the [C4 Model](https://c4model.com/) documentation for architecture concepts
