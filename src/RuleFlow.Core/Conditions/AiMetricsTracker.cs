namespace RuleFlow.Core.Conditions;

/// <summary>
/// Accumulates AI condition metrics during a single rule evaluation.
/// Passed to <see cref="FluentConditionEvaluator{T}"/> to track AI-specific counters.
/// Only allocated when both observability and AI conditions are enabled.
/// </summary>
internal sealed class AiMetricsTracker
{
    private int _aiEvaluations;
    private int _aiFailures;
    private int _aiSkipped;
    private long _aiTotalMilliseconds;

    /// <summary>Records a successful AI evaluation with its duration.</summary>
    public void RecordEvaluation(TimeSpan duration)
    {
        _aiEvaluations++;
        _aiTotalMilliseconds += (long)duration.TotalMilliseconds;
    }

    /// <summary>Records a failed AI evaluation (exception, timeout, or cancellation) with its duration.</summary>
    public void RecordFailure(TimeSpan duration)
    {
        _aiEvaluations++;
        _aiFailures++;
        _aiTotalMilliseconds += (long)duration.TotalMilliseconds;
    }

    /// <summary>Records an AI condition that was skipped (AI disabled or no evaluator).</summary>
    public void RecordSkipped()
    {
        _aiSkipped++;
    }

    /// <summary>Merges another tracker's counters into this one.</summary>
    public void Merge(AiMetricsTracker other)
    {
        _aiEvaluations += other._aiEvaluations;
        _aiFailures += other._aiFailures;
        _aiSkipped += other._aiSkipped;
        _aiTotalMilliseconds += other._aiTotalMilliseconds;
    }

    public int AiEvaluations => _aiEvaluations;
    public int AiFailures => _aiFailures;
    public int AiSkipped => _aiSkipped;
    public TimeSpan AiTotalDuration => TimeSpan.FromMilliseconds(_aiTotalMilliseconds);
}
