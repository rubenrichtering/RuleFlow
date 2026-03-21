using RuleFlow.Abstractions.Results;

namespace RuleFlow.Abstractions;

public interface IRuleEngine
{
    RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context = null);
    
    Task<RuleResult> EvaluateAsync<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context = null);
}