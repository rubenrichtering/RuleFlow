namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Thrown when a field path cannot be resolved (unknown segment on a type).
/// </summary>
public sealed class FieldResolutionException : Exception
{
    public FieldResolutionException(string fieldPath, string message)
        : base(message)
    {
        FieldPath = fieldPath;
    }

    /// <summary>
    /// The path that was requested (e.g. <c>Customer.Name</c>).
    /// </summary>
    public string FieldPath { get; }
}
