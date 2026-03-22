namespace RuleFlow.Abstractions.Results;

/// <summary>
/// Represents the complete execution state of a single rule.
/// 
/// State combinations (invariants):
/// - Executed=true, Matched=true, Skipped=false → Rule evaluated and matched
/// - Executed=true, Matched=false, Skipped=false → Rule evaluated but didn't match
/// - Executed=false, Matched=false, Skipped=true → Rule was skipped (filter/group filter)
/// - Executed=true, Matched=true, StoppedProcessing=true → Rule matched and stopped pipeline
/// 
/// Invalid combinations:
/// - Skipped=true AND Executed=true (mutually exclusive)
/// - Matched=true AND (Executed=false OR Skipped=true) (can't match if not executed)
/// </summary>
public class RuleExecution
{
    /// <summary>
    /// The name of the rule.
    /// </summary>
    public string RuleName { get; set; } = default!;

    /// <summary>
    /// Whether the rule was evaluated (condition was checked).
    /// Opposite of Skipped: exactly one must be true.
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// Whether the rule's condition matched.
    /// Only meaningful if Executed=true and Skipped=false.
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// Whether the rule was skipped due to filters or group inclusion.
    /// If true, Executed must be false (mutually exclusive).
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Why the rule was skipped, if applicable (e.g., "MetadataFilter", "GroupFilter").
    /// Only set if Skipped=true.
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// The reason for the rule (from Because()).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// The priority of the rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this rule's execution stopped the processing pipeline.
    /// Only true if Executed=true and Matched=true and rule has StopProcessing set or option was StopOnFirstMatch.
    /// </summary>
    public bool StoppedProcessing { get; set; }

    /// <summary>
    /// The name of the group this rule belongs to, if any.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Timestamp when the rule was evaluated.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata attached to the rule.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Collection of action executions within this rule.
    /// Built when the rule matches and actions are executed.
    /// </summary>
    public List<ActionExecution> Actions { get; } = new();
}