using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Context;

namespace RuleFlow.Core.Engine;

public class RuleEngine : IRuleEngine
{
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        return Evaluate(input, ruleSet, context, options: null);
    }

    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        RuleExecutionOptions<T> options)
    {
        return Evaluate(input, ruleSet, context: null, options);
    }

    public RuleResult Evaluate<T>(
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

        // Create root node for the RuleSet
        var root = new RuleExecutionNode
        {
            Name = ruleSet.Name,
            Type = "Group",
            Executed = true,
            Priority = 0
        };

        var executionState = new ExecutionState();
        var shouldStop = EvaluateRuleSet(input, ruleSet, context, result, root, options, executionState, groupPath: null);

        if (options.EnableExplainability)
        {
            result.Root = root;
        }

        return result;
    }

    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        return await EvaluateAsync(input, ruleSet, context, options: null);
    }

    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        RuleExecutionOptions<T> options)
    {
        return await EvaluateAsync(input, ruleSet, context: null, options);
    }

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

        // Create root node for the RuleSet
        var root = new RuleExecutionNode
        {
            Name = ruleSet.Name,
            Type = "Group",
            Executed = true,
            Priority = 0
        };

        var executionState = new ExecutionState();
        var shouldStop = await EvaluateRuleSetAsync(input, ruleSet, context, result, root, options, executionState, groupPath: null);

        if (options.EnableExplainability)
        {
            result.Root = root;
        }

        return result;
    }

    private bool EvaluateRuleSet<T>(
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
            return false; // Skip this group
        }

        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Evaluate rules in current RuleSet
        foreach (var rule in orderedRules)
        {
            // Check if rule passes metadata filter
            if (options.MetadataFilter != null && !options.MetadataFilter(rule))
            {
                continue; // Skip this rule
            }

            var matched = rule.Evaluate(input, context);

            var execution = new RuleExecution
            {
                RuleName = rule.Name,
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

            if (options.EnableExplainability)
            {
                // Create node for this rule
                var ruleNode = new RuleExecutionNode
                {
                    Name = rule.Name,
                    Type = "Rule",
                    Matched = matched,
                    Reason = rule.Reason,
                    Priority = rule.Priority,
                    Executed = true
                };

                parentNode.Children.Add(ruleNode);
            }

            if (matched)
            {
                rule.Execute(input, context);

                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    if (options.EnableExplainability)
                    {
                        parentNode.Children.Last().StoppedProcessing = true;
                    }
                    return true; // Signal to stop (rule-level takes precedence)
                }

                // Check StopOnFirstMatch option
                if (options.StopOnFirstMatch)
                {
                    executionState.StoppedByFirstMatchOption = true;
                    return true; // Stop execution
                }
            }
        }

        // Evaluate groups in insertion order
        foreach (var group in ruleSet.Groups)
        {
            if (options.EnableExplainability)
            {
                var groupNode = new RuleExecutionNode
                {
                    Name = group.Name,
                    Type = "Group",
                    Executed = true,
                    Priority = 0
                };

                parentNode.Children.Add(groupNode);
            }

            var shouldStop = EvaluateRuleSet(input, group, context, result, parentNode, options, executionState, groupPath: group.Name);

            if (shouldStop)
            {
                if (options.EnableExplainability)
                {
                    // Mark remaining groups/rules in this set as not executed
                    MarkRemainingAsSkipped(parentNode.Children.Last(), result);
                }
                return true; // Stop entire execution
            }
        }

        return false;
    }

    private async Task<bool> EvaluateRuleSetAsync<T>(
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
            return false; // Skip this group
        }

        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Evaluate rules in current RuleSet
        foreach (var rule in orderedRules)
        {
            // Check if rule passes metadata filter
            if (options.MetadataFilter != null && !options.MetadataFilter(rule))
            {
                continue; // Skip this rule
            }

            var matched = await rule.EvaluateAsync(input, context);

            var execution = new RuleExecution
            {
                RuleName = rule.Name,
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

            if (options.EnableExplainability)
            {
                // Create node for this rule
                var ruleNode = new RuleExecutionNode
                {
                    Name = rule.Name,
                    Type = "Rule",
                    Matched = matched,
                    Reason = rule.Reason,
                    Priority = rule.Priority,
                    Executed = true
                };

                parentNode.Children.Add(ruleNode);
            }

            if (matched)
            {
                await rule.ExecuteAsync(input, context);

                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    if (options.EnableExplainability)
                    {
                        parentNode.Children.Last().StoppedProcessing = true;
                    }
                    return true; // Signal to stop (rule-level takes precedence)
                }

                // Check StopOnFirstMatch option
                if (options.StopOnFirstMatch)
                {
                    executionState.StoppedByFirstMatchOption = true;
                    return true; // Stop execution
                }
            }
        }

        // Evaluate groups in insertion order
        foreach (var group in ruleSet.Groups)
        {
            if (options.EnableExplainability)
            {
                var groupNode = new RuleExecutionNode
                {
                    Name = group.Name,
                    Type = "Group",
                    Executed = true,
                    Priority = 0
                };

                parentNode.Children.Add(groupNode);
            }

            var shouldStop = await EvaluateRuleSetAsync(input, group, context, result, parentNode, options, executionState, groupPath: group.Name);

            if (shouldStop)
            {
                if (options.EnableExplainability)
                {
                    // Mark remaining groups/rules in this set as not executed
                    MarkRemainingAsSkipped(parentNode.Children.Last(), result);
                }
                return true; // Stop entire execution
            }
        }

        return false;
    }

    private void MarkRemainingAsSkipped(RuleExecutionNode node, RuleResult result)
    {
        // Mark all children of this node as skipped if they contain unevaluated rules
        foreach (var child in node.Children)
        {
            if (child.Type == "Rule" && !child.Executed)
            {
                child.Executed = false;
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