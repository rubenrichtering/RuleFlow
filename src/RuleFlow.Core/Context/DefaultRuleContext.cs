using RuleFlow.Abstractions;

namespace RuleFlow.Core.Context;

public class DefaultRuleContext : IRuleContext
{
    public static DefaultRuleContext Instance { get; } = new();
}