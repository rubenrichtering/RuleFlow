namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Debug representation of a single rule's execution state.
/// </summary>
public class DebugRule
{
    /// <summary>Name of the rule.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the rule's condition evaluated to true.</summary>
    public bool Matched { get; set; }

    /// <summary>Whether the rule was evaluated (not filtered/skipped).</summary>
    public bool Executed { get; set; }

    /// <summary>Whether the rule was skipped due to a metadata or group filter.</summary>
    public bool Skipped { get; set; }

    /// <summary>Reason the rule was skipped, if applicable.</summary>
    public string? SkipReason { get; set; }

    /// <summary>Human-readable reason from the rule's <c>Because()</c> clause.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether this rule stopped further processing of the pipeline.</summary>
    public bool StoppedProcessing { get; set; }

    /// <summary>Number of actions that were executed for this rule.</summary>
    public int ActionsExecuted { get; set; }

    /// <summary>Detailed action execution list.</summary>
    public List<DebugAction> Actions { get; set; } = [];

    /// <summary>
    /// Per-rule execution duration in milliseconds.
    /// Null unless detailed timing is available.
    /// </summary>
    public double? DurationMs { get; set; }

    /// <summary>
    /// Condition tree evaluated for this rule.
    /// Null when condition data is not available in the execution result.
    /// </summary>
    public DebugConditionNode? Condition { get; set; }
}
