namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Resolves field values from a strongly-typed input (e.g. by property name).
/// </summary>
public interface IFieldResolver<T>
{
    object? GetValue(T input, string field);

    /// <summary>
    /// Returns the declared type of the field, if known (used for value conversion).
    /// </summary>
    Type? GetFieldType(string field);
}
