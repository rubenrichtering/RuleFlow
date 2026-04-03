namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Structured, UI-friendly representation of a complete rule execution.
/// Produced by <c>result.ToDebugView()</c>.
/// </summary>
public class RuleExecutionDebugView
{
    /// <summary>Name of the top-level rule set.</summary>
    public string RuleSetName { get; set; } = string.Empty;

    /// <summary>Top-level groups contained in the rule set.</summary>
    public List<DebugGroup> Groups { get; set; } = [];

    /// <summary>Top-level rules (not inside any group).</summary>
    public List<DebugRule> Rules { get; set; } = [];

    /// <summary>
    /// Aggregated execution metrics. Null when observability is disabled.
    /// </summary>
    public DebugMetrics? Metrics { get; set; }
}
