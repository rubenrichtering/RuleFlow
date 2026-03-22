namespace RuleFlow.Extensions.DependencyInjection;

/// <summary>
/// Configuration options for RuleFlow Dependency Injection integration.
/// </summary>
public class RuleFlowOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether explainability is enabled.
    /// When enabled, rule execution results include detailed explanations.
    /// Default: true
    /// </summary>
    public bool EnableExplainability { get; set; } = true;
}
