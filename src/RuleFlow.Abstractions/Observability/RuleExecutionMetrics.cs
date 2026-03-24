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
}
