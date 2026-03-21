using RuleFlow.Abstractions;

namespace RuleFlow.Core.Rules;

public class Rule<T> : IRule<T>
{
    public string Name { get; }
    public string? Reason { get; private set; }
    
    private int _priority = 0;
    public int Priority => _priority;
    
    private bool _stopProcessing = false;
    public bool StopProcessing => _stopProcessing;

    private Func<T, bool> _condition = _ => true;
    private Action<T>? _action;
    
    private Func<T, Task<bool>>? _asyncCondition;
    private Func<T, Task>? _asyncAction;

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

    public Rule<T> WhenAsync(Func<T, Task<bool>> condition)
    {
        _asyncCondition = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    public Rule<T> Then(Action<T> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        return this;
    }

    public Rule<T> ThenAsync(Func<T, Task> action)
    {
        _asyncAction = action ?? throw new ArgumentNullException(nameof(action));
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

    public Rule<T> StopIfMatched()
    {
        _stopProcessing = true;
        return this;
    }


    // Sync interface implementation

    public bool Evaluate(T input, IRuleContext context)
    {
        return _condition(input);
    }

    public void Execute(T input, IRuleContext context)
    {
        _action?.Invoke(input);
    }

    // Async interface implementation

    public async Task<bool> EvaluateAsync(T input, IRuleContext context)
    {
        if (_asyncCondition != null)
        {
            return await _asyncCondition(input);
        }
        return _condition(input);
    }

    public async Task ExecuteAsync(T input, IRuleContext context)
    {
        if (_asyncAction != null)
        {
            await _asyncAction(input);
        }
        else
        {
            _action?.Invoke(input);
        }
    }
}