using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Builders;
using RuleFlow.Core.Conditions;

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

    // Structured condition (from persistence/dynamic rules) — enables debug tree capture
    private ConditionNode? _structuredConditionNode;
    private ConditionEvaluator<T>? _structuredConditionEvaluator;

    // Fluent-built condition tree (.WhenAI / .WhenGroup)
    private ConditionNode? _fluentConditionNode;
    private IAiConditionEvaluator<T>? _fluentAiEvaluator;
    private bool _enableFluentAi;
    private bool _hasExplicitLambdaCondition;

    /// <summary>True when this rule was built with a structured condition node (e.g. from persistence).</summary>
    internal bool HasStructuredCondition => _structuredConditionNode != null && _structuredConditionEvaluator != null;

    /// <summary>True when this rule has a fluent-built condition tree (from .WhenAI or .WhenGroup).</summary>
    internal bool HasFluentCondition => _fluentConditionNode != null;

    /// <summary>The fluent condition tree built by .WhenAI / .WhenGroup (for inspection/testing).</summary>
    internal ConditionNode? FluentConditionNode => _fluentConditionNode;

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
        _hasExplicitLambdaCondition = true;
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

    // ============ AI Condition Overloads ============

    /// <summary>
    /// Registers the AI evaluator used for <c>.WhenAI()</c> conditions on this rule.
    /// </summary>
    /// <param name="aiEvaluator">The AI evaluator implementation.</param>
    /// <param name="enabled">
    /// When <see langword="false"/>, AI conditions are skipped and evaluate to
    /// <see langword="false"/>. Defaults to <see langword="true"/>.
    /// </param>
    public Rule<T> WithAiEvaluator(IAiConditionEvaluator<T> aiEvaluator, bool enabled = true)
    {
        _fluentAiEvaluator = aiEvaluator ?? throw new ArgumentNullException(nameof(aiEvaluator));
        _enableFluentAi = enabled;
        return this;
    }

    /// <summary>
    /// Adds an AI condition with the given prompt.
    /// The full input is passed to the AI evaluator as context.
    /// When chained after <c>.When()</c>, both conditions must be satisfied (AND logic).
    /// </summary>
    public Rule<T> WhenAI(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));

        AddToFluentTree(new AiConditionNode { Prompt = prompt });
        return this;
    }

    /// <summary>
    /// Adds an AI condition with the given prompt and a focused sub-object projection
    /// passed to the AI evaluator as context.
    /// When chained after <c>.When()</c>, both conditions must be satisfied (AND logic).
    /// </summary>
    public Rule<T> WhenAI(string prompt, Func<T, object> inputSelector)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        ArgumentNullException.ThrowIfNull(inputSelector);

        AddToFluentTree(new AiConditionNode
        {
            Prompt = prompt,
            InputSelector = input => inputSelector((T)input!)
        });
        return this;
    }

    /// <summary>
    /// Adds a logical condition group (AND/OR) that can mix lambda and AI conditions.
    /// </summary>
    public Rule<T> WhenGroup(Action<ConditionGroupBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ConditionGroupBuilder<T>();
        configure(builder);
        AddToFluentTree(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds the given node to the fluent condition tree, AND-ing with existing nodes.
    /// When a lambda condition from <c>.When()</c> was set, it is incorporated as the
    /// first child of the AND group.
    /// </summary>
    private void AddToFluentTree(ConditionNode node)
    {
        if (_fluentConditionNode == null)
        {
            if (_hasExplicitLambdaCondition)
            {
                // Incorporate the existing lambda as first child
                _fluentConditionNode = new ConditionGroup
                {
                    Operator = "AND",
                    Conditions = [new LambdaConditionNode<T>(_condition), node]
                };
            }
            else
            {
                _fluentConditionNode = node;
            }
        }
        else
        {
            // AND the new node onto the existing fluent tree
            if (_fluentConditionNode is ConditionGroup existingAnd &&
                existingAnd.Operator.Equals("AND", StringComparison.OrdinalIgnoreCase))
            {
                existingAnd.Conditions.Add(node);
            }
            else
            {
                _fluentConditionNode = new ConditionGroup
                {
                    Operator = "AND",
                    Conditions = [_fluentConditionNode, node]
                };
            }
        }
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

    /// <summary>
    /// Configures this rule to use a structured <see cref="ConditionNode"/> tree evaluated by the provided
    /// <see cref="ConditionEvaluator{T}"/>. Enables full condition debug tree capture at runtime.
    /// The regular evaluation path uses short-circuit logic; the debug path evaluates all children.
    /// </summary>
    public Rule<T> WithStructuredCondition(ConditionNode conditionNode, ConditionEvaluator<T> evaluator)
    {
        ArgumentNullException.ThrowIfNull(conditionNode);
        ArgumentNullException.ThrowIfNull(evaluator);

        _structuredConditionNode = conditionNode;
        _structuredConditionEvaluator = evaluator;

        // Wire the regular (short-circuit) async evaluation path.
        _asyncConditionWithContext = (input, ctx) => evaluator.EvaluateAsync(input, conditionNode, ctx);
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
    /// 1. Fluent condition tree (.WhenAI / .WhenGroup)
    /// 2. Context-based async condition
    /// 3. Async condition
    /// 4. Context-based sync condition
    /// 5. Sync condition
    /// </summary>
    public async Task<bool> EvaluateAsync(T input, IRuleContext context)
    {
        if (_fluentConditionNode != null)
        {
            var fluentEval = new FluentConditionEvaluator<T>(_fluentAiEvaluator, _enableFluentAi);
            return await fluentEval.EvaluateAsync(input, _fluentConditionNode);
        }
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

    /// <summary>
    /// Evaluates the rule using execution options (AI timeout, failure strategy, caching, logger).
    /// Used by the engine as the preferred evaluation path for <see cref="Rule{T}"/> instances.
    /// Metrics are reported into <paramref name="aiMetrics"/> when provided.
    /// </summary>
    internal async Task<bool> EvaluateWithOptionsAsync(
        T input,
        IRuleContext context,
        RuleExecutionOptions<T> options,
        AiMetricsTracker? aiMetrics = null,
        CancellationToken ct = default)
    {
        if (_fluentConditionNode != null)
        {
            var fluentEval = BuildFluentEvaluator(options, aiMetrics);
            return await fluentEval.EvaluateAsync(input, _fluentConditionNode, ct);
        }

        // Fall through to existing paths
        return await EvaluateAsync(input, context);
    }

    /// <summary>
    /// Evaluates the rule with debug tree capture and execution options.
    /// Covers both fluent and structured condition paths.
    /// </summary>
    internal async Task<bool> EvaluateWithDebugAndOptionsAsync(
        T input,
        IRuleContext context,
        RuleExecutionOptions<T> options,
        AiMetricsTracker? aiMetrics,
        Action<DebugConditionNode?> onDebugTree,
        CancellationToken ct = default)
    {
        if (_fluentConditionNode != null && _structuredConditionNode == null)
        {
            var fluentEval = BuildFluentEvaluator(options, aiMetrics);
            var (fluentResult, fluentTree) = await fluentEval.EvaluateWithDebugAsync(input, _fluentConditionNode, ct);
            onDebugTree(fluentTree);
            return fluentResult;
        }

        if (_structuredConditionNode != null && _structuredConditionEvaluator != null)
        {
            var (result, tree) = await _structuredConditionEvaluator.EvaluateWithDebugAsync(
                input, _structuredConditionNode, context, ct);
            onDebugTree(tree);
            return result;
        }

        var matched = await EvaluateAsync(input, context);
        onDebugTree(null);
        return matched;
    }

    private FluentConditionEvaluator<T> BuildFluentEvaluator(
        RuleExecutionOptions<T> options,
        AiMetricsTracker? aiMetrics)
    {
        return new FluentConditionEvaluator<T>(
            _fluentAiEvaluator,
            _enableFluentAi && options.EnableAiConditions,
            options.AiTimeout,
            options.AiFailureStrategy,
            options.EnableAiCaching,
            options.AiLogger,
            aiMetrics);
    }

    /// <summary>
    /// Evaluates the rule using its structured or fluent condition, capturing the full debug
    /// condition tree via <paramref name="onDebugTree"/>. Falls back to the regular evaluation
    /// path (without a debug tree) when no structured or fluent condition is configured.
    /// Uses defaults for AI execution options (no timeout, ReturnFalse strategy, no caching).
    /// </summary>
    internal async Task<bool> EvaluateWithDebugAsync(
        T input,
        IRuleContext context,
        Action<DebugConditionNode?> onDebugTree,
        CancellationToken ct = default)
    {
        if (_fluentConditionNode != null && _structuredConditionNode == null)
        {
            var fluentEval = new FluentConditionEvaluator<T>(_fluentAiEvaluator, _enableFluentAi);
            var (fluentResult, fluentTree) = await fluentEval.EvaluateWithDebugAsync(input, _fluentConditionNode, ct);
            onDebugTree(fluentTree);
            return fluentResult;
        }

        if (_structuredConditionNode != null && _structuredConditionEvaluator != null)
        {
            var (result, tree) = await _structuredConditionEvaluator.EvaluateWithDebugAsync(
                input, _structuredConditionNode, context, ct);
            onDebugTree(tree);
            return result;
        }

        var matched = await EvaluateAsync(input, context);
        onDebugTree(null);
        return matched;
    }
}