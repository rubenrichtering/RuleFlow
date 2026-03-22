using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions.Operators;

internal sealed class EqualsOperator : IOperator
{
    public string Name => "equals";

    public bool Evaluate(object? left, object? right) => OperandComparison.AreEqual(left, right);
}

internal sealed class GreaterThanOperator : IOperator
{
    public string Name => "greater_than";

    public bool Evaluate(object? left, object? right) => OperandComparison.Compare(left, right) > 0;
}

internal sealed class LessThanOperator : IOperator
{
    public string Name => "less_than";

    public bool Evaluate(object? left, object? right) => OperandComparison.Compare(left, right) < 0;
}

internal sealed class BetweenOperator : IOperator
{
    public string Name => "between";

    public bool Evaluate(object? left, object? right)
    {
        var (min, max) = OperandComparison.UnpackRange(right);
        var c1 = OperandComparison.Compare(left, min);
        var c2 = OperandComparison.Compare(left, max);
        return c1 >= 0 && c2 <= 0;
    }
}

internal sealed class InOperator : IOperator
{
    public string Name => "in";

    public bool Evaluate(object? left, object? right)
    {
        foreach (var item in OperandComparison.EnumerateCollection(right))
        {
            if (OperandComparison.AreEqual(left, item))
                return true;
        }

        return false;
    }
}
