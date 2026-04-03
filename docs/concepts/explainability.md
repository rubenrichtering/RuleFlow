---
title: Explainability
---

Explainability is a **first-class** feature: results include flat **rule** and **action** execution records, plus optional **tree** and **JSON** views for logs and UIs. Playground: **Explainability**, **Explainability Refactor** (`ExplainabilityScenario.cs`, `ExplainabilityRefactorScenario.cs`).

## `RuleResult`

`RuleEngine.Evaluate` / `EvaluateAsync` returns a `RuleResult` with:

- **`Executions`** — list of `RuleExecution` (flat, one entry per rule evaluation)
- **`Root`** — when explainability is enabled, a hierarchical `RuleExecutionNode` tree
- **`Explain(...)`** — formatted output via pluggable formatters

## `RuleExecution`

Each `RuleExecution` describes one rule’s participation in a run:

| Field | Meaning |
| --- | --- |
| `RuleName` | Rule display name |
| `Executed` | Condition was evaluated (vs skipped by filters) |
| `Matched` | Condition was true |
| `Skipped` | Rule skipped (e.g. metadata or group filter); `SkipReason` set |
| `Reason` | From `.Because(...)` |
| `Priority`, `Metadata`, `GroupName` | As defined on the rule |
| `StoppedProcessing` | Pipeline stopped after this rule |
| `Actions` | List of `ActionExecution` for that rule’s steps |

Typical states: executed but not matched (condition false); executed and matched; skipped with a reason such as `MetadataFilter` or `GroupFilter`.

## `ActionExecution`

When a rule matches, each `Then` / `ThenIf` step can produce an `ActionExecution`:

| Field | Meaning |
| --- | --- |
| `Description` | e.g. `Then: …` or `ThenIf: …` |
| `Executed` | Step ran |
| `Skipped` | For `ThenIf`, predicate was false |
| `SkipReason` | Why a conditional step did not run |

With explainability disabled for performance, the engine may omit per-step `ActionExecution` allocations while still running actions (see [Execution options](../advanced/execution-options)).

## Output examples

**Default text** (`result.Explain()`):

Uses the default formatter (see **Basic Rules** scenario).

**Tree** (`TextTreeFormatter`):

```csharp
Console.WriteLine(result.Explain(new TextTreeFormatter()));
```

**JSON** (`JsonRuleResultFormatter`):

```csharp
var json = new JsonRuleResultFormatter().Format(result);
```

## Debug output for quick inspection

For development and debugging, use `ToDebugString()` for instant human-readable output:

```csharp
Console.WriteLine(result.ToDebugString());
```

Produces a **structured tree view** showing:

- **Hierarchical layout** — groups and rules with indentation
- **Status markers** — ✅ matched, ❌ not matched, 🛑 stopped processing
- **Rule details** — reason (from `.Because()`), action execution, skip reasons
- **Condition trees** — when available (dynamic/persisted rules with structured conditions)
- **Execution summary** — when observability is enabled (rules evaluated, matched, elapsed time)

**Example output:**

```
RuleSet: OrderEngine

  📁 Validation
    ✅ Amount validation
       Reason: Order amount is positive
       → Action executed
    ✅ Country validation
    
  📁 Approval
    ✅ High amount approval [STOPPED]
       Reason: Amount exceeds threshold
    ❌ Standard approval [NOT EXECUTED]

─────────────────────────────
Execution Summary:
  Rules evaluated: 4
  Rules matched: 3
  Actions executed: 5
  Groups traversed: 2
  Elapsed: 12ms
```

**When to use:**

- **During development** — quickly verify rule execution flows in `Main()` or tests
- **Debugging** — understand why a rule did/didn't match before digging into code
- **Console demos** — show rule evaluation to stakeholders without complex tooling

For more advanced integration (logs, dashboards, persistence), see [Observability](../advanced/observability.md).

See **Debug Formatting** scenario in the playground for working examples.

## Debug DTO and JSON export

For **structured, UI-friendly output** — dashboards, APIs, tooling — use the Debug DTO pipeline:

```csharp
// Structured DTO
var view = result.ToDebugView();

// Indented JSON string
var json = result.ToDebugJson();
Console.WriteLine(json);
```

### `RuleExecutionDebugView`

