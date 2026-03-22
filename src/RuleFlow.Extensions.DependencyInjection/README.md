# RuleFlow.Extensions.DependencyInjection

This project provides ASP.NET Core Dependency Injection integration for RuleFlow.

## Overview

Seamlessly integrate RuleFlow into your ASP.NET Core applications with minimal setup. Just register RuleFlow services and inject `IRuleEngine` where needed.

## Features

- **Simple Registration**: One-line setup with `AddRuleFlow()`
- **Singleton Engine**: RuleEngine is registered as a singleton for optimal performance
- **Scoped Context**: IRuleContext is scoped per request/operation
- **Optional Configuration**: Fine-tune behavior with fluent configuration
- **No ASP.NET Coupling**: Pure DI integration, works with any .NET Core app

## Installation

Add the NuGet package to your project:

```bash
dotnet add package RuleFlow.Extensions.DependencyInjection
```

## Usage

### Basic Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRuleFlow();

var app = builder.Build();
```

### With Configuration

```csharp
builder.Services.AddRuleFlow(options =>
{
    options.EnableExplainability = true;
});
```

### Consuming in Services

```csharp
public class OrderService
{
    private readonly IRuleEngine _ruleEngine;

    public OrderService(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    public RuleResult ProcessOrder(Order order)
    {
        var result = _ruleEngine.Evaluate(order, OrderRules.ApprovalRules);
        return result;
    }
}
```

### Consuming in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public IActionResult ProcessOrder([FromBody] Order order)
    {
        var result = _orderService.ProcessOrder(order);
        return Ok(result);
    }
}
```

## What Gets Registered

When you call `AddRuleFlow()`, the following services are registered:

| Service | Implementation | Lifetime | Purpose |
|---------|---|---|---|
| `IRuleEngine` | `RuleEngine` | Singleton | Evaluates rules |
| `IRuleContext` | `DefaultRuleContext` | Scoped | Provides context for rule execution |
| `RuleFlowOptions` | User configuration | Options | Configuration settings |

## Options

### RuleFlowOptions

```csharp
public class RuleFlowOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether explainability is enabled.
    /// When enabled, rule execution results include detailed explanations.
    /// Default: true
    /// </summary>
    public bool EnableExplainability { get; set; } = true;
}
```

## Design Principles

- **Minimal**: Only registers what's necessary
- **Convention-based**: Follows .NET DI conventions
- **Non-intrusive**: No middleware, filters, or magic
- **Composable**: Works alongside other services
- **Backward Compatible**: No breaking changes to RuleFlow

## See Also

- [RuleFlow.Abstractions](../RuleFlow.Abstractions/README.md) - Core interfaces
- [RuleFlow.Core](../RuleFlow.Core/README.md) - Core implementation
