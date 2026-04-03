namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Validates <see cref="ConditionNode"/> trees before execution.
/// </summary>
public static class ConditionValidator
{
    public static void Validate(ConditionNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        ValidateRecursive(node);
    }

    private static void ValidateRecursive(ConditionNode node)
    {
        switch (node)
        {
            case ConditionLeaf leaf:
                ValidateLeaf(leaf);
                break;
            case ConditionGroup group:
                ValidateGroup(group);
                foreach (var child in group.Conditions)
                    ValidateRecursive(child);
                break;
            case AiConditionNode ai:
                ValidateAiNode(ai);
                break;
            default:
                throw new InvalidOperationException($"Unknown condition node type: {node.GetType().Name}");
        }
    }

    private static void ValidateLeaf(ConditionLeaf leaf)
    {
        if (string.IsNullOrWhiteSpace(leaf.Field))
            throw new InvalidOperationException("Condition leaf Field must not be null or empty.");

        if (string.IsNullOrWhiteSpace(leaf.Operator))
            throw new InvalidOperationException("Condition leaf Operator must not be null or empty.");

        var hasCompareField = !string.IsNullOrWhiteSpace(leaf.CompareToField);

        // Literal path: CompareToField unset — right operand is Value (may be null).
        // Field path: CompareToField set — Value must not also carry a literal.
        if (hasCompareField && leaf.Value != null)
            throw new InvalidOperationException(
                "Condition leaf must not set both CompareToField and Value; use one comparison mode.");
    }

    private static void ValidateGroup(ConditionGroup group)
    {
        if (group.Conditions == null || group.Conditions.Count == 0)
            throw new InvalidOperationException("Condition group must contain at least one child condition.");

        var op = group.Operator.Trim();
        if (!op.Equals("AND", StringComparison.OrdinalIgnoreCase) &&
            !op.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Condition group Operator must be AND or OR (got '{group.Operator}').");
        }
    }

    private static void ValidateAiNode(AiConditionNode ai)
    {
        if (string.IsNullOrWhiteSpace(ai.Prompt))
            throw new InvalidOperationException("AI condition node Prompt must not be null or empty.");
    }
}
