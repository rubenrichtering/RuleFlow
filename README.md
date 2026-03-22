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
```

## Features

- Fluent API for rules and rule sets
- Conditional chains (`ThenIf`) and rule groups
- Explainability: execution records, tree and JSON formatters
- Async conditions and actions
- Execution options (stop on first match, metadata filter, group filter)
- Optional JSON-backed definitions with a registry mapper
- ASP.NET Core: `AddRuleFlow()` for `IRuleEngine` and `IRuleContext`

## Documentation

Full documentation (GitHub Pages): [https://rubenrichtering.github.io/RuleFlow/](https://rubenrichtering.github.io/RuleFlow/)

## Repository layout

| Path | Role |
| --- | --- |
| `docs/` | Documentation source (GitHub Pages) |
| `src/RuleFlow.*` | Libraries and the `RuleFlow` metapackage |
| `samples/RuleFlow.ConsoleSample` | Interactive playground |
| `tests/RuleFlow.Core.Tests` | Unit tests |

## License

MIT
