namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Optional hook for logging AI condition evaluations.
/// Intended for audit logging, compliance tracking, and debugging.
/// </summary>
/// <remarks>
/// Implementation is fully optional. When not registered (null), all callbacks
/// are bypassed with zero overhead.
/// Implementations must not throw — any exceptions are silently suppressed.
/// </remarks>
public interface IAiExecutionLogger
{
    /// <summary>
    /// Called immediately before the AI evaluator is invoked.
    /// </summary>
    /// <param name="prompt">The prompt sent to the AI evaluator.</param>
    /// <param name="input">The input object (or projected sub-object) passed to the AI evaluator.</param>
    void OnEvaluating(string prompt, object input);

    /// <summary>
    /// Called after a successful AI evaluation.
    /// </summary>
    /// <param name="prompt">The prompt sent to the AI evaluator.</param>
    /// <param name="result">The result returned by the AI evaluator.</param>
    /// <param name="duration">Time elapsed during evaluation.</param>
    void OnEvaluated(string prompt, AiConditionResult result, TimeSpan duration);

    /// <summary>
    /// Called when an AI evaluation fails due to an exception, timeout, or cancellation.
    /// </summary>
    /// <param name="prompt">The prompt that was being evaluated.</param>
    /// <param name="ex">The exception that caused the failure, or null for timeout/cancellation.</param>
    void OnFailure(string prompt, Exception? ex);
}
