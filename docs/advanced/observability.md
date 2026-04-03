# Observability

RuleFlow provides a lightweight, optional observability layer that gives you runtime insights into rule execution without impacting performance or adding complexity.

## Overview

Observability in RuleFlow enables you to:
- 📊 **Track execution metrics** — Rules evaluated, matched, actions executed, groups traversed
- 🔍 **Monitor rule lifecycle** — Observe when rules are evaluated, match, and execute
- ⏱️ **Measure performance** — Capture timing data for execution analysis
- 🔌 **Extend behavior** — Implement custom observers for logging, monitoring, or analytics

**Important:** Observability is completely optional and has **zero overhead when disabled**. No observer calls, no Stopwatch allocations, no extra objects are created unless you opt-in.

## Quick Start

### Using the Built-in Observer

The simplest way to get started is to enable observability with the built-in observer:

```csharp
var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true,
    EnableDetailedTiming = true  // Optional: capture per-execution timing
};

var result = engine.Evaluate(order, rules, options);

// Metrics are now available
if (result.Metrics != null)
{
    Console.WriteLine($"Evaluated: {result.Metrics.TotalRulesEvaluated} rules");
    Console.WriteLine($"Matched: {result.Metrics.RulesMatched} rules");
    Console.WriteLine($"Executed: {result.Metrics.ActionsExecuted} actions");
    Console.WriteLine($"Traversed: {result.Metrics.GroupsTraversed} groups");
    
    if (result.Metrics.TotalElapsedMilliseconds.HasValue)
    {
        Console.WriteLine($"Duration: {result.Metrics.TotalElapsedMilliseconds}ms");
    }
}
```

### Metrics Explained

| Metric | Description |
|--------|-------------|
| `TotalRulesEvaluated` | Total number of rules evaluated (including filtered/skipped) |
| `RulesMatched` | Number of rules where the condition evaluated to true |
| `ActionsExecuted` | Total number of action steps executed across all matched rules |
| `GroupsTraversed` | Number of rule groups traversed during evaluation |
| `ExecutionStopped` | Whether execution stopped early (StopProcessing or StopOnFirstMatch) |
| `TotalElapsedMilliseconds` | Total execution time in milliseconds (only if `EnableDetailedTiming` is true) |

## Custom Observers

For more control, implement the `IRuleObserver<T>` interface to receive real-time callbacks during rule execution:

```csharp
public class LoggingObserver : IRuleObserver<Order>
{
    public void OnRuleEvaluating(RuleEvaluationContext<Order> context)
    {
        Console.WriteLine($"Evaluating rule: {context.RuleName}");
        if (context.GroupPath != null)
        {
            Console.WriteLine($"  Group: {context.GroupPath}");
        }
    }

    public void OnRuleMatched(RuleMatchContext<Order> context)
    {
        Console.WriteLine($"✓ Rule matched: {context.RuleName}");
        if (context.Reason != null)
        {
            Console.WriteLine($"  Reason: {context.Reason}");
        }
    }

    public void OnRuleExecuted(RuleExecutionContext<Order> context)
    {
        Console.WriteLine($"Executed: {context.RuleName}");
        Console.WriteLine($"  Actions executed: {context.ActionsExecutedCount}");
        
        if (context.TotalDuration.HasValue)
        {
            Console.WriteLine($"  Duration: {context.TotalDuration.Value.TotalMilliseconds}ms");
        }
    }

    public void OnExecutionCompleted(RuleExecutionSummary summary)
    {
        Console.WriteLine($"Execution complete:");
        Console.WriteLine($"  Rules matched: {summary.RulesMatched}/{summary.TotalRulesEvaluated}");
        Console.WriteLine($"  Actions executed: {summary.ActionsExecuted}");
        Console.WriteLine($"  Groups traversed: {summary.GroupsTraversed}");
    }
}

// Use your custom observer
var observer = new LoggingObserver();

var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true,
    Observer = observer
};

var result = engine.Evaluate(order, rules, options);
```

### Observer Callback Order

Observer callbacks are always invoked in a predictable sequence:

