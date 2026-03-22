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

Console.WriteLine(result.Explain());
```

## ASP.NET Core and DI

Register `IRuleEngine` and related services with `AddRuleFlow()`. See [ASP.NET Core integration](advanced/aspnet-integration).

## Run the playground

From the repository root:

```bash
dotnet run --project samples/RuleFlow.ConsoleSample
```

The interactive menu runs scenarios that correspond to the documentation pages. When you change behavior or APIs, update both the relevant scenario and the matching doc page.

## Next steps

- [Rules](concepts/rules) — conditions, actions, `ThenIf`
- [Rule sets](concepts/rulesets) — groups and ordering
- [Explainability](concepts/explainability) — execution records and formatters
