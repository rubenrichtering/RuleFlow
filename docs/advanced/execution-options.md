---
title: Execution options
---

`RuleExecutionOptions<T>` configures a single evaluation. All cases below are implemented in **`ExecutionOptionsScenario.cs`**.

## `StopOnFirstMatch`

When `true`, the engine stops after the first **matching** rule (after actions for that rule). Compare with and without this flag in **CASE 1**.

## `MetadataFilter`

Provide a predicate over `IRule<T>` to include only rules whose **metadata** matches (for example a `"Category"` key). See **CASE 2**.

## `IncludeGroups`

Restrict execution to specific groups; other groups are skipped. See **CASE 3**.

Matching supports:
- Full hierarchical paths (recommended), for example `Parent/Child/SubChild`
- Leaf-name matching (legacy compatibility), for example `SubChild`

When full paths are used, group selection is deterministic even if multiple branches reuse the same leaf group name.

## `EnableExplainability`

When `false`, the detailed **`Root`** tree is not built, which can reduce overhead; rule executions can still be recorded. See **CASE 4**.

## API shape

```csharp
var options = new RuleExecutionOptions<Order>
{
    StopOnFirstMatch = true,
    EnableExplainability = false
};

var result = engine.Evaluate(order, rules, options);
```

Async overloads accept the same options type.
