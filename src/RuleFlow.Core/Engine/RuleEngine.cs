using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Context;
using RuleFlow.Core.Rules;

namespace RuleFlow.Core.Engine;

/// <summary>
/// RuleEngine implements a single, unified async-first execution pipeline.
/// All execution paths (sync and async) delegate to a single internal async method.
/// </summary>
public class RuleEngine : IRuleEngine
{
    /// <summary>
    /// Evaluates rules against input synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        return Evaluate(input, ruleSet, context, options: null);
    }

    /// <summary>
    /// Evaluates rules against input with execution options synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        RuleExecutionOptions<T> options)
    {
        return Evaluate(input, ruleSet, context: null, options);
    }

    /// <summary>
    /// Evaluates rules against input with context and options synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context,
        RuleExecutionOptions<T>? options)
    {
        return EvaluateAsync(input, ruleSet, context, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Evaluates rules against input asynchronously.
    /// Delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        return await EvaluateAsync(input, ruleSet, context, options: null);
    }

    /// <summary>
    /// Evaluates rules against input with execution options asynchronously.
    /// Delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        RuleExecutionOptions<T> options)
    {
        return await EvaluateAsync(input, ruleSet, context: null, options);
    }

    /// <summary>
    /// Evaluates rules against input with context and options asynchronously.
    /// Main entry point that delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context,
        RuleExecutionOptions<T>? options)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));

        context ??= new RuleContext();
        options ??= new RuleExecutionOptions<T>();

        var result = new RuleResult();
        var executionState = new ExecutionState();

        // Create root node for the RuleSet
        var root = new RuleExecutionNode
        {
            Name = ruleSet.Name,
            Type = "Group",
            Executed = true,
            Priority = 0
        };

        // Single unified execution pipeline
        await EvaluateInternalAsync(input, ruleSet, context, result, root, options, executionState, groupPath: null);

        // Attach explainability tree if enabled
        if (options.EnableExplainability)
        {
            result.Root = root;
        }

        return result;
    }

    /// <summary>
    /// SINGLE UNIFIED EXECUTION METHOD - The source of truth for all rule evaluation and execution.
    /// 
    /// Responsibilities:
    /// - Evaluate conditions (respecting async priority)
    /// - Record execution results with clear state tracking (Executed/Skipped/Matched)
    /// - Track action-level execution
    /// - Execute actions (respecting async priority)
    /// - Handle StopProcessing and StopOnFirstMatch
    /// - Process groups recursively (using the same pipeline)
    /// - Apply metadata filters and inclusion filters
    /// - Build explainability tree
    /// 
    /// All execution paths (sync and async) flow through this method.
    /// </summary>
    private async Task EvaluateInternalAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext context,
        RuleResult result,
        RuleExecutionNode parentNode,
        RuleExecutionOptions<T> options,
        ExecutionState executionState,
        string? groupPath = null)
    {
        // Check if this group should be included
        if (groupPath != null && options.IncludeGroups != null && !options.IncludeGroups.Contains(groupPath))
        {
            return; // Skip this group
        }

        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority);

        // === EVALUATE RULES IN CURRENT RULESET ===
        foreach (var rule in orderedRules)
        {
            // 1. Check metadata filter
            if (options.MetadataFilter != null && !options.MetadataFilter(rule))
            {
                // Rule is skipped due to metadata filter
                var skippedExecution = new RuleExecution
                {
                    RuleName = rule.Name,
                    Executed = false,
                    Skipped = true,
                    SkipReason = "MetadataFilter",
                    Matched = false,
                    Reason = rule.Reason,
                    Priority = rule.Priority,
                    GroupName = groupPath
                };

                // Copy metadata from rule to execution
                foreach (var kvp in rule.Metadata)
                {
                    skippedExecution.Metadata[kvp.Key] = kvp.Value;
                }

                result.Executions.Add(skippedExecution);

                // Build explainability tree node for skipped rule
                if (options.EnableExplainability)
                {
                    var skippedRuleNode = new RuleExecutionNode
                    {
                        Name = rule.Name,
                        Type = "Rule",
                        Executed = false,
                        Skipped = true,
                        SkipReason = "MetadataFilter",
                        Matched = false,
                        Reason = rule.Reason,
                        Priority = rule.Priority
                    };

                    parentNode.Children.Add(skippedRuleNode);
                }

                continue; // Skip to next rule
            }

            // 2. Evaluate condition (async-first)
            var matched = await rule.EvaluateAsync(input, context);

            // 3. Record execution with clear states
            var execution = new RuleExecution
            {
                RuleName = rule.Name,
                Executed = true,  // Rule was evaluated
                Skipped = false,   // Rule was not skipped
                Matched = matched,
                Reason = rule.Reason,
                Priority = rule.Priority,
                GroupName = groupPath
            };

            // Copy metadata from rule to execution
            foreach (var kvp in rule.Metadata)
            {
                execution.Metadata[kvp.Key] = kvp.Value;
            }

            result.Executions.Add(execution);

            // 4. Build explainability tree
            if (options.EnableExplainability)
            {
                var ruleNode = new RuleExecutionNode
                {
                    Name = rule.Name,
                    Type = "Rule",
                    Executed = true,
                    Skipped = false,
                    Matched = matched,
                    Reason = rule.Reason,
                    Priority = rule.Priority
                };

                parentNode.Children.Add(ruleNode);

                // 5. If matched, execute actions and track them
                if (matched)
                {
                    var actionExecutions = await ExecuteActionsWithTrackingAsync(rule, input, context);
                    execution.Actions.AddRange(actionExecutions);
                    ruleNode.Actions.AddRange(actionExecutions);
                }
            }
            else if (matched)
            {
                // Explainability disabled, but still execute actions
                var actionExecutions = await ExecuteActionsWithTrackingAsync(rule, input, context);
                execution.Actions.AddRange(actionExecutions);
            }

            // 6. Check stop signals if rule matched
            if (matched)
            {
                // Check StopProcessing (rule-level takes precedence)
                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    if (options.EnableExplainability)
                    {
                        parentNode.Children[parentNode.Children.Count - 1].StoppedProcessing = true;
                    }
                    return; // Stop entire pipeline
                }

                // Check StopOnFirstMatch option
                if (options.StopOnFirstMatch)
                {
                    executionState.StoppedByFirstMatchOption = true;
                    return; // Stop entire pipeline
                }
            }
        }

        // === EVALUATE GROUPS IN INSERTION ORDER (Recursive) ===
        foreach (var group in ruleSet.Groups)
        {
            // Add group node to explainability tree
            RuleExecutionNode? groupNode = null;
            if (options.EnableExplainability)
            {
                groupNode = new RuleExecutionNode
                {
                    Name = group.Name,
                    Type = "Group",
                    Executed = true,
                    Priority = 0
                };

                parentNode.Children.Add(groupNode);
            }

            // Recursively evaluate group using the SAME pipeline
            var groupParentNode = groupNode ?? parentNode;
            var previousExecutionCount = result.Executions.Count;

            await EvaluateInternalAsync(input, group, context, result, groupParentNode, options, executionState, groupPath: group.Name);

            // Check if any rule in the group stopped processing
            var newExecutions = result.Executions.Skip(previousExecutionCount);
            if (newExecutions.Any(e => e.StoppedProcessing))
            {
                if (options.EnableExplainability && groupNode != null)
                {
                    MarkRemainingAsSkipped(groupNode, result);
                }
                return; // Stop entire pipeline
            }

            // Check if StopOnFirstMatch was triggered in the group
            if (executionState.StoppedByFirstMatchOption)
            {
                return; // Stop entire pipeline
            }
        }
    }

    /// <summary>
    /// Executes all action steps in a rule and tracks which ones executed vs. were skipped.
    /// Returns a list of ActionExecution records for explainability.
    /// </summary>
    private async Task<List<ActionExecution>> ExecuteActionsWithTrackingAsync<T>(
        IRule<T> rule,
        T input,
        IRuleContext context)
    {
        var actionExecutions = new List<ActionExecution>();
        
        // Get the action steps from the rule
        var rule_impl = rule as Rule<T>;
        if (rule_impl == null)
            return actionExecutions; // No action steps available

        var actionSteps = rule_impl.GetActionSteps();

        foreach (var step in actionSteps)
        {
            var actionDesc = $"{step.Label}";
            
            // Check if this is a conditional step
            if (step.PredicateAsync == null)
            {
                // Unconditional action - always execute
                await step.ExecuteAsync(input, context);
                
                actionExecutions.Add(new ActionExecution
                {
                    Description = actionDesc,
                    Executed = true,
                    Skipped = false
                });
            }
            else
            {
                // Conditional action - check predicate first
                bool predicatePassed = await step.PredicateAsync(input, context);
                
                if (predicatePassed)
                {
                    await step.ExecuteAsync(input, context);
                    
                    actionExecutions.Add(new ActionExecution
                    {
                        Description = actionDesc,
                        Executed = true,
                        Skipped = false
                    });
                }
                else
                {
                    actionExecutions.Add(new ActionExecution
                    {
                        Description = actionDesc,
                        Executed = false,
                        Skipped = true,
                        SkipReason = "PredicateNotMet"
                    });
                }
            }
        }

        return actionExecutions;
    }

    /// <summary>
    /// Marks remaining unevaluated rules in a node as skipped in the explainability tree.
    /// </summary>
    private void MarkRemainingAsSkipped(RuleExecutionNode node, RuleResult result)
    {
        foreach (var child in node.Children)
        {
            if (child.Type == "Rule" && !child.Executed)
            {
                child.Skipped = true;
                child.SkipReason = "StoppedByPreviousRule";
            }
            else if (child.Type == "Group")
            {
                MarkRemainingAsSkipped(child, result);
            }
        }
    }
}

/// <summary>
/// Internal state tracking during rule execution.
/// </summary>
internal class ExecutionState
{
    /// <summary>
    /// Set to true when StopOnFirstMatch option causes early termination.
    /// </summary>
    public bool StoppedByFirstMatchOption { get; set; }
}