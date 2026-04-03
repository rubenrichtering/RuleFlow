# AI Conditions

RuleFlow supports AI-powered conditions alongside deterministic rules. AI conditions are **always advisory** — they complement deterministic logic and are never the sole decision-maker in critical paths.

## Concept

| Principle | Detail |
|---|---|
| **Advisory only** | AI augments rules; it does not replace them |
| **Always explainable** | Debug output shows prompt, reason, confidence, and whether AI was evaluated |
| **Safe by default** | AI disabled unless explicitly opted in; failures always use a fallback strategy |
| **Zero overhead** | `EnableAiConditions = false` → no allocations, no evaluator calls |

## Quick Start

```csharp
var rule = Rule.For<Order>("Fraud Check")
    .WithAiEvaluator(new MyAiEvaluator())         // Register AI evaluator
    .When(o => o.Amount > 1000)                    // Deterministic gate
    .WhenAI("Is this transaction suspicious?", o => new { o.Amount, o.Supplier, o.Country })
    .Then(o => o.Flag = true);

var options = new RuleExecutionOptions<Order>
{
    EnableAiConditions = true,
};

var result = await engine.EvaluateAsync(order, ruleSet, options);
```

## Implementing `IAiConditionEvaluator<T>`

```csharp
public class MyAiEvaluator : IAiConditionEvaluator<Order>
{
    public async Task<AiConditionResult> EvaluateAsync(
        string prompt, Order input, CancellationToken ct)
    {
        // Call your AI service here
        var response = await _aiService.AskAsync(prompt, input, ct);
        return new AiConditionResult
        {
            Result = response.IsPositive,
            Reason = response.Explanation,
            Confidence = response.Score
        };
    }
}
```

## Fluent API

| Method | Description |
|---|---|
| `.WithAiEvaluator(evaluator)` | Register the AI evaluator for this rule |
| `.WhenAI("prompt")` | Add an AI condition using full input as context |
| `.WhenAI("prompt", x => new { x.Field })` | Add AI condition with focused sub-object projection |
| `.WhenGroup(g => g.WhenAI(...))` | Compose AI conditions in AND/OR groups |

When chained with `.When()`, all conditions are combined with **AND logic**.

## Execution Options

All AI options live on `RuleExecutionOptions<T>`:

### `EnableAiConditions` (default: `false`)

Gates all AI condition evaluation. When `false`, every `WhenAI` condition resolves to `false` with **zero overhead** — no evaluator is called, no allocations occur.

```csharp
new RuleExecutionOptions<T> { EnableAiConditions = true }
```

### `AiTimeout` (default: `null`)

Maximum time allowed for a single AI condition evaluation. When exceeded:
- Evaluation is cancelled
- `AiFailureStrategy` is applied
- Pipeline continues safely — no exception thrown

```csharp
new RuleExecutionOptions<T>
{
    EnableAiConditions = true,
    AiTimeout = TimeSpan.FromSeconds(5),
}
```

### `AiFailureStrategy` (default: `ReturnFalse`)

Determines the fallback value when AI evaluation fails (exception, timeout, or cancellation).

| Value | Behavior |
|---|---|
| `ReturnFalse` | Failed AI condition = `false`. Rule will not match on AI failure. **Safe default.** |
| `ReturnTrue` | Failed AI condition = `true`. Use when absence of AI judgment should not block execution. |

```csharp
new RuleExecutionOptions<T>
{
    EnableAiConditions = true,
    AiFailureStrategy = AiFailureStrategy.ReturnFalse,
}
```

> ⚠ AI failures **never throw**. All failure paths are caught and resolved by this strategy.

### `EnableAiCaching` (default: `false`)

Enables per-evaluation caching of AI results. Cache key = `prompt + serialized input`.

When enabled, duplicate `WhenAI` calls with the same prompt and identical input within a single rule evaluation will only call the evaluator **once**.

```csharp
new RuleExecutionOptions<T>
{
    EnableAiConditions = true,
    EnableAiCaching = true,
}
```

Scope: per rule evaluation. There is no global or cross-evaluation cache.

### `AiLogger` (default: `null`)

Optional hook for audit logging, compliance, and debugging.

```csharp
public class MyAuditLogger : IAiExecutionLogger
{
    public void OnEvaluating(string prompt, object input)
        => _log.Info($"AI evaluating: {prompt}");

    public void OnEvaluated(string prompt, AiConditionResult result, TimeSpan duration)
        => _log.Info($"AI result={result.Result} confidence={result.Confidence} in {duration.TotalMs}ms");

    public void OnFailure(string prompt, Exception? ex)
        => _log.Warn($"AI failed for: {prompt} — {ex?.Message ?? "timeout"}");
}

new RuleExecutionOptions<T>
{
    EnableAiConditions = true,
    AiLogger = new MyAuditLogger(),
}
```

> Logger exceptions are silently suppressed — a failing logger never breaks rule execution.

## Observability (AI Metrics)

When `EnableObservability = true`, `RuleExecutionMetrics` includes AI-specific counters:

```csharp
var options = new RuleExecutionOptions<T>
{
    EnableAiConditions = true,
    EnableObservability = true,
};

var result = await engine.EvaluateAsync(input, ruleSet, options);

Console.WriteLine(result.Metrics.AiEvaluations);  // Total AI evaluations attempted
Console.WriteLine(result.Metrics.AiFailures);      // Failed evaluations
Console.WriteLine(result.Metrics.AiSkipped);       // Skipped (AI disabled / no evaluator)
Console.WriteLine(result.Metrics.AiTotalDuration); // Cumulative AI evaluation time
```

AI metrics are zero-overhead when `EnableObservability = false`.

## Debug Output

AI conditions appear clearly in the debug tree:

```
[AI ✅] Fraud Check
   Prompt: Is this transaction suspicious?
   Reason: High amount + unknown supplier in high-risk country
   Confidence: 87%
   ⚠ AI-generated — verify manually
```

Use `result.ToDebugString()` or `result.ToDebugJson()` to inspect AI condition results.

`DebugAiConditionLeaf` fields:
- `AiPrompt` — the prompt sent to the evaluator
- `AiEvaluated` — whether the AI was actually called (`false` when disabled)
- `AiReason` — explanation from the AI
- `AiConfidence` — confidence score (0–1)
- `AiFailed` — whether evaluation failed (exception, timeout, cancellation)

## Best Practices

1. **Always combine AI with deterministic rules** — use `.When()` as a gate before `.WhenAI()`
2. **Use focused projections** — pass only the fields the AI needs: `.WhenAI("prompt", x => new { x.Amount, x.Country })`
3. **Set a timeout** — never let AI block your pipeline indefinitely
4. **Choose `ReturnFalse`** — the safe default prevents AI failures from incorrectly triggering rules
5. **Enable logging in production** — use `IAiExecutionLogger` for audit trails and compliance
6. **Monitor with observability** — track `AiEvaluations`, `AiFailures`, and `AiTotalDuration`

## Warnings

> ⚠ **AI conditions are non-deterministic.** The same input may produce different results across calls.

> ⚠ **Requires monitoring.** AI failures are silent by default. Use `AiLogger` and observability metrics to detect degradation.

> ⚠ **Should be audited in critical systems.** Never rely solely on AI conditions for security, compliance, or financial decisions.

> ⚠ **AI is advisory, never authoritative.** Combine with deterministic conditions to ensure your rules remain predictable and auditable.
