namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Debug representation of a single action execution within a rule.
/// </summary>
public class DebugAction
{
    /// <summary>Human-readable description of the action step.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether the action was executed.</summary>
    public bool Executed { get; set; }

    /// <summary>Whether the action was skipped (ThenIf predicate returned false).</summary>
    public bool Skipped { get; set; }

    /// <summary>Reason the action was skipped, if applicable.</summary>
    public string? SkipReason { get; set; }
}
