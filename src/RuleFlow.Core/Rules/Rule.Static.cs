namespace RuleFlow.Core.Rules;

public static class Rule
{
    public static Rule<T> For<T>(string name)
    {
        return Rule<T>.For(name);
    }
}