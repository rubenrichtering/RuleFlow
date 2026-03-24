namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Lightweight context passed to observer when a rule condition matched.
/// </summary>
/// <typeparam name="T">The type of object being evaluated.</typeparam>
public class RuleMatchContext<T>
{
    /// <summary>
    /// The name of the rule that matched.
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
    /// Optional reason or description from the rule.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Optional elapsed time from evaluation start to match (only if detailed timing enabled).
    /// </summary>
    public TimeSpan? DurationFromEvaluation { get; init; }
}
