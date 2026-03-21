using RuleFlow.Abstractions;

namespace RuleFlow.Core.Rules;

public class Rule<T> : IRule<T>
{
    public string Name { get; }
    public string? Reason { get; private set; }
    
    private int _priority = 0;
    public int Priority => _priority;

    private Func<T, bool> _condition = _ => true;
    private Action<T>? _action;

    private Rule(string name)
    {
        Name = name;
    }

    public static Rule<T> For(string name)
    {
        return new Rule<T>(name);
    }

    public Rule<T> When(Func<T, bool> condition)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    public Rule<T> Then(Action<T> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        return this;
    }

    public Rule<T> Because(string reason)
    {
        Reason = reason;
        return this;
    }

    public Rule<T> WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }


    // Interface implementatie

    public bool Evaluate(T input, IRuleContext context)
    {
        return _condition(input);
    }

    public void Execute(T input, IRuleContext context)
    {
        _action?.Invoke(input);
    }
}