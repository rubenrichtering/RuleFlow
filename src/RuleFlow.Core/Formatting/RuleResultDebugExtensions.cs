using System.Text.Json;
using System.Text.Json.Serialization;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Extension methods providing debug-friendly output for <see cref="RuleResult"/>.
/// All methods are null-safe and never throw exceptions.
/// </summary>
public static class RuleResultDebugExtensions
{
    private static readonly JsonSerializerOptions DebugJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Converts a <see cref="RuleResult"/> to a human-readable debug string.
    /// Renders execution trees, condition details, and metrics.
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

    /// <summary>
    /// Converts a <see cref="RuleExecutionDebugView"/> to a human-readable debug string.
    /// </summary>
    /// <param name="view">The debug view to format. Can be null.</param>
    /// <returns>A formatted debug string, or empty string if view is null.</returns>
    public static string ToDebugString(this RuleExecutionDebugView? view)
    {
        if (view is null) return string.Empty;
        try
        {
            var formatter = new RuleExecutionDebugFormatter();
            return formatter.Format(view);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Converts a <see cref="RuleResult"/> to a structured <see cref="RuleExecutionDebugView"/> DTO.
    /// </summary>
    /// <param name="result">The rule execution result to map. Can be null.</param>
    /// <returns>
    /// A populated debug view, or an empty <see cref="RuleExecutionDebugView"/> if result is null.
    /// </returns>
    public static RuleExecutionDebugView ToDebugView(this RuleResult? result)
    {
        if (result is null) return new RuleExecutionDebugView();
        try
        {
            return RuleExecutionDebugMapper.Map(result);
        }
        catch
        {
            return new RuleExecutionDebugView();
        }
    }

    /// <summary>
    /// Serializes a <see cref="RuleResult"/> to a structured, indented JSON string.
    /// Returns <c>"{}"</c> for null input or on any unexpected error.
    /// </summary>
    /// <param name="result">The rule execution result to serialize. Can be null.</param>
    /// <returns>An indented JSON representation of the execution debug view.</returns>
    public static string ToDebugJson(this RuleResult? result)
    {
        if (result is null) return "{}";
        try
        {
            return JsonSerializer.Serialize(result.ToDebugView(), DebugJsonOptions);
        }
        catch
        {
            return "{}";
        }
    }

    /// <summary>
    /// Serializes a <see cref="RuleExecutionDebugView"/> to an indented JSON string.
    /// Returns <c>"{}"</c> for null input or on any unexpected error.
    /// </summary>
    /// <param name="view">The debug view to serialize. Can be null.</param>
    /// <returns>An indented JSON representation of the debug view.</returns>
    public static string ToDebugJson(this RuleExecutionDebugView? view)
    {
        if (view is null) return "{}";
        try
        {
            return JsonSerializer.Serialize(view, DebugJsonOptions);
        }
        catch
        {
            return "{}";
        }
    }
}
