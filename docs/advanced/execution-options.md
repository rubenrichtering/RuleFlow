---
title: Execution options
---

`RuleExecutionOptions<T>` configures a single evaluation. All cases below are implemented in **`ExecutionOptionsScenario.cs`**.

## `StopOnFirstMatch`

When `true`, the engine stops after the first **matching** rule (after actions for that rule). Compare with and without this flag in **CASE 1**.

## `MetadataFilter`

Provide a predicate over `IRule<T>` to include only rules whose **metadata** matches (for example a `"Category"` key). See **CASE 2**.

## `IncludeGroups`

Restrict execution to specific **group names**; other groups are skipped. See **CASE 3**.

## `EnableExplainability`

When `false`, the detailed **`Root`** tree is not built, which can reduce overhead; rule executions and applied rules can still be recorded. See **CASE 4**.

## API shape

Typical usage:

```csharp
var options = new RuleExecutionOptions<Order>
{
    StopOnFirstMatch = true,
    EnableExplainability = false
};

var result = engine.Evaluate(order, rules, options);
```

Async overloads accept the same options type.
