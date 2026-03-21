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

        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        foreach (var rule in orderedRules)
        {
            var matched = rule.Evaluate(input, context);

            result.Executions.Add(new RuleExecution
            {
                RuleName = rule.Name,
                Matched = matched,
                Reason = rule.Reason,
                Priority = rule.Priority
            });

            if (matched)
            {
                rule.Execute(input, context);
            }
        }

        return result;
    }
}