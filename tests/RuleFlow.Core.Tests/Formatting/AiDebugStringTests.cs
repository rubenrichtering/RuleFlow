using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Conditions.Operators;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Formatting;

/// <summary>
/// Snapshot-style tests for AI condition rendering in <c>result.ToDebugString()</c>.
/// Verifies that AI conditions produce the correct visual markers, prompt/reason/confidence
/// lines, and warning messages — and that deterministic rules remain unaffected.
/// </summary>
public class AiDebugStringTests
{
    private sealed record Order(decimal Amount, string Category);

    // ── Formatter helpers ────────────────────────────────────────────────────

    private static string Format(RuleExecutionDebugView view)
        => view.ToDebugString();

    private static string FormatRule(DebugRule rule, string ruleSetName = "RS")
    {
        var view = new RuleExecutionDebugView
        {
            RuleSetName = ruleSetName,
            Rules = [rule]
        };
        return Format(view);
    }

    // ── AI success ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugString_AI_Success_True_Shows_AI_Check_Prefix()
    {
        var rule = new DebugRule
        {
            Name = "FraudCheck",
            Executed = true,
            Matched = true,
            Condition = new DebugAiConditionLeaf
            {
                Result = true,
                AiPrompt = "Is this transaction suspicious?",
                AiReason = "High amount + unknown supplier",
                AiConfidence = 0.82,
                AiEvaluated = true
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("[AI ✅] FraudCheck");
        output.ShouldContain("Prompt: Is this transaction suspicious?");
        output.ShouldContain("Reason: High amount + unknown supplier");
        output.ShouldContain("Confidence: 0.82");
        output.ShouldContain("⚠ AI-generated — verify manually");
        output.ShouldNotContain("[AI ❌]");
        output.ShouldNotContain("[AI ⚠ FAILED]");
        output.ShouldNotContain("[AI ⏭ SKIPPED]");
    }

    [Fact]
    public void ToDebugString_AI_Success_False_Shows_AI_X_Prefix()
    {
        var rule = new DebugRule
        {
            Name = "FraudCheck",
            Executed = true,
            Matched = false,
            Condition = new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = "Is this transaction suspicious?",
                AiReason = "Normal transaction pattern",
                AiConfidence = 0.15,
                AiEvaluated = true
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("[AI ❌] FraudCheck");
        output.ShouldContain("Prompt: Is this transaction suspicious?");
        output.ShouldContain("Reason: Normal transaction pattern");
        output.ShouldContain("Confidence: 0.15");
        output.ShouldContain("⚠ AI-generated — verify manually");
        output.ShouldNotContain("[AI ✅]");
    }

    [Fact]
    public void ToDebugString_AI_Success_Without_Optional_Fields_Still_Shows_Prompt_And_Warning()
    {
        var rule = new DebugRule
        {
            Name = "SimpleAiRule",
            Executed = true,
            Matched = true,
            Condition = new DebugAiConditionLeaf
            {
                Result = true,
                AiPrompt = "Approve?",
                AiEvaluated = true
                // No reason or confidence
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("[AI ✅] SimpleAiRule");
        output.ShouldContain("Prompt: Approve?");
        output.ShouldContain("⚠ AI-generated — verify manually");
        output.ShouldNotContain("Reason:");
        output.ShouldNotContain("Confidence:");
    }

    // ── AI failed ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugString_AI_Failed_Shows_Warning_Prefix()
    {
        var rule = new DebugRule
        {
            Name = "FraudCheck",
            Executed = true,
            Matched = false,
            Condition = new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = "Is this transaction suspicious?",
                AiEvaluated = true,
                AiFailed = true
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("[AI ⚠ FAILED] FraudCheck");
        output.ShouldContain("Prompt: Is this transaction suspicious?");
        output.ShouldContain("⚠ AI evaluation failed — fallback applied");
        output.ShouldNotContain("[AI ✅]");
        output.ShouldNotContain("[AI ❌]");
        output.ShouldNotContain("⚠ AI-generated — verify manually");
    }

    // ── AI skipped ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugString_AI_Skipped_Shows_Skipped_Prefix()
    {
        var rule = new DebugRule
        {
            Name = "FraudCheck",
            Executed = true,
            Matched = false,
            Condition = new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = "Is this transaction suspicious?",
                AiEvaluated = false
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("[AI ⏭ SKIPPED] FraudCheck");
        output.ShouldContain("Prompt: Is this transaction suspicious?");
        output.ShouldNotContain("[AI ✅]");
        output.ShouldNotContain("[AI ❌]");
        output.ShouldNotContain("⚠ AI-generated — verify manually");
        output.ShouldNotContain("⚠ AI evaluation failed");
    }

    // ── Deterministic rules unchanged ─────────────────────────────────────────

    [Fact]
    public void ToDebugString_Deterministic_Rule_Uses_Standard_Markers()
    {
        var rule = new DebugRule
        {
            Name = "Amount > 1000",
            Executed = true,
            Matched = true
        };

        var output = FormatRule(rule);

        output.ShouldContain("✅ Amount > 1000");
        output.ShouldNotContain("[AI");
        output.ShouldNotContain("Prompt:");
        output.ShouldNotContain("⚠ AI-generated");
    }

    [Fact]
    public void ToDebugString_Deterministic_Rule_Not_Matched_Uses_X_Marker()
    {
        var rule = new DebugRule
        {
            Name = "Amount > 1000",
            Executed = true,
            Matched = false
        };

        var output = FormatRule(rule);

        output.ShouldContain("❌ Amount > 1000");
        output.ShouldNotContain("[AI");
    }

    // ── Mixed scenario: deterministic + AI ───────────────────────────────────

    [Fact]
    public async Task ToDebugString_Mixed_Rule_Shows_AI_Inline_In_Group()
    {
        var aiEvaluator = new FixedAiEvaluator(result: true, reason: "Risky", confidence: 0.9);
        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            aiEvaluator,
            enableAiConditions: true);

        var conditionNode = new ConditionGroup
        {
            Operator = "AND",
            Conditions =
            [
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 500m },
                new AiConditionNode { Prompt = "Is category high-risk?" }
            ]
        };

        var rule = Rule<Order>.For("RiskRule")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(1000m, "Unknown"), ruleSet);
        var output = result.ToDebugString();

        // Group shown at rule level with standard marker (group, not AI leaf at top)
        output.ShouldContain("✅ RiskRule");

        // AI node rendered inline within condition tree
        output.ShouldContain("[AI ✅]");
        output.ShouldContain("Prompt: Is category high-risk?");
        output.ShouldContain("Reason: Risky");
        output.ShouldContain("⚠ AI-generated — verify manually");

        // Deterministic leaf also rendered
        output.ShouldContain("Amount greater_than");
    }

    [Fact]
    public void ToDebugString_Mixed_View_Both_Rules_Shown_Correctly()
    {
        var view = new RuleExecutionDebugView
        {
            RuleSetName = "MixedTest",
            Rules =
            [
                new DebugRule
                {
                    Name = "DeterministicRule",
                    Executed = true,
                    Matched = true
                },
                new DebugRule
                {
                    Name = "AiRule",
                    Executed = true,
                    Matched = true,
                    Condition = new DebugAiConditionLeaf
                    {
                        Result = true,
                        AiPrompt = "Check risk",
                        AiReason = "Risk confirmed",
                        AiConfidence = 0.7,
                        AiEvaluated = true
                    }
                }
            ]
        };

        var output = Format(view);

        // Deterministic rule: standard marker
        output.ShouldContain("✅ DeterministicRule");
        output.ShouldNotContain("[AI ✅] DeterministicRule");

        // AI rule: AI marker
        output.ShouldContain("[AI ✅] AiRule");
        output.ShouldContain("Prompt: Check risk");
        output.ShouldContain("Reason: Risk confirmed");
        output.ShouldContain("Confidence: 0.7");
    }

    // ── Confidence formatting ──────────────────────────────────────────────────

    [Fact]
    public void ToDebugString_Confidence_Formatted_Without_Trailing_Zeros()
    {
        var rule = new DebugRule
        {
            Name = "R",
            Executed = true,
            Matched = true,
            Condition = new DebugAiConditionLeaf
            {
                Result = true,
                AiPrompt = "p",
                AiConfidence = 1.0,
                AiEvaluated = true
            }
        };

        var output = FormatRule(rule);

        output.ShouldContain("Confidence: 1");
        output.ShouldNotContain("Confidence: 1.00");
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class FixedAiEvaluator : IAiConditionEvaluator<Order>
    {
        private readonly bool _result;
        private readonly string? _reason;
        private readonly double? _confidence;

        public FixedAiEvaluator(bool result, string? reason = null, double? confidence = null)
        {
            _result = result;
            _reason = reason;
            _confidence = confidence;
        }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Order input, CancellationToken ct)
            => Task.FromResult(new AiConditionResult
            {
                Result = _result,
                Reason = _reason,
                Confidence = _confidence
            });
    }
}
