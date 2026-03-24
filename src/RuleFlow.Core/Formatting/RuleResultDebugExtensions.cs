using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Extension method providing debug-friendly output for RuleResult.
/// Produces human-readable execution trees with stable, deterministic formatting.
/// </summary>
public static class RuleResultDebugExtensions
{
    /// <summary>
    /// Converts a RuleResult to a debug string representation.
    /// Renders execution trees, condition details, and metrics in a human-readable format.
    /// Handles null inputs gracefully and never throws exceptions.
    /// </summary>
    /// <param name="result">The rule execution result to format. Can be null.</param>
    /// <returns>A formatted debug string, or empty string if result is null.</returns>
    public static string ToDebugString(this RuleResult? result)
    {
        try
        {
            var formatter = new RuleExecutionDebugFormatter();
            return formatter.Format(result);
        }
        catch
        {
            // Graceful fallback: never throw from ToDebugString
            return string.Empty;
        }
    }
}
