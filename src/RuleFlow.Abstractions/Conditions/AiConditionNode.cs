using System.Text.Json.Serialization;

namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// A condition node that delegates evaluation to an AI model.
/// Identified by <c>"kind": "ai"</c> in the JSON condition tree.
/// </summary>
public sealed class AiConditionNode : ConditionNode
{
    /// <summary>
    /// The prompt describing the condition to be evaluated by the AI.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Optional selector to extract a focused sub-object from the input before passing it to the AI.
    /// Not serialized; wire up programmatically when needed.
    /// </summary>
    [JsonIgnore]
    public Func<object, object>? InputSelector { get; set; }
}
