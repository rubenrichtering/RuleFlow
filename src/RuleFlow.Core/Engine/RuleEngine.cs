using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Context;

namespace RuleFlow.Core.Engine;

public class RuleEngine : IRuleEngine
{
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));

        context ??= DefaultRuleContext.Instance;

        var result = new RuleResult();

        EvaluateRuleSet(input, ruleSet, context, result, groupName: null);

        return result;
    }

    private void EvaluateRuleSet<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext context,
        RuleResult result,
        string? groupName = null)
    {
        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Evaluate rules in current RuleSet
        foreach (var rule in orderedRules)
        {
            var matched = rule.Evaluate(input, context);

            var execution = new RuleExecution
            {
                RuleName = rule.Name,
                Matched = matched,
                Reason = rule.Reason,
                Priority = rule.Priority,
                GroupName = groupName
            };

            result.Executions.Add(execution);

            if (matched)
            {
                rule.Execute(input, context);

                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    return; // Stop entire execution (groups included)
                }
            }
        }

        // Evaluate groups in insertion order
        foreach (var group in ruleSet.Groups)
        {
            EvaluateRuleSet(input, group, context, result, groupName: group.Name);

            // Check if any execution in the result has StoppedProcessing set (from nested evaluation)
            if (result.Executions.Any(e => e.StoppedProcessing))
            {
                return; // Stop entire execution
            }
        }
    }
}