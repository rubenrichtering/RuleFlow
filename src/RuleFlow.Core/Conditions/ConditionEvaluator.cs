using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Evaluates <see cref="ConditionNode"/> trees using field resolution, conversion, and operators.
/// Supports AI-backed <see cref="AiConditionNode"/> conditions when an
/// <see cref="IAiConditionEvaluator{T}"/> is provided and AI evaluation is enabled.
/// </summary>
public sealed class ConditionEvaluator<T> : IConditionEvaluator<T>
{
    private readonly IFieldResolver<T> _fieldResolver;
    private readonly IOperatorRegistry _operatorRegistry;
    private readonly IValueConverter _valueConverter;
    private readonly IAiConditionEvaluator<T>? _aiEvaluator;
    private readonly bool _enableAiConditions;

    public ConditionEvaluator(
        IFieldResolver<T> fieldResolver,
        IOperatorRegistry operatorRegistry,
        IValueConverter valueConverter,
        IAiConditionEvaluator<T>? aiEvaluator = null,
        bool enableAiConditions = false)
    {
        _fieldResolver = fieldResolver ?? throw new ArgumentNullException(nameof(fieldResolver));
        _operatorRegistry = operatorRegistry ?? throw new ArgumentNullException(nameof(operatorRegistry));
        _valueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
        _aiEvaluator = aiEvaluator;
        _enableAiConditions = enableAiConditions;
    }

    public bool Evaluate(T input, ConditionNode node, IRuleContext context)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        _ = context;

        return EvaluateRecursive(input, node);
    }

    public async Task<bool> EvaluateAsync(T input, ConditionNode node, IRuleContext context, CancellationToken ct = default)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        _ = context;

        return await EvaluateRecursiveAsync(input, node, ct);
    }

    private bool EvaluateRecursive(T input, ConditionNode node)
    {
        switch (node)
        {
            case ConditionLeaf leaf:
                return EvaluateLeaf(input, leaf);
            case ConditionGroup group:
                return EvaluateGroup(input, group);
            case AiConditionNode:
                // Sync path: AI requires async — always return safe default.
                return false;
            default:
                throw new InvalidOperationException($"Unknown condition node: {node.GetType().Name}");
        }
    }

    private async Task<bool> EvaluateRecursiveAsync(T input, ConditionNode node, CancellationToken ct)
    {
        switch (node)
        {
            case ConditionLeaf leaf:
                return EvaluateLeaf(input, leaf);
            case ConditionGroup group:
                return await EvaluateGroupAsync(input, group, ct);
            case AiConditionNode ai:
                return await EvaluateAiAsync(input, ai, ct);
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

    private async Task<bool> EvaluateGroupAsync(T input, ConditionGroup group, CancellationToken ct)
    {
        var isAnd = group.Operator.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase);

        if (isAnd)
        {
            foreach (var child in group.Conditions)
            {
                if (!await EvaluateRecursiveAsync(input, child, ct))
                    return false;
            }

            return true;
        }

        foreach (var child in group.Conditions)
        {
            if (await EvaluateRecursiveAsync(input, child, ct))
                return true;
        }

        return false;
    }

    private async Task<bool> EvaluateAiAsync(T input, AiConditionNode ai, CancellationToken ct)
    {
        if (!_enableAiConditions || _aiEvaluator == null)
            return false;

        try
        {
            var result = await _aiEvaluator.EvaluateAsync(ai.Prompt, input, ct);
            return result.Result;
        }
        catch
        {
            return false;
        }
    }

    // ── Debug evaluation: produces both a result and a full DebugConditionNode tree ──────────

    /// <summary>
    /// Evaluates a <see cref="ConditionNode"/> tree and produces a corresponding
    /// <see cref="DebugConditionNode"/> tree capturing every evaluation result,
    /// including full AI metadata for <see cref="AiConditionNode"/> leaves.
    /// Unlike <see cref="EvaluateAsync"/>, group evaluation is non-short-circuiting:
    /// all children are evaluated to ensure complete debug coverage.
    /// </summary>
    public async Task<(bool Result, DebugConditionNode Tree)> EvaluateWithDebugAsync(
        T input, ConditionNode node, IRuleContext context, CancellationToken ct = default)
    {
        _ = context;
        return await EvaluateWithDebugRecursiveAsync(input, node, ct);
    }

    private async Task<(bool Result, DebugConditionNode Tree)> EvaluateWithDebugRecursiveAsync(
        T input, ConditionNode node, CancellationToken ct)
    {
        switch (node)
        {
            case ConditionLeaf leaf:
                return EvaluateLeafWithDebug(input, leaf);

            case ConditionGroup group:
                return await EvaluateGroupWithDebugAsync(input, group, ct);

            case AiConditionNode ai:
                return await EvaluateAiWithDebugAsync(input, ai, ct);

            default:
                throw new InvalidOperationException($"Unknown condition node: {node.GetType().Name}");
        }
    }

    private (bool Result, DebugConditionNode Tree) EvaluateLeafWithDebug(T input, ConditionLeaf leaf)
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
        var result = op.Evaluate(left, right);

        return (result, new DebugConditionLeaf
        {
            Result = result,
            Field = leaf.Field,
            Operator = leaf.Operator,
            Expected = leaf.Value ?? leaf.CompareToField,
            Actual = left
        });
    }

    private async Task<(bool Result, DebugConditionNode Tree)> EvaluateGroupWithDebugAsync(
        T input, ConditionGroup group, CancellationToken ct)
    {
        var isAnd = group.Operator.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase);
        var children = new List<DebugConditionNode>();
        bool groupResult = isAnd; // AND starts true, OR starts false

        // Evaluate ALL children for full debug coverage (no short-circuit).
        foreach (var child in group.Conditions)
        {
            var (childResult, childTree) = await EvaluateWithDebugRecursiveAsync(input, child, ct);
            children.Add(childTree);
            if (isAnd)
                groupResult = groupResult && childResult;
            else
                groupResult = groupResult || childResult;
        }

        return (groupResult, new DebugConditionGroup
        {
            Result = groupResult,
            Operator = group.Operator,
            Children = children
        });
    }

    private async Task<(bool Result, DebugConditionNode Tree)> EvaluateAiWithDebugAsync(
        T input, AiConditionNode ai, CancellationToken ct)
    {
        if (!_enableAiConditions || _aiEvaluator == null)
        {
            return (false, new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = ai.Prompt,
                AiEvaluated = false
            });
        }

        try
        {
            var aiResult = await _aiEvaluator.EvaluateAsync(ai.Prompt, input, ct);
            return (aiResult.Result, new DebugAiConditionLeaf
            {
                Result = aiResult.Result,
                AiPrompt = ai.Prompt,
                AiEvaluated = true,
                AiReason = aiResult.Reason,
                AiConfidence = aiResult.Confidence
            });
        }
        catch
        {
            return (false, new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = ai.Prompt,
                AiEvaluated = true,
                AiFailed = true
            });
        }
    }
}
