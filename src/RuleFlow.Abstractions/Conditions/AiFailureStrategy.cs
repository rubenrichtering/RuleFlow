namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Defines the fallback strategy when an AI condition evaluation fails due to
/// an exception, timeout, or cancellation.
/// </summary>
/// <remarks>
/// AI failures must never propagate exceptions. This strategy ensures deterministic
/// fallback behavior regardless of the failure cause.
/// </remarks>
public enum AiFailureStrategy
{
    /// <summary>
    /// The AI condition resolves to <see langword="false"/> on failure.
    /// This is the safe default — a failed AI condition does not trigger the rule.
    /// </summary>
    ReturnFalse = 0,

    /// <summary>
    /// The AI condition resolves to <see langword="true"/> on failure.
    /// Use this when missing AI judgment should not block rule execution.
    /// </summary>
    ReturnTrue = 1,
}
