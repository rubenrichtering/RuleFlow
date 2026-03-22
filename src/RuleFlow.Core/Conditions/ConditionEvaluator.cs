using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Evaluates <see cref="ConditionNode"/> trees using field resolution, conversion, and operators.
/// </summary>
public sealed class ConditionEvaluator<T> : IConditionEvaluator<T>
{
    private readonly IFieldResolver<T> _fieldResolver;
    private readonly IOperatorRegistry _operatorRegistry;
    private readonly IValueConverter _valueConverter;

    public ConditionEvaluator(
        IFieldResolver<T> fieldResolver,
        IOperatorRegistry operatorRegistry,
        IValueConverter valueConverter)
    {
        _fieldResolver = fieldResolver ?? throw new ArgumentNullException(nameof(fieldResolver));
        _operatorRegistry = operatorRegistry ?? throw new ArgumentNullException(nameof(operatorRegistry));
        _valueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
    }

    public bool Evaluate(T input, ConditionNode node, IRuleContext context)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        _ = context;

        return EvaluateRecursive(input, node);
    }

    private bool EvaluateRecursive(T input, ConditionNode node)
    {
        switch (node)
        {
            case ConditionLeaf leaf:
                return EvaluateLeaf(input, leaf);
            case ConditionGroup group:
                return EvaluateGroup(input, group);
            default:
                throw new InvalidOperationException($"Unknown condition node: {node.GetType().Name}");
        }
    }

    private bool EvaluateLeaf(T input, ConditionLeaf leaf)
    {
        var left = _fieldResolver.GetValue(input, leaf.Field);
        var leftType = _fieldResolver.GetFieldType(leaf.Field) ?? left?.GetType() ?? typeof(object);
        var targetType = Nullable.GetUnderlyingType(leftType) ?? leftType;

        object? right;
        if (!string.IsNullOrWhiteSpace(leaf.CompareToField))
        {
            right = _fieldResolver.GetValue(input, leaf.CompareToField);
        }
        else
        {
            var opName = leaf.Operator.Trim();
            if (opName.Equals("between", StringComparison.OrdinalIgnoreCase) ||
                opName.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                var arrayType = targetType.MakeArrayType();
                right = _valueConverter.Convert(leaf.Value, arrayType);
            }
            else
            {
                right = _valueConverter.Convert(leaf.Value, targetType);
            }
        }

        var op = _operatorRegistry.Get(leaf.Operator);
        return op.Evaluate(left, right);
    }

    private bool EvaluateGroup(T input, ConditionGroup group)
    {
        var isAnd = group.Operator.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase);

        if (isAnd)
        {
            foreach (var child in group.Conditions)
            {
                if (!EvaluateRecursive(input, child))
                    return false;
            }

            return true;
        }

        foreach (var child in group.Conditions)
        {
            if (EvaluateRecursive(input, child))
                return true;
        }

        return false;
    }
}
