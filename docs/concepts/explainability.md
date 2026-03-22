---
title: Explainability
---

Explainability is a **first-class** feature: execution results can be turned into human-readable **trees**, legacy string output, or **JSON** for logs and UIs.

## `RuleResult`

`RuleEngine.Evaluate` / `EvaluateAsync` returns a `RuleResult` with:

- **Flat execution records** — what ran, matched, or skipped
- **`Root`** — when explainability is enabled, a hierarchical `RuleExecutionNode` tree
- **Helpers** such as **`Explain(...)`** and **`ToString()`** for formatted output

## Default text explain

Calling `result.Explain()` without arguments uses the default formatter (see **Basic Rules** and **Conditional Chains** scenarios).

## Tree formatter

Use `TextTreeFormatter` for structured tree output (`ExplainabilityScenario.cs`):

```csharp
Console.WriteLine(result.Explain(new TextTreeFormatter()));
```

## JSON

Use `JsonRuleResultFormatter` to serialize the result for APIs or tools:

```csharp
var json = new JsonRuleResultFormatter().Format(result);
```

## Toggle explainability tree building

Building the tree has a cost. You can disable it with `RuleExecutionOptions<T>.EnableExplainability = false` while still recording executions — see **CASE 4** in `ExecutionOptionsScenario.cs`.

## Feature parity

Anything described here is exercised in:

- `ExplainabilityScenario.cs` — tree + JSON
- `ExplainabilityRefactorScenario.cs` — refactor-oriented explainability demos

When changing explainability behavior, update these scenarios and this page together.
