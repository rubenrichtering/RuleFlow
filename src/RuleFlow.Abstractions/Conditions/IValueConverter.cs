namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Converts literal values to a target type (e.g. JSON string to int).
/// </summary>
public interface IValueConverter
{
    object? Convert(object? input, Type targetType);
}
