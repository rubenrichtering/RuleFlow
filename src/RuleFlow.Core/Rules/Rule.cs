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
    
    // Action chain: supports multiple Then/ThenIf steps
    private List<ActionStep<T>> _actionSteps = new();
    
    // Async variants
    private Func<T, Task<bool>>? _asyncCondition;
    private Func<T, IRuleContext, Task<bool>>? _asyncConditionWithContext;

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

    /// <summary>
    /// Adds an unconditional action step to the rule.
    /// The action is always executed if the rule matches.
    /// </summary>
    public Rule<T> Then(Action<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) =>
            {
                action(input);
                return Task.CompletedTask;
            },
            PredicateAsync = null,
            IsAsync = false,
            Label = "Then"
        });

        return this;
    }

    /// <summary>
    /// Adds an unconditional action step to the rule with context.
    /// The action is always executed if the rule matches.
    /// </summary>
    public Rule<T> Then(Action<T, IRuleContext> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) =>
            {
                action(input, context);
                return Task.CompletedTask;
            },
            PredicateAsync = null,
            IsAsync = false,
            Label = "Then"
        });

        return this;
    }

    /// <summary>
    /// Adds a conditional action step to the rule.
    /// The action is only executed if both the rule matches AND the predicate returns true.
    /// </summary>
    public Rule<T> ThenIf(Func<T, bool> predicate, Action<T> action)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) =>
            {
                action(input);
                return Task.CompletedTask;
            },
            PredicateAsync = (input, context) => Task.FromResult(predicate(input)),
            IsAsync = false,
            Label = "ThenIf"
        });

        return this;
    }

    /// <summary>
    /// Adds a conditional action step to the rule with context.
    /// The action is only executed if both the rule matches AND the predicate returns true.
    /// </summary>
    public Rule<T> ThenIf(Func<T, IRuleContext, bool> predicate, Action<T, IRuleContext> action)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) =>
            {
                action(input, context);
                return Task.CompletedTask;
            },
            PredicateAsync = (input, context) => Task.FromResult(predicate(input, context)),
            IsAsync = false,
            Label = "ThenIf"
        });

        return this;
    }

    // ============ Async Action Overloads ============

    /// <summary>
    /// Adds an async unconditional action step to the rule.
    /// The action is always executed if the rule matches.
    /// </summary>
    public Rule<T> ThenAsync(Func<T, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) => action(input),
            PredicateAsync = null,
            IsAsync = true,
            Label = "ThenAsync"
        });

        return this;
    }

    /// <summary>
    /// Adds an async unconditional action step to the rule with context.
    /// The action is always executed if the rule matches.
    /// </summary>
    public Rule<T> ThenAsync(Func<T, IRuleContext, Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = action,
            PredicateAsync = null,
            IsAsync = true,
            Label = "ThenAsync"
        });

        return this;
    }

    /// <summary>
    /// Adds an async conditional action step to the rule.
    /// The action is only executed if both the rule matches AND the predicate returns true (async evaluation).
    /// </summary>
    public Rule<T> ThenIfAsync(Func<T, Task<bool>> predicate, Func<T, Task> action)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = (input, context) => action(input),
            PredicateAsync = (input, context) => predicate(input),
            IsAsync = true,
            Label = "ThenIfAsync"
        });

        return this;
    }

    /// <summary>
    /// Adds an async conditional action step to the rule with context.
    /// The action is only executed if both the rule matches AND the predicate returns true (async evaluation).
    /// </summary>
    public Rule<T> ThenIfAsync(Func<T, IRuleContext, Task<bool>> predicate, Func<T, IRuleContext, Task> action)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (action == null) throw new ArgumentNullException(nameof(action));
        
        // Add to action chain
        _actionSteps.Add(new ActionStep<T>
        {
            ExecuteAsync = action,
            PredicateAsync = predicate,
            IsAsync = true,
            Label = "ThenIfAsync"
        });

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
    /// Executes the rule action(s) against the input and context.
    /// Executes all action steps in order, evaluating conditional predicates as needed.
    /// </summary>
    public void Execute(T input, IRuleContext context)
    {
        ExecuteAsync(input, context).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the rule action(s) against the input and context.
    /// Executes all action steps in order, awaiting each and evaluating conditional predicates.
    /// </summary>
    public async Task ExecuteAsync(T input, IRuleContext context)
    {
        foreach (var step in _actionSteps)
        {
            // Execute conditionally if predicate is defined
            if (step.PredicateAsync == null)
            {
                // Unconditional step - always execute
                await step.ExecuteAsync(input, context);
            }
            else
            {
                // Conditional step - check predicate first
                bool predicatePassed = await step.PredicateAsync(input, context);
                if (predicatePassed)
                {
                    await step.ExecuteAsync(input, context);
                }
            }
        }
    }

    /// <summary>
    /// Returns the list of action steps for debugging/introspection.
    /// </summary>
    internal IReadOnlyList<ActionStep<T>> GetActionSteps() => _actionSteps;
}