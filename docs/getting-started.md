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

var order = new Order { Amount = 500 };

var rules = RuleSet.For<Order>("BasicRules")
    .Add(Rule.For<Order>("Standard order")
        .When(o => o.Amount > 0)
        .Then(o => o.IsValid = true)
        .Because("Order amount is valid"))
    .Add(Rule.For<Order>("Free shipping")
        .When(o => o.Amount < 100)
        .Then(o => o.FreeShipping = true)
        .Because("Order qualifies for free shipping"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);

Console.WriteLine(result.Explain());
```

## ASP.NET Core and DI

To register `IRuleEngine` and options with `AddRuleFlow()`, see the extension package readme in the repo: `src/RuleFlow.Extensions.DependencyInjection/README.md`.

## Run the playground

From the repository root:

```bash
dotnet run --project samples/RuleFlow.ConsoleSample
```

The interactive menu runs scenarios that correspond to the documentation pages. When you change behavior or APIs, update both the relevant scenario and the matching doc page.

## Next steps

- [Rules](concepts/rules) — conditions, actions, `ThenIf`
- [Rule sets](concepts/rulesets) — groups and ordering
- [Explainability](concepts/explainability) — trees and JSON output
