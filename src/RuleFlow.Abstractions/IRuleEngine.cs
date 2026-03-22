using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Abstractions;

public interface IRuleEngine
{
    RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context = null);
    
    RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, RuleExecutionOptions<T> options);
    
    RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context, RuleExecutionOptions<T>? options);
    
    Task<RuleResult> EvaluateAsync<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context = null);
    
    Task<RuleResult> EvaluateAsync<T>(T input, IRuleSet<T> ruleSet, RuleExecutionOptions<T> options);
    
    Task<RuleResult> EvaluateAsync<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context, RuleExecutionOptions<T>? options);
}