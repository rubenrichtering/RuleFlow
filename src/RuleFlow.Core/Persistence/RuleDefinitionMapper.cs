using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Persistence;
using RuleFlow.Core.Rules;

namespace RuleFlow.Core.Persistence;

/// <summary>
/// Maps persisted rule definitions to executable Rule&lt;T&gt; and RuleSet&lt;T&gt; instances.
///
/// Uses <see cref="IRuleRegistry{T}"/> to resolve condition and action logic from string keys,
/// or <see cref="IConditionEvaluator{T}"/> when a persisted <see cref="RuleDefinition.Condition"/> tree is present.
/// </summary>
public class RuleDefinitionMapper<T>
{
    private readonly IRuleRegistry<T> _registry;
    private readonly IConditionEvaluator<T>? _conditionEvaluator;

    public RuleDefinitionMapper(
        IRuleRegistry<T> registry,
        IConditionEvaluator<T>? conditionEvaluator = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _conditionEvaluator = conditionEvaluator;
    }

    /// <summary>
    /// Maps a RuleDefinition to an executable Rule<T>.
    /// </summary>
    /// <param name="definition">The rule definition to map.</param>
    /// <returns>An executable rule instance.</returns>
    /// <exception cref="KeyNotFoundException">If a condition or action key is not found in the registry.</exception>
    public Rule<T> MapRule(RuleDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        // Start with the rule name
        var rule = Rule.For<T>(definition.Name);

        // Resolve and apply the condition (structured tree or registry key)
        if (definition.Condition != null)
        {
            ConditionValidator.Validate(definition.Condition);
            if (_conditionEvaluator == null)
            {
                throw new InvalidOperationException(
                    "RuleDefinition has a dynamic Condition; supply IConditionEvaluator<T> to RuleDefinitionMapper.");
            }

            var tree = definition.Condition;
            rule = rule.When((input, ctx) => _conditionEvaluator.Evaluate(input, tree, ctx));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(definition.ConditionKey))
            {
                throw new InvalidOperationException(
                    "RuleDefinition has no Condition; ConditionKey is required.");
            }

            var condition = _registry.GetCondition(definition.ConditionKey);
            rule = rule.When(condition);
        }

        // Resolve and apply all actions in sequence
        foreach (var actionKey in definition.ActionKeys)
        {
            var action = _registry.GetAction(actionKey);
            rule = rule.Then(action);
        }

        // Apply optional metadata
        if (!string.IsNullOrEmpty(definition.Reason))
        {
            rule = rule.Because(definition.Reason);
        }

        // Apply priority
        if (definition.Priority != 0)
        {
            rule = rule.WithPriority(definition.Priority);
        }

        // Apply stop processing flag
        if (definition.StopProcessing)
        {
            rule = rule.StopIfMatched();
        }

        // Apply custom metadata
        foreach (var metadata in definition.Metadata)
        {
            rule = rule.WithMetadata(metadata.Key, metadata.Value);
        }

        return rule;
    }

    /// <summary>
    /// Maps a RuleSetDefinition to an executable RuleSet<T>.
    /// </summary>
    /// <param name="definition">The rule set definition to map.</param>
    /// <returns>An executable rule set instance.</returns>
    /// <exception cref="KeyNotFoundException">If a condition or action key is not found in the registry.</exception>
    public RuleSet<T> MapRuleSet(RuleSetDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        var ruleSet = RuleSet.For<T>(definition.Name);

        // Map all rules
        foreach (var ruleDef in definition.Rules)
        {
            var rule = MapRule(ruleDef);
            ruleSet = ruleSet.Add(rule);
        }

        // Map all nested groups
        foreach (var groupDef in definition.Groups)
        {
            var mappedGroup = MapRuleSet(groupDef);
            ruleSet = ruleSet.AddGroup(groupDef.Name, rs =>
            {
                // Add all rules from the mapped group
                foreach (var rule in mappedGroup.Rules)
                {
                    rs.Add(rule);
                }
                return rs;
            });
        }

        return ruleSet;
    }
}
