namespace RuleFlow.Abstractions.Persistence;

/// <summary>
/// Represents a persisted rule definition (data only, no execution logic).
/// </summary>
public class RuleDefinition
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Business reason or explanation for this rule.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Execution priority (higher = earlier execution).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Stop processing remaining rules if this rule matches (short-circuit).
    /// </summary>
    public bool StopProcessing { get; set; }

    /// <summary>
    /// Key to resolve the condition logic from the registry.
    /// </summary>
    public string ConditionKey { get; set; } = string.Empty;

    /// <summary>
    /// Keys to resolve the action logic from the registry.
    /// </summary>
    public List<string> ActionKeys { get; set; } = new();

    /// <summary>
    /// Custom metadata for the rule (extensibility).
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}
