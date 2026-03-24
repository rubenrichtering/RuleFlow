namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Lightweight context passed to observer when a rule evaluation begins.
/// </summary>
/// <typeparam name="T">The type of object being evaluated.</typeparam>
public class RuleEvaluationContext<T>
{
    /// <summary>
    /// The name of the rule being evaluated.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// The input object being evaluated.
    /// </summary>
    public required T Input { get; init; }

    /// <summary>
    /// The hierarchical group path (e.g., "Parent/Child"), or null if rule is in root set.
    /// </summary>
    public string? GroupPath { get; init; }

    /// <summary>
    /// Optional timestamp when evaluation started (only if detailed timing enabled).
    /// </summary>
    public DateTime? StartTime { get; init; }
}
