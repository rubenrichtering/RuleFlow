namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Lightweight context passed to observer after a matched rule's actions have been executed.
/// </summary>
/// <typeparam name="T">The type of object being evaluated.</typeparam>
public class RuleExecutionContext<T>
{
    /// <summary>
    /// The name of the rule that was executed.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// The input object.
    /// </summary>
    public required T Input { get; init; }

    /// <summary>
    /// The hierarchical group path (e.g., "Parent/Child"), or null if rule is in root set.
    /// </summary>
    public string? GroupPath { get; init; }

    /// <summary>
    /// True if the rule's actions executed successfully; false if actions were skipped or failed.
    /// </summary>
    public bool Executed { get; init; }

    /// <summary>
    /// Number of action steps that were executed.
    /// </summary>
    public int ActionsExecutedCount { get; init; }

    /// <summary>
    /// Optional elapsed time for the entire rule execution (evaluation + action execution)
    /// only if detailed timing enabled.
    /// </summary>
    public TimeSpan? TotalDuration { get; init; }
}
