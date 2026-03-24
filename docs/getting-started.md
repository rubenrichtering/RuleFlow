---
title: Getting started
---

## Install

Add the NuGet package (after publish, or use a project reference while developing):

```bash
dotnet add package RuleFlow
```

RuleFlow targets **.NET 10** (`net10.0`).

## Minimal example

This matches the **Basic Rules** scenario in the playground (`BasicRulesScenario.cs`).

```csharp
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

var order = new Order { Amount = 1500 };

var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);

// View execution results
Console.WriteLine(result.Explain());  // Or use result.ToDebugString() for quick tree view

// result properties: Executions (flat), Root (tree), AppliedRules (matched names), Metrics (if observability enabled)
```

## ASP.NET Core and DI

Register `IRuleEngine` and related services with `AddRuleFlow()`. See [ASP.NET Core integration](advanced/aspnet-integration).

## Observability (optional)

Enable observability to track metrics and monitor rule execution:

```csharp
var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true,
    EnableDetailedTiming = true
};

var result = engine.Evaluate(order, rules, options);

if (result.Metrics != null)
{
    Console.WriteLine($"Rules matched: {result.Metrics.RulesMatched}");
    Console.WriteLine($"Duration: {result.Metrics.TotalElapsedMilliseconds}ms");
}
```

Observability has **zero overhead when disabled** and supports custom observers for logging, monitoring, and analytics. See [Observability](advanced/observability) for details.

## Run the playground

From the repository root:

```bash
dotnet run --project samples/RuleFlow.ConsoleSample
```

The interactive menu runs scenarios that correspond to the documentation pages. When you change behavior or APIs, update both the relevant scenario and the matching doc page.

## Dynamic conditions (optional)

For rules whose logic is **authored as data** (JSON/UI/database), use structured **`ConditionNode`** trees and **`IConditionEvaluator<T>`** instead of C# lambdas in `When`. Same engine and explainability; mapping is described in [Persistence](advanced/persistence) and [Dynamic conditions](advanced/dynamic-conditions).

## Next steps

- [Rules](concepts/rules) — conditions, actions, `ThenIf`, dynamic `ConditionNode`
- [Rule sets](concepts/rulesets) — groups and ordering
- [Explainability](concepts/explainability) — execution records and formatters
- [Observability](advanced/observability) — metrics, custom observers, performance monitoring
- [Dynamic conditions](advanced/dynamic-conditions) — JSON-friendly condition trees and nested paths
