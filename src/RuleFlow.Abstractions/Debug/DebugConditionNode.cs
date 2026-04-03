using System.Text.Json.Serialization;

namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Abstract base for a node in the debug condition tree.
/// Serialized as a discriminated union via the <c>"kind"</c> property.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DebugConditionLeaf), "leaf")]
[JsonDerivedType(typeof(DebugConditionGroup), "group")]
[JsonDerivedType(typeof(DebugAiConditionLeaf), "ai")]
[JsonDerivedType(typeof(DebugLambdaConditionLeaf), "lambda")]
public abstract class DebugConditionNode
{
    /// <summary>Whether this condition node evaluated to true.</summary>
    public bool Result { get; set; }
}

/// <summary>
/// A single field comparison: field operator expected-value.
/// </summary>
public class DebugConditionLeaf : DebugConditionNode
{
    /// <summary>Name of the field being tested.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Operator token, e.g. <c>"greater_than"</c>, <c>"equals"</c>.</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Expected (configured) value.</summary>
    public object? Expected { get; set; }

    /// <summary>
    /// Actual runtime value of the field.
    /// Null when runtime condition tracking is not available.
    /// </summary>
    public object? Actual { get; set; }
}

/// <summary>
/// A logical grouping of child condition nodes (AND / OR).
/// </summary>
public class DebugConditionGroup : DebugConditionNode
{
    /// <summary>Logical operator: <c>"AND"</c> or <c>"OR"</c>.</summary>
    public string Operator { get; set; } = "AND";

    /// <summary>Child condition nodes.</summary>
    public List<DebugConditionNode> Children { get; set; } = [];
}

/// <summary>
/// An AI-evaluated condition leaf. Carries the full AI decision audit trail:
/// prompt, result, reason, confidence, and evaluation state.
/// </summary>
public sealed class DebugAiConditionLeaf : DebugConditionNode
{
    /// <summary>Always <see langword="true"/> — distinguishes AI nodes from deterministic nodes.</summary>
    public bool IsAi => true;

    /// <summary>The prompt submitted to the AI evaluator.</summary>
    public string? AiPrompt { get; set; }

    /// <summary>Human-readable reasoning returned by the AI, if any.</summary>
    public string? AiReason { get; set; }

    /// <summary>Confidence score [0.0, 1.0] reported by the AI, if available.</summary>
    public double? AiConfidence { get; set; }

    /// <summary>
    /// <see langword="true"/> when the AI evaluator was actually called and returned a result.
    /// <see langword="false"/> when AI evaluation was disabled or skipped.
    /// </summary>
    public bool AiEvaluated { get; set; }

    /// <summary><see langword="true"/> when the AI evaluator threw an exception during evaluation.</summary>
    public bool AiFailed { get; set; }
}

/// <summary>
/// A lambda/delegate condition. Captures only the result — lambda body is opaque at runtime.
/// </summary>
public sealed class DebugLambdaConditionLeaf : DebugConditionNode
{
    /// <summary>Display label for the lambda condition.</summary>
    public string Label { get; set; } = "λ";
}
