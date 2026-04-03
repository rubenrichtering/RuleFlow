namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Aggregated execution metrics for a debug view.
/// Only populated when observability is enabled.
/// </summary>
public class DebugMetrics
{
    /// <summary>Total number of rules evaluated during execution.</summary>
    public int RulesEvaluated { get; set; }

    /// <summary>Number of rules whose conditions matched.</summary>
    public int RulesMatched { get; set; }

    /// <summary>Total number of action steps executed across all matched rules.</summary>
    public int ActionsExecuted { get; set; }

    /// <summary>
    /// Total elapsed execution time in milliseconds.
    /// Zero when detailed timing is not enabled.
    /// </summary>
    public double TotalExecutionTimeMs { get; set; }
}
