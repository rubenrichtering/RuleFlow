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

    private Dictionary<string, object?> _metadata = new();
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    // Sync variants
    private Func<T, bool> _condition = _ => true;
    private Func<T, IRuleContext, bool>? _conditionWithContext;
    private Action<T>? _action;
    private Action<T, IRuleContext>? _actionWithContext;
    
    // Async variants
    private Func<T, Task<bool>>? _asyncCondition;
    private Func<T, IRuleContext, Task<bool>>? _asyncConditionWithContext;
    private Func<T, Task>? _asyncAction;
    private Func<T, IRuleContext, Task>? _asyncActionWithContext;

    private Rule(string name)
    {
        Name = name;
    }

    public static Rule<T> For(string name)
    {
        return new Rule<T>(name);
    }

    // ============ Sync Condition Overloads ============

    public Rule<T> When(Func<T, bool> condition)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _conditionWithContext = null; // Clear context version
        return this;
    }

    public Rule<T> When(Func<T, IRuleContext, bool> condition)
    {
        _conditionWithContext = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    // ============ Async Condition Overloads ============

    public Rule<T> WhenAsync(Func<T, Task<bool>> condition)
    {
        _asyncCondition = condition ?? throw new ArgumentNullException(nameof(condition));
        _asyncConditionWithContext = null; // Clear context version
        return this;
    }

    public Rule<T> WhenAsync(Func<T, IRuleContext, Task<bool>> condition)
    {
        _asyncConditionWithContext = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    // ============ Sync Action Overloads ============

    public Rule<T> Then(Action<T> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _actionWithContext = null; // Clear context version
        return this;
    }

    public Rule<T> Then(Action<T, IRuleContext> action)
    {
        _actionWithContext = action ?? throw new ArgumentNullException(nameof(action));
        return this;
    }

    // ============ Async Action Overloads ============

    public Rule<T> ThenAsync(Func<T, Task> action)
    {
        _asyncAction = action ?? throw new ArgumentNullException(nameof(action));
        _asyncActionWithContext = null; // Clear context version
        return this;
    }

    public Rule<T> ThenAsync(Func<T, IRuleContext, Task> action)
    {
        _asyncActionWithContext = action ?? throw new ArgumentNullException(nameof(action));
        return this;
    }

    // ============ Fluent Configuration ============

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

    public Rule<T> WithMetadata(string key, object? value)
    {
        _metadata[key] = value;
        return this;
    }

    // ============ Execution Logic ============

    /// <summary>
    /// Evaluates the rule condition against the input and context.
    /// Priority order:
    /// 1. Context-based async condition
    /// 2. Async condition
    /// 3. Context-based sync condition
    /// 4. Sync condition
    /// </summary>
    public bool Evaluate(T input, IRuleContext context)
    {
        if (_conditionWithContext != null)
        {
            return _conditionWithContext(input, context);
        }
        return _condition(input);
    }

    /// <summary>
    /// Executes the rule action against the input and context.
    /// Priority order:
    /// 1. Context-based sync action
    /// 2. Sync action
    /// </summary>
    public void Execute(T input, IRuleContext context)
    {
        if (_actionWithContext != null)
        {
            _actionWithContext(input, context);
        }
        else
        {
            _action?.Invoke(input);
        }
    }

    /// <summary>
    /// Asynchronously evaluates the rule condition against the input and context.
    /// Priority order:
    /// 1. Context-based async condition
    /// 2. Async condition
    /// 3. Context-based sync condition
    /// 4. Sync condition
    /// </summary>
    public async Task<bool> EvaluateAsync(T input, IRuleContext context)
    {
        if (_asyncConditionWithContext != null)
        {
            return await _asyncConditionWithContext(input, context);
        }
        if (_asyncCondition != null)
        {
            return await _asyncCondition(input);
        }
        if (_conditionWithContext != null)
        {
            return _conditionWithContext(input, context);
        }
        return _condition(input);
    }

    /// <summary>
    /// Asynchronously executes the rule action against the input and context.
    /// Priority order:
    /// 1. Context-based async action
    /// 2. Async action
    /// 3. Context-based sync action
    /// 4. Sync action
    /// </summary>
    public async Task ExecuteAsync(T input, IRuleContext context)
    {
        if (_asyncActionWithContext != null)
        {
            await _asyncActionWithContext(input, context);
        }
        else if (_asyncAction != null)
        {
            await _asyncAction(input);
        }
        else if (_actionWithContext != null)
        {
            _actionWithContext(input, context);
        }
        else
        {
            _action?.Invoke(input);
        }
    }
}