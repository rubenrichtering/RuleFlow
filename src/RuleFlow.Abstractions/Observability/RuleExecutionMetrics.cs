namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Metrics collected during a rule execution cycle.
/// Designed to be lightweight and easily serializable.
/// </summary>
public class RuleExecutionMetrics
{
    /// <summary>
    /// Total number of rules evaluated in this execution.
    /// </summary>
    public int TotalRulesEvaluated { get; set; }

    /// <summary>
    /// Number of rules that matched (condition evaluated to true).
    /// </summary>
    public int RulesMatched { get; set; }

    /// <summary>
    /// Total number of action steps executed across all matched rules.
    /// </summary>
    public int ActionsExecuted { get; set; }

    /// <summary>
    /// Number of groups traversed during evaluation.
    /// </summary>
    public int GroupsTraversed { get; set; }

    /// <summary>
    /// Total milliseconds for the complete evaluation and execution cycle.
    /// Only populated when EnableDetailedTiming is true.
    /// </summary>
    public long? TotalElapsedMilliseconds { get; set; }

    /// <summary>
    /// Timestamp when execution started (UTC).
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when execution completed (UTC).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // ── AI Condition Metrics ─────────────────────────────────────────────────

    /// <summary>
    /// Total number of AI condition evaluations attempted.
    /// Only populated when <c>EnableObservability</c> is true.
    /// </summary>
    public int AiEvaluations { get; set; }

    /// <summary>
    /// Number of AI condition evaluations that failed (exception, timeout, or cancellation).
    /// Only populated when <c>EnableObservability</c> is true.
    /// </summary>
    public int AiFailures { get; set; }

    /// <summary>
    /// Number of AI conditions that were skipped because AI was disabled or no evaluator
    /// was registered.
    /// Only populated when <c>EnableObservability</c> is true.
    /// </summary>
    public int AiSkipped { get; set; }

    /// <summary>
    /// Total cumulative time spent in AI evaluations.
    /// Only populated when <c>EnableObservability</c> is true.
    /// </summary>
    public TimeSpan AiTotalDuration { get; set; }
}
