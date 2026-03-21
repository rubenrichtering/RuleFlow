namespace RuleFlow.Core.Rules;

public static class RuleSet
{
    public static RuleSet<T> For<T>(string name)
    {
        return RuleSet<T>.For(name);
    }
}