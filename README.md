# RuleFlow

RuleFlow is a lightweight, developer-friendly rule engine for .NET.

It focuses on simplicity, performance, and explainability.

## Quick example

```csharp
var order = new Order { Amount = 1500 };

var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);

// Async-first path for server workloads:
// var result = await engine.EvaluateAsync(order, rules);
```

## Features

- Fluent API
- Conditional chains (`ThenIf`)
- Explainability (execution records, tree and JSON formatters)
- **Observability** — optional metrics and custom observer callbacks (zero overhead when disabled)
- Async support (conditions and actions)
- Rule groups
- **Dynamic conditions** — structured, JSON-friendly `ConditionNode` trees (no expression parsing)

## Dynamic conditions

RuleFlow supports **user-defined rules** via structured conditions (JSON, database, or UI): `ConditionLeaf` / `ConditionGroup`, pluggable operators, and dotted **nested property** paths (e.g. `Customer.Name`) resolved with cached reflection.

See the full documentation: [Dynamic conditions](https://rubenrichtering.github.io/RuleFlow/advanced/dynamic-conditions).

## Documentation

Full documentation (GitHub Pages): [https://rubenrichtering.github.io/RuleFlow/](https://rubenrichtering.github.io/RuleFlow/)

## Changelog

See what's new and upcoming:
[CHANGELOG.md](CHANGELOG.md)

## Repository layout

| Path | Role |
| --- | --- |
| `docs/` | Documentation source (GitHub Pages) |
| `src/RuleFlow.*` | Libraries and the `RuleFlow` metapackage |
| `samples/RuleFlow.ConsoleSample` | Interactive playground |
| `tests/RuleFlow.Core.Tests` | Unit tests |

## License

MIT
