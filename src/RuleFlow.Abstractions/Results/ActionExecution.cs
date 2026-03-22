namespace RuleFlow.Abstractions.Results;

/// <summary>
/// Represents the execution of a single action within a rule.
/// Actions can be: Then steps or ThenIf conditional steps.
/// </summary>
public class ActionExecution
{
    /// <summary>
    /// Human-readable description of the action (e.g., "Then: SetFlag", "ThenIf: SendNotification").
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// Whether this action was executed.
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// Whether this action was skipped.
    /// For Then steps: false (always executed if rule matches)
    /// For ThenIf steps: true if the predicate returned false
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Optional reason why the action was skipped (e.g., "Condition not met").
    /// </summary>
    public string? SkipReason { get; set; }
}
