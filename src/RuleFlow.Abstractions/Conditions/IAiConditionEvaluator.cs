namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Evaluates an <see cref="AiConditionNode"/> against an input using an AI model.
/// </summary>
/// <typeparam name="T">The rule input type.</typeparam>
public interface IAiConditionEvaluator<T>
{
    /// <summary>
    /// Evaluates the given <paramref name="prompt"/> against <paramref name="input"/>
    /// and returns an <see cref="AiConditionResult"/>.
    /// </summary>
    Task<AiConditionResult> EvaluateAsync(string prompt, T input, CancellationToken ct);
}
