---
title: Rules
---

A **rule** ties a **condition** to one or more **actions**, with optional metadata and execution hints.

## Naming and reasons

Create a rule with `Rule.For<T>("Name")`. Use `.Because("…")` to record why the rule exists; this feeds explainability output.

## Conditions: `When` / `WhenAsync`

- `.When(Func<T, bool>)` — synchronous condition on the input.
- `.When(Func<T, IRuleContext, bool>)` — condition can read context (time, keyed items).
- `.WhenAsync(…)` — async variants for I/O or async checks.

If the condition is **false**, the rule’s actions do not run (the rule may still appear in traces depending on options).

## Actions: `Then` / `ThenAsync`

- `.Then(Action<T>)` — run when the condition matched.
- `.ThenAsync(Func<T, Task>)` — async action.

You can chain **multiple** steps; they run in order.

## Conditional steps: `ThenIf`

Run a step only when an extra predicate holds (see **Conditional Chains** in `ConditionalChainsScenario.cs`):

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

## Priority and stop processing

- `.WithPriority(n)` — higher numbers run first among peers (see [Rule sets](rulesets)).
- `.StopIfMatched()` — after this rule matches and runs, processing can stop (combined with engine options; see [Execution options](../advanced/execution-options)).

## Metadata

`.WithMetadata(key, value)` attaches data you can filter on at execution time via `RuleExecutionOptions` (see **Metadata** case in `ExecutionOptionsScenario.cs`).