1. **OnRuleEvaluating** — Before a rule's condition is evaluated
2. **OnRuleMatched** — When a rule's condition evaluated to true
3. **OnRuleExecuted** — After a matched rule's actions have been executed (one callback per matched rule)
4. **OnExecutionCompleted** — Once at the end of the entire evaluation cycle

```
Rule 1 → Evaluating → (not matched)
Rule 2 → Evaluating → Matched → Executed
Rule 3 → Evaluating → (not matched)
...
OnExecutionCompleted (final summary)
```

### Context Models

Each observer callback receives a lightweight, immutable context:

#### RuleEvaluationContext
```csharp
public class RuleEvaluationContext<T>
{
    public string RuleName { get; init; }           // Name of the rule
    public T Input { get; init; }                   // Input object being evaluated
    public string? GroupPath { get; init; }         // e.g., "Parent/Child" or null if root
    public DateTime? StartTime { get; init; }       // Evaluation start time (if detailed timing enabled)
}
```

#### RuleMatchContext
```csharp
public class RuleMatchContext<T>
{
    public string RuleName { get; init; }           // Name of the matched rule
    public T Input { get; init; }                   // Input object
    public string? GroupPath { get; init; }         // Hierarchical group path
    public string? Reason { get; init; }            // Rule's reason (from Because())
    public TimeSpan? DurationFromEvaluation { get; init; }  // Time from evaluation start to match
}
```

#### RuleExecutionContext
```csharp
public class RuleExecutionContext<T>
{
    public string RuleName { get; init; }           // Name of the executed rule
    public T Input { get; init; }                   // Input object
    public string? GroupPath { get; init; }         // Hierarchical group path
    public bool Executed { get; init; }             // Whether actions executed successfully
    public int ActionsExecutedCount { get; init; }  // Number of action steps executed
    public TimeSpan? TotalDuration { get; init; }   // Total time for evaluation + execution
}
```

#### RuleExecutionSummary
```csharp
public class RuleExecutionSummary
{
    public int TotalRulesEvaluated { get; set; }
    public int RulesMatched { get; set; }
    public int ActionsExecuted { get; set; }
    public int GroupsTraversed { get; set; }
    public bool ExecutionStopped { get; set; }
    public TimeSpan? TotalExecutionTime { get; set; }
    public RuleExecutionMetrics Metrics { get; init; }
}
```

## Observability vs Explainability

Observability and explainability are separate concepts:

| Feature | Purpose | Overhead | When to Use |
|---------|---------|----------|------------|
| **Observability** | Real-time runtime insights, metrics, custom monitoring | Zero when disabled | Logging, monitoring, analytics, performance analysis |
| **Explainability** | Detailed hierarchical audit trail of rule execution | Always active by default | Understanding why rules matched, debugging |

You can use both together for comprehensive visibility into rule execution.

## Performance Considerations

### Zero Overhead When Disabled

When `EnableObservability` is false (the default):
- ✅ No observer callbacks are invoked
- ✅ No Stopwatch objects are created
- ✅ No context objects are allocated
- ✅ No performance impact whatsoever

### Timing Overhead

When `EnableDetailedTiming` is enabled:
- A single Stopwatch is created per evaluation
- Duration is captured only for the overall execution (not per-rule)
- Overhead is minimal (~1-2% for typical workloads)

If you need detailed per-rule timing, capture it in your custom observer's callbacks.

### Observer Exception Handling

Observer callbacks are wrapped in exception-safe guards. If an observer throws:
- ✅ The exception is suppressed
- ✅ Rule execution continues normally
- ✅ Rule outcomes are unaffected

This ensures observability failures never break rule evaluation.

## Integration with Groups and Nesting

Observability works seamlessly with nested rule groups:

```csharp
var rules = RuleSet.For<Order>("Main")
    .Add(rootRule)
    .AddGroup("Approval", g => g
        .Add(approvalRule)
        .AddGroup("Escalation", sub => sub
            .Add(escalationRule)));

var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true
};

var result = engine.Evaluate(order, rules, options);

// Metrics show both rules and groups were traversed
Console.WriteLine($"Groups traversed: {result.Metrics!.GroupsTraversed}"); // 2
```

