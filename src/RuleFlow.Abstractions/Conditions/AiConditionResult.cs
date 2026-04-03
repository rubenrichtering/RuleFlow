namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Represents the outcome of an AI-based condition evaluation.
/// </summary>
public sealed class AiConditionResult
{
    /// <summary>The boolean conclusion returned by the AI.</summary>
    public bool Result { get; set; }

    /// <summary>Optional human-readable explanation provided by the AI.</summary>
    public string? Reason { get; set; }

    /// <summary>Optional confidence score [0.0, 1.0] reported by the AI.</summary>
    public double? Confidence { get; set; }

    /// <summary>Always <see langword="false"/>: AI results are never deterministic.</summary>
    public bool IsDeterministic => false;
}