`ToDebugView()` returns a `RuleExecutionDebugView` with:

| Property | Type | Description |
| --- | --- | --- |
| `RuleSetName` | `string` | Name of the top-level rule set |
| `Groups` | `List<DebugGroup>` | Top-level nested groups |
| `Rules` | `List<DebugRule>` | Top-level rules (not inside any group) |
| `Metrics` | `DebugMetrics?` | Aggregated metrics; `null` when observability is disabled |

### `DebugGroup`

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Group name |
| `FullPath` | `string` | Full path from the root, e.g. `"OrderApproval/Validation/HighValue"` |
| `Groups` | `List<DebugGroup>` | Nested child groups |
| `Rules` | `List<DebugRule>` | Rules directly in this group |

`FullPath` is always unique, even when different branches share the same group name.

### `DebugRule`

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Rule name |
| `Matched` | `bool` | Condition evaluated to true |
| `Executed` | `bool` | Condition was evaluated (not skipped by filters) |
| `Skipped` | `bool` | Skipped due to metadata/group filter |
| `SkipReason` | `string?` | Why it was skipped |
| `Reason` | `string?` | From `.Because()` |
| `StoppedProcessing` | `bool` | Pipeline was stopped after this rule |
| `ActionsExecuted` | `int` | Count of actions that ran |
| `Actions` | `List<DebugAction>` | Per-action execution details |
| `DurationMs` | `double?` | Execution duration (requires detailed timing) |
| `Condition` | `DebugConditionNode?` | Condition tree snapshot (when available) |

### Condition tree

`DebugConditionNode` is a polymorphic type serialized with a `"kind"` discriminator:

```json
{
  "kind": "group",
  "operator": "AND",
  "result": true,
  "children": [
    { "kind": "leaf", "field": "Amount", "operator": "greater_than", "expected": 1000, "result": true },
    {
      "kind": "group",
      "operator": "OR",
      "result": true,
      "children": [
        { "kind": "leaf", "field": "Customer.IsPremium", "operator": "equals", "expected": true, "result": true }
      ]
    }
  ]
}
```

### `DebugMetrics`

| Property | Type | Description |
| --- | --- | --- |
| `RulesEvaluated` | `int` | Total rules evaluated |
| `RulesMatched` | `int` | Rules whose conditions matched |
| `ActionsExecuted` | `int` | Total action steps executed |
| `TotalExecutionTimeMs` | `double` | Elapsed time (zero unless detailed timing is enabled) |

### JSON output shape

`ToDebugJson()` produces camelCase, indented JSON:

```json
{
  "ruleSetName": "OrderApproval",
  "groups": [
    {
      "name": "Validation",
      "fullPath": "OrderApproval/Validation",
      "groups": [],
      "rules": [
        {
          "name": "Amount check",
          "matched": true,
          "executed": true,
          "skipped": false,
          "stoppedProcessing": false,
          "actionsExecuted": 1,
          "actions": [
            { "description": "Then: …", "executed": true, "skipped": false }
          ]
        }
      ]
    }
  ],
  "rules": [],
  "metrics": {
    "rulesEvaluated": 3,
    "rulesMatched": 2,
    "actionsExecuted": 2,
    "totalExecutionTimeMs": 0
  }
}
```

### Null-safety guarantees

Both methods are safe to call unconditionally:

- `result.ToDebugView()` returns an empty view when `result` is `null`
- `result.ToDebugJson()` returns `"{}"` when `result` is `null` or on any unexpected error
- Both methods never throw

### When explainability is disabled

When `EnableExplainability = false`, the `Root` tree is not built. The Debug DTO pipeline automatically falls back to the flat `Executions` list grouped by `GroupName`. All properties remain populated; `FullPath` uses the group name directly.

### When observability is disabled

`Metrics` on `RuleExecutionDebugView` is `null`. The JSON output omits the `"metrics"` key (via `WhenWritingNull`).



Building the `Root` tree has a cost. Set `RuleExecutionOptions<T>.EnableExplainability = false` to skip tree construction while still recording rule-level executions — see **CASE 4** in `ExecutionOptionsScenario.cs`.

## Keeping docs honest

When explainability behavior changes, update:

- `ExplainabilityScenario.cs` / `ExplainabilityRefactorScenario.cs`
- This page
