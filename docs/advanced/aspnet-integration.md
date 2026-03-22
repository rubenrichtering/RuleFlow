---
title: ASP.NET Core integration
---

The **`RuleFlow.Extensions.DependencyInjection`** package registers RuleFlow with **`Microsoft.Extensions.DependencyInjection`**. Playground: **Dependency Injection** (`DependencyInjectionScenario.cs`).

## `services.AddRuleFlow()`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRuleFlow();

var app = builder.Build();
```

Optional configuration:

```csharp
builder.Services.AddRuleFlow(options =>
{
    options.EnableExplainability = true;
});
```

## What gets registered

| Service | Lifetime | Role |
| --- | --- | --- |
| `IRuleEngine` | Singleton | `RuleEngine` — evaluate rules |
| `IRuleContext` | Scoped | `DefaultRuleContext` — per-request context |
| `RuleFlowOptions` | Options | `EnableExplainability` (default `true`) |

## Consuming `IRuleEngine`

```csharp
public class OrderService
{
    private readonly IRuleEngine _ruleEngine;

    public OrderService(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    public RuleResult Process(Order order)
    {
        return _ruleEngine.Evaluate(order, OrderRules.ApprovalRules);
    }
}
```

## See also

- [Getting started](../getting-started) — packages and minimal example
