using RuleFlow.Abstractions.Observability;

namespace RuleFlow.Core.Observability;

/// <summary>
/// Built-in lightweight observer that collects metrics and execution summary during rule evaluation.
/// Thread-safe for single-threaded evaluation but not designed for concurrent use.
/// </summary>
/// <typeparam name="T">The type of object being evaluated.</typeparam>
public sealed class InMemoryRuleObserver<T> : IRuleObserver<T>
{
    private readonly RuleExecutionMetrics _metrics = new();
    private int _groupsTraversed;
    private int _actionsExecuted;
    private bool _executionStopped;

    public void OnRuleEvaluating(RuleEvaluationContext<T> context)
    {
        // Track group traversals - count unique groups
        if (context.GroupPath != null)
        {
            _groupsTraversed++;
        }
    }

    public void OnRuleMatched(RuleMatchContext<T> context)
    {
        _metrics.RulesMatched++;
    }

    public void OnRuleExecuted(RuleExecutionContext<T> context)
    {
        if (context.Executed)
        {
            _actionsExecuted += context.ActionsExecutedCount;
        }
    }

    public void OnExecutionCompleted(RuleExecutionSummary summary)
    {
        // Execution completed - summary is already populated by engine
        _executionStopped = summary.ExecutionStopped;
    }

    /// <summary>
    /// Gets the current metrics snapshot.
    /// </summary>
    public RuleExecutionMetrics GetMetrics()
    {
        return _metrics;
    }

    /// <summary>
    /// Gets the execution summary (for final result attachment).
    /// </summary>
    public RuleExecutionSummary GetSummary()
    {
        return new RuleExecutionSummary
        {
            TotalRulesEvaluated = _metrics.TotalRulesEvaluated,
            RulesMatched = _metrics.RulesMatched,
            ActionsExecuted = _actionsExecuted,
            GroupsTraversed = _groupsTraversed,
            ExecutionStopped = _executionStopped,
            TotalExecutionTime = _metrics.TotalElapsedMilliseconds.HasValue 
                ? TimeSpan.FromMilliseconds(_metrics.TotalElapsedMilliseconds.Value)
                : null,
            Metrics = _metrics
        };
    }

    /// <summary>
    /// Updates total rules evaluated count (called by engine).
    /// </summary>
    internal void SetTotalRulesEvaluated(int count)
    {
        _metrics.TotalRulesEvaluated = count;
    }

    /// <summary>
    /// Updates total elapsed time (called by engine).
    /// </summary>
    internal void SetTotalElapsedMilliseconds(long? ms)
    {
        _metrics.TotalElapsedMilliseconds = ms;
    }

    /// <summary>
    /// Sets timing boundaries.
    /// </summary>
    internal void SetTimeBoundaries(DateTime? startedAt, DateTime? completedAt)
    {
        _metrics.StartedAt = startedAt;
        _metrics.CompletedAt = completedAt;
    }
}
