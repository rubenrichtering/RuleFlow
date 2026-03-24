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

## Toggle tree building

Building the `Root` tree has a cost. Set `RuleExecutionOptions<T>.EnableExplainability = false` to skip tree construction while still recording rule-level executions — see **CASE 4** in `ExecutionOptionsScenario.cs`.

## Keeping docs honest

When explainability behavior changes, update:

- `ExplainabilityScenario.cs` / `ExplainabilityRefactorScenario.cs`
- This page
