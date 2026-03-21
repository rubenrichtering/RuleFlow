using RuleFlow.Abstractions;
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
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));

        context ??= new RuleContext();

        var result = new RuleResult();

        // Create root node for the RuleSet
        var root = new RuleExecutionNode
        {
            Name = ruleSet.Name,
            Type = "Group",
            Executed = true,
            Priority = 0
        };

        var shouldStop = EvaluateRuleSet(input, ruleSet, context, result, root, groupPath: null);

        result.Root = root;

        return result;
    }

    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));

        context ??= new RuleContext();

        var result = new RuleResult();

        // Create root node for the RuleSet
        var root = new RuleExecutionNode
        {
            Name = ruleSet.Name,
            Type = "Group",
            Executed = true,
            Priority = 0
        };

        var shouldStop = await EvaluateRuleSetAsync(input, ruleSet, context, result, root, groupPath: null);

        result.Root = root;

        return result;
    }

    private bool EvaluateRuleSet<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext context,
        RuleResult result,
        RuleExecutionNode parentNode,
        string? groupPath = null)
    {
        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Evaluate rules in current RuleSet
        foreach (var rule in orderedRules)
        {
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

            if (matched)
            {
                rule.Execute(input, context);

                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    ruleNode.StoppedProcessing = true;
                    return true; // Signal to stop
                }
            }
        }

        // Evaluate groups in insertion order
        foreach (var group in ruleSet.Groups)
        {
            var groupNode = new RuleExecutionNode
            {
                Name = group.Name,
                Type = "Group",
                Executed = true,
                Priority = 0
            };

            parentNode.Children.Add(groupNode);

            var shouldStop = EvaluateRuleSet(input, group, context, result, groupNode, groupPath: group.Name);

            if (shouldStop)
            {
                // Mark remaining groups/rules in this set as not executed
                MarkRemainingAsSkipped(groupNode, result);
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
        string? groupPath = null)
    {
        var orderedRules = ruleSet.Rules
            .OrderByDescending(r => r.Priority)
            .ToList();

        // Evaluate rules in current RuleSet
        foreach (var rule in orderedRules)
        {
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

            if (matched)
            {
                await rule.ExecuteAsync(input, context);

                if (rule.StopProcessing)
                {
                    execution.StoppedProcessing = true;
                    ruleNode.StoppedProcessing = true;
                    return true; // Signal to stop
                }
            }
        }

        // Evaluate groups in insertion order
        foreach (var group in ruleSet.Groups)
        {
            var groupNode = new RuleExecutionNode
            {
                Name = group.Name,
                Type = "Group",
                Executed = true,
                Priority = 0
            };

            parentNode.Children.Add(groupNode);

            var shouldStop = await EvaluateRuleSetAsync(input, group, context, result, groupNode, groupPath: group.Name);

            if (shouldStop)
            {
                // Mark remaining groups/rules in this set as not executed
                MarkRemainingAsSkipped(groupNode, result);
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