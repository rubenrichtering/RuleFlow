namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Lightweight summary of a complete rule evaluation and execution cycle.
/// </summary>
public class RuleExecutionSummary
{
    /// <summary>
    /// Total number of rules evaluated.
    /// </summary>
    public int TotalRulesEvaluated { get; set; }

    /// <summary>
    /// Number of rules that matched.
    /// </summary>
    public int RulesMatched { get; set; }

    /// <summary>
    /// Number of rule actions executed.
    /// </summary>
    public int ActionsExecuted { get; set; }

    /// <summary>
    /// Number of groups traversed.
    /// </summary>
    public int GroupsTraversed { get; set; }

    /// <summary>
    /// Whether execution was stopped early (by StopProcessing or StopOnFirstMatch).
    /// </summary>
    public bool ExecutionStopped { get; set; }

    /// <summary>
    /// Optional total execution time (only if detailed timing enabled).
    /// </summary>
    public TimeSpan? TotalExecutionTime { get; set; }

    /// <summary>
    /// Aggregated metrics from evaluation.
    /// </summary>
    public RuleExecutionMetrics Metrics { get; init; } = new();
}
