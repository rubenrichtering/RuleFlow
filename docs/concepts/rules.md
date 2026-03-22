---
title: Rules
---

A **rule** ties a **condition** to one or more **actions**, with optional metadata and execution hints. Playground: **Conditional Chains** (`ConditionalChainsScenario.cs`), **Basic Rules** (`BasicRulesScenario.cs`).

## `When` (conditions)

- `.When(Func<T, bool>)` — synchronous condition on the input.
- `.When(Func<T, IRuleContext, bool>)` — condition can read context (time, keyed items).
- `.WhenAsync(…)` — async variants for I/O or async checks.

If the condition is **false**, the rule’s actions do not run (the rule may still appear in traces depending on options).

### Dynamic conditions (`ConditionNode`)

Rules can also be driven by a **structured** condition tree (`ConditionNode`) instead of a C# lambda. That tree is **data** (JSON, database, UI): no expression parsing, no compiled code. At runtime, `RuleDefinitionMapper<T>` turns a persisted `RuleDefinition.Condition` into a `.When((input, ctx) => evaluator.Evaluate(input, node, ctx))` so execution matches the same `When` / engine pipeline as hand-written rules.

See [Dynamic conditions](../advanced/dynamic-conditions) for the model (`ConditionLeaf`, `ConditionGroup`), operators, nested property paths, and JSON examples. Playground: **`DynamicConditionsScenario.cs`**.

## `Then` (actions)

- `.Then(Action<T>)` — runs when the condition matched.
- `.ThenAsync(Func<T, Task>)` — async action.

You can chain **multiple** steps; they run in order.

## `ThenIf` (conditional steps)

Run a step only when an extra predicate holds:

```csharp
Rule.For<Order>("High amount processing")
    .When(o => o.Amount > 1000)
    .Then(o => { o.RequiresApproval = true; })
    .ThenIf(o => o.Customer?.IsPremium == true, o =>
    {
        // Premium path
    })
    .ThenIf(o => o.Customer?.IsPremium == false, o =>
    {
        // Standard path
    })
    .Because("Amount exceeds approval threshold");
```

## `Because` (reason)

`.Because("…")` records why the rule exists. It appears on `RuleExecution.Reason` and in explainability output.

## Priority and stop processing

- `.WithPriority(n)` — higher numbers run first among peers (see [Rule sets](rulesets)).
- `.StopIfMatched()` — after this rule matches and runs, processing can stop (combined with engine options; see [Execution options](../advanced/execution-options)).

## Metadata

`.WithMetadata(key, value)` attaches data you can filter on at execution time via `RuleExecutionOptions` (see **Metadata** in `ExecutionOptionsScenario.cs`).