In observer callbacks, `GroupPath` shows the full hierarchical path:
- Root rules: `GroupPath = null`
- "Approval" group rules: `GroupPath = "Approval"`
- "Escalation" group rules: `GroupPath = "Approval/Escalation"`

## Combining observability with the debug DTO

When observability is enabled, `result.Metrics` is populated and flows directly into the `DebugMetrics` property of `RuleExecutionDebugView`. This means a single call captures both runtime metrics and a structured execution snapshot:

```csharp
var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true,
    EnableDetailedTiming = true
};

var result = engine.Evaluate(order, rules, options);

// Human-readable tree (for logs / console)
Console.WriteLine(result.ToDebugString());

// Structured JSON (for dashboards / APIs / storage)
Console.WriteLine(result.ToDebugJson());
```

The JSON output includes a `"metrics"` section only when observability is enabled; it is omitted entirely (`null` → omitted via `WhenWritingNull`) otherwise.

See [Explainability — Debug DTO and JSON](../concepts/explainability#debug-dto-and-json-export) for the full DTO shape reference.

## Integration with Stop Processing

When a rule has `StopIfMatched()` or `StopOnFirstMatch` is enabled, observability tracks this:

```csharp
var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true
};

var result = engine.Evaluate(order, rules, options);

if (result.Metrics!.ExecutionStopped)
{
    Console.WriteLine("Execution stopped early due to stop-processing rule");
}
```

## Example: Monitoring Dashboard

Here's a practical example of building a monitoring dashboard with observability:

```csharp
public class MonitoringObserver : IRuleObserver<Order>
{
    private readonly List<RuleExecutionRecord> _executions = new();

    public void OnRuleEvaluating(RuleEvaluationContext<Order> context)
    {
        // Track rule evaluation start
    }

    public void OnRuleMatched(RuleMatchContext<Order> context)
    {
        // Log matched rules
        Console.WriteLine($"📌 {context.RuleName} matched");
    }

    public void OnRuleExecuted(RuleExecutionContext<Order> context)
    {
        _executions.Add(new RuleExecutionRecord
        {
            RuleName = context.RuleName,
            Executed = context.Executed,
            Duration = context.TotalDuration,
            ActionsCount = context.ActionsExecutedCount
        });
    }

    public void OnExecutionCompleted(RuleExecutionSummary summary)
    {
        // Generate report
        Console.WriteLine($"\n{'=',60}");
        Console.WriteLine($"Execution Summary");
        Console.WriteLine($"{'=',60}");
        Console.WriteLine($"Total evaluated: {summary.TotalRulesEvaluated}");
        Console.WriteLine($"Matched: {summary.RulesMatched}");
        Console.WriteLine($"Duration: {summary.TotalExecutionTime?.TotalMilliseconds}ms");
        
        foreach (var exec in _executions.Where(e => e.Executed))
        {
            Console.WriteLine($"  ✓ {exec.RuleName} ({exec.Duration?.TotalMilliseconds}ms)");
        }
    }

    private record RuleExecutionRecord(string RuleName, bool Executed, TimeSpan? Duration, int ActionsCount);
}

// Usage
var observer = new MonitoringObserver();
var options = new RuleExecutionOptions<Order>
{
    EnableObservability = true,
    EnableDetailedTiming = true,
    Observer = observer
};

engine.Evaluate(order, rules, options);
```

## Best Practices

1. **Keep observers lightweight** — Observers are called in the hot path; keep logic fast
2. **Disable in production if not needed** — Zero overhead when disabled means no reason to leave it on unnecessarily
3. **Use for monitoring, not control flow** — Observers cannot modify rule outcomes; they're audit-only
4. **Combine with explainability** — Use both for comprehensive understanding: observability for metrics, explainability for audit trail
5. **Handle observer exceptions** — The engine won't break if your observer throws, but you should still implement graceful error handling
6. **Consider async operations carefully** — Observers are synchronous; don't make blocking I/O calls inside them

## See Also

- [Explainability](./explainability.md) — Understand the audit trail of rule execution
- [Rules](../concepts/rules.md) — Learn about rule structure and configuration
- [Rule Sets](../concepts/rulesets.md) — Understand grouping and hierarchies
