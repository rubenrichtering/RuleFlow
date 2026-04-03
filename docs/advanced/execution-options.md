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

> **Debug DTO fallback:** `ToDebugView()` and `ToDebugJson()` automatically fall back to the flat `Executions` list when `Root` is `null`, so both methods still produce valid output even when explainability is disabled.

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

---

## AI Condition Options

> These options require `EnableAiConditions = true`. See [AI Conditions](ai-conditions.md) for full documentation.

### `EnableAiConditions` (default: `false`)

Enables evaluation of `.WhenAI(...)` conditions. When `false`, all AI conditions resolve to `false` with zero overhead.

### `AiTimeout` (default: `null`)

Maximum duration for a single AI evaluation. Exceeded evaluations are cancelled and the `AiFailureStrategy` is applied.

### `AiFailureStrategy` (default: `ReturnFalse`)

Fallback value when AI evaluation fails. `ReturnFalse` is the safe default — a failing AI never triggers a rule match. `ReturnTrue` is appropriate when missing AI judgment should not block execution.

### `EnableAiCaching` (default: `false`)

Per-rule evaluation cache keyed on `prompt + serialized input`. Prevents duplicate AI calls for identical inputs within one rule evaluation.

### `AiLogger` (default: `null`)

Optional `IAiExecutionLogger` for audit logging. Callbacks: `OnEvaluating`, `OnEvaluated`, `OnFailure`. Logger exceptions are suppressed.

```csharp
var options = new RuleExecutionOptions<Order>
{
    EnableAiConditions = true,
    AiTimeout = TimeSpan.FromSeconds(5),
    AiFailureStrategy = AiFailureStrategy.ReturnFalse,
    EnableAiCaching = true,
    AiLogger = new MyAuditLogger(),
    EnableObservability = true,  // Populates result.Metrics.AiEvaluations etc.
};
```
