namespace RuleFlow.Abstractions.Persistence;

/// <summary>
/// Represents a persisted rule set definition (data only, no execution logic).
/// Can be nested with groups.
/// </summary>
public class RuleSetDefinition
{
    /// <summary>
    /// Unique identifier for this rule set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rules in this rule set.
    /// </summary>
    public List<RuleDefinition> Rules { get; set; } = new();

    /// <summary>
    /// Nested rule sets (groups).
    /// </summary>
    public List<RuleSetDefinition> Groups { get; set; } = new();
}
