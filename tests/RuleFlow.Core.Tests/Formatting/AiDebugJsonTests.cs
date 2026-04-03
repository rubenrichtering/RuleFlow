using System.Text.Json;
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
/// Snapshot-style tests for AI condition serialization in <c>result.ToDebugJson()</c>.
/// Verifies field presence, naming, discriminator, and deterministic ordering.
/// </summary>
public class AiDebugJsonTests
{
    private sealed record Order(decimal Amount, string Category);

    // ── Kind discriminator ────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_DebugAiConditionLeaf_Has_Kind_Ai()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Is this suspicious?",
            AiEvaluated = true
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("kind").GetString().ShouldBe("ai");
    }

    [Fact]
    public void ToDebugJson_DebugConditionLeaf_Has_Kind_Leaf()
    {
        var view = ViewWithCondition(new DebugConditionLeaf
        {
            Result = true,
            Field = "Amount",
            Operator = "greater_than",
            Expected = 1000
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("kind").GetString().ShouldBe("leaf");
    }

    // ── AI field presence ─────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_AI_Success_Contains_All_Ai_Fields()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Is this transaction suspicious?",
            AiReason = "High amount + unknown supplier",
            AiConfidence = 0.82,
            AiEvaluated = true,
            AiFailed = false
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("kind").GetString().ShouldBe("ai");
        condition.GetProperty("isAi").GetBoolean().ShouldBeTrue();
        condition.GetProperty("result").GetBoolean().ShouldBeTrue();
        condition.GetProperty("aiEvaluated").GetBoolean().ShouldBeTrue();
        condition.GetProperty("aiFailed").GetBoolean().ShouldBeFalse();
        condition.GetProperty("aiPrompt").GetString().ShouldBe("Is this transaction suspicious?");
        condition.GetProperty("aiReason").GetString().ShouldBe("High amount + unknown supplier");
        condition.GetProperty("aiConfidence").GetDouble().ShouldBe(0.82);
    }

    [Fact]
    public void ToDebugJson_AI_Failed_Has_AiFailed_True()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = false,
            AiPrompt = "Check risk",
            AiEvaluated = true,
            AiFailed = true
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("aiFailed").GetBoolean().ShouldBeTrue();
        condition.GetProperty("aiEvaluated").GetBoolean().ShouldBeTrue();
        condition.GetProperty("result").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ToDebugJson_AI_Skipped_Has_AiEvaluated_False()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = false,
            AiPrompt = "Check risk",
            AiEvaluated = false
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("aiEvaluated").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ToDebugJson_AI_Optional_Fields_Omitted_When_Null()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Minimal AI",
            AiEvaluated = true
            // AiReason and AiConfidence not set
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        // Null properties should be omitted (WhenWritingNull policy)
        condition.TryGetProperty("aiReason", out _).ShouldBeFalse();
        condition.TryGetProperty("aiConfidence", out _).ShouldBeFalse();
    }

    // ── Non-AI conditions unchanged ───────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Non_Ai_Leaf_Has_No_IsAi_Or_Ai_Fields()
    {
        var view = ViewWithCondition(new DebugConditionLeaf
        {
            Result = true,
            Field = "Amount",
            Operator = "greater_than",
            Expected = 500
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.TryGetProperty("isAi", out _).ShouldBeFalse();
        condition.TryGetProperty("aiPrompt", out _).ShouldBeFalse();
        condition.TryGetProperty("aiReason", out _).ShouldBeFalse();
        condition.GetProperty("field").GetString().ShouldBe("Amount");
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_AI_Condition_Is_Deterministic_Across_Multiple_Calls()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Fraud check",
            AiReason = "Suspicious activity",
            AiConfidence = 0.91,
            AiEvaluated = true
        });

        var json1 = view.ToDebugJson();
        var json2 = view.ToDebugJson();
        var json3 = view.ToDebugJson();

        json2.ShouldBe(json1);
        json3.ShouldBe(json1);
    }

    [Fact]
    public void ToDebugJson_Produces_Valid_Parseable_Json_With_AI_Conditions()
    {
        var view = ViewWithAiCondition(new DebugAiConditionLeaf
        {
            Result = false,
            AiPrompt = "Is this suspicious?",
            AiEvaluated = true,
            AiFailed = true
        });

        Should.NotThrow(() => JsonDocument.Parse(view.ToDebugJson()));
    }

    // ── Mixed group with AI child ─────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Group_With_AI_Child_Serializes_Correctly()
    {
        var view = ViewWithCondition(new DebugConditionGroup
        {
            Operator = "AND",
            Result = true,
            Children =
            [
                new DebugConditionLeaf
                {
                    Result = true,
                    Field = "Amount",
                    Operator = "greater_than",
                    Expected = 500
                },
                new DebugAiConditionLeaf
                {
                    Result = true,
                    AiPrompt = "Is category high-risk?",
                    AiReason = "Unusual category",
                    AiConfidence = 0.75,
                    AiEvaluated = true
                }
            ]
        });

        var doc = JsonDocument.Parse(view.ToDebugJson());
        var condition = GetFirstRuleCondition(doc);

        condition.GetProperty("kind").GetString().ShouldBe("group");
        var children = condition.GetProperty("children");
        children.GetArrayLength().ShouldBe(2);

        children[0].GetProperty("kind").GetString().ShouldBe("leaf");
        var aiChild = children[1];
        aiChild.GetProperty("kind").GetString().ShouldBe("ai");
        aiChild.GetProperty("isAi").GetBoolean().ShouldBeTrue();
        aiChild.GetProperty("aiPrompt").GetString().ShouldBe("Is category high-risk?");
        aiChild.GetProperty("aiConfidence").GetDouble().ShouldBe(0.75);
    }

    // ── Pipeline integration ──────────────────────────────────────────────────

    [Fact]
    public async Task ToDebugJson_From_RuleEngine_With_AI_Condition_Contains_Ai_Fields()
    {
        var aiEvaluator = new FixedAiEvaluator(
            result: true, reason: "Transaction looks risky", confidence: 0.88);

        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            aiEvaluator,
            enableAiConditions: true);

        var rule = Rule<Order>.For("RiskCheck")
            .WithStructuredCondition(
                new AiConditionNode { Prompt = "Is this transaction suspicious?" },
                evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("OrderRuleSet").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(2000m, "Jewelry"), ruleSet);

        var doc = JsonDocument.Parse(result.ToDebugJson());
        var firstRule = doc.RootElement.GetProperty("rules")[0];
        var condition = firstRule.GetProperty("condition");

        condition.GetProperty("kind").GetString().ShouldBe("ai");
        condition.GetProperty("isAi").GetBoolean().ShouldBeTrue();
        condition.GetProperty("aiEvaluated").GetBoolean().ShouldBeTrue();
        condition.GetProperty("aiPrompt").GetString().ShouldBe("Is this transaction suspicious?");
        condition.GetProperty("aiReason").GetString().ShouldBe("Transaction looks risky");
        condition.GetProperty("aiConfidence").GetDouble().ShouldBe(0.88);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RuleExecutionDebugView ViewWithAiCondition(DebugAiConditionLeaf ai)
        => ViewWithCondition(ai);

    private static RuleExecutionDebugView ViewWithCondition(DebugConditionNode condition)
        => new()
        {
            RuleSetName = "TestRS",
            Rules =
            [
                new DebugRule
                {
                    Name = "TestRule",
                    Executed = true,
                    Matched = condition.Result,
                    Condition = condition
                }
            ]
        };

    private static JsonElement GetFirstRuleCondition(JsonDocument doc)
        => doc.RootElement.GetProperty("rules")[0].GetProperty("condition");

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
