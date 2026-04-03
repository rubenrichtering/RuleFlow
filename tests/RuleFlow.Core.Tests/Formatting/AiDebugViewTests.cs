using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Persistence;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Conditions.Operators;
using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Persistence;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Formatting;

/// <summary>
/// Tests for AI condition visibility in <c>result.ToDebugView()</c>.
/// Verifies that AI metadata is correctly mapped from the execution tree to the Debug DTO,
/// and that deterministic conditions remain unaffected.
/// </summary>
public class AiDebugViewTests
{
    private sealed record Order(decimal Amount, string Category);

    // ── DebugAiConditionLeaf construction ────────────────────────────────────

    [Fact]
    public void DebugAiConditionLeaf_IsAi_Is_Always_True()
    {
        var node = new DebugAiConditionLeaf();
        node.IsAi.ShouldBeTrue();
    }

    [Fact]
    public void DebugAiConditionLeaf_Stores_All_Ai_Fields()
    {
        var node = new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Is this suspicious?",
            AiReason = "High amount and unknown supplier",
            AiConfidence = 0.87,
            AiEvaluated = true,
            AiFailed = false
        };

        node.Result.ShouldBeTrue();
        node.AiPrompt.ShouldBe("Is this suspicious?");
        node.AiReason.ShouldBe("High amount and unknown supplier");
        node.AiConfidence.ShouldBe(0.87);
        node.AiEvaluated.ShouldBeTrue();
        node.AiFailed.ShouldBeFalse();
    }

    [Fact]
    public void DebugAiConditionLeaf_Defaults_Mark_As_Not_Evaluated()
    {
        var node = new DebugAiConditionLeaf { AiPrompt = "p" };

        node.AiEvaluated.ShouldBeFalse();
        node.AiFailed.ShouldBeFalse();
        node.AiReason.ShouldBeNull();
        node.AiConfidence.ShouldBeNull();
        node.Result.ShouldBeFalse();
    }

    // ── ToDebugView with manually constructed DebugRule ──────────────────────

    [Fact]
    public void ToDebugView_Rule_With_DebugAiConditionLeaf_Exposes_Ai_Fields()
    {
        var aiLeaf = new DebugAiConditionLeaf
        {
            Result = true,
            AiPrompt = "Is fraud risk?",
            AiReason = "Unusual transaction pattern",
            AiConfidence = 0.92,
            AiEvaluated = true
        };

        var view = new RuleExecutionDebugView
        {
            RuleSetName = "FraudCheck",
            Rules =
            [
                new DebugRule
                {
                    Name = "FraudRule",
                    Executed = true,
                    Matched = true,
                    Condition = aiLeaf
                }
            ]
        };

        var rule = view.Rules[0];
        var condition = rule.Condition.ShouldBeOfType<DebugAiConditionLeaf>();

        condition.IsAi.ShouldBeTrue();
        condition.AiPrompt.ShouldBe("Is fraud risk?");
        condition.AiReason.ShouldBe("Unusual transaction pattern");
        condition.AiConfidence.ShouldBe(0.92);
        condition.AiEvaluated.ShouldBeTrue();
        condition.AiFailed.ShouldBeFalse();
        condition.Result.ShouldBeTrue();
    }

    [Fact]
    public void ToDebugView_Rule_With_Failed_Ai_Has_AiFailed_True()
    {
        var aiLeaf = new DebugAiConditionLeaf
        {
            Result = false,
            AiPrompt = "Will this throw?",
            AiEvaluated = true,
            AiFailed = true
        };

        var view = new RuleExecutionDebugView
        {
            Rules = [new DebugRule { Name = "FailRule", Condition = aiLeaf }]
        };

        var condition = view.Rules[0].Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        condition.AiFailed.ShouldBeTrue();
        condition.AiEvaluated.ShouldBeTrue();
        condition.AiReason.ShouldBeNull();
        condition.AiConfidence.ShouldBeNull();
    }

    [Fact]
    public void ToDebugView_Rule_With_Skipped_Ai_Has_AiEvaluated_False()
    {
        var aiLeaf = new DebugAiConditionLeaf
        {
            Result = false,
            AiPrompt = "Check risk",
            AiEvaluated = false
        };

        var view = new RuleExecutionDebugView
        {
            Rules = [new DebugRule { Name = "SkippedAiRule", Condition = aiLeaf }]
        };

        var condition = view.Rules[0].Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        condition.AiEvaluated.ShouldBeFalse();
        condition.AiFailed.ShouldBeFalse();
    }

    // ── Non-AI rules unchanged ────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_Non_Ai_Rule_Has_Null_Condition_From_Fluent_Rule()
    {
        var obj = new Order(100m, "Books");
        var rule = Rule<Order>.For("Amount Check")
            .When(o => o.Amount > 50)
            .Then(_ => { });
        var ruleSet = RuleSet<Order>.For("RS").Add(rule);

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        view.Rules[0].Condition.ShouldBeNull(); // Fluent rules have no condition tree
    }

    [Fact]
    public void ToDebugView_Non_Ai_Condition_Leaf_Is_Not_DebugAiConditionLeaf()
    {
        // DebugConditionLeaf is a concrete class — it is never a DebugAiConditionLeaf
        DebugConditionNode leaf = new DebugConditionLeaf
        {
            Result = true,
            Field = "Amount",
            Operator = "greater_than",
            Expected = 50
        };

        (leaf is DebugAiConditionLeaf).ShouldBeFalse();
    }

    // ── Pipeline integration: ConditionEvaluator → RuleEngine → DebugView ────

    [Fact]
    public async Task Pipeline_AI_Success_Produces_DebugAiConditionLeaf_In_DebugView()
    {
        var aiEvaluator = new FixedAiEvaluator(
            result: true,
            reason: "High amount + unknown supplier",
            confidence: 0.82);

        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            aiEvaluator,
            enableAiConditions: true);

        var conditionNode = new AiConditionNode { Prompt = "Is this transaction suspicious?" };
        var rule = Rule<Order>.For("FraudCheck")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var input = new Order(9500m, "Unknown");

        var result = await new RuleEngine().EvaluateAsync(input, ruleSet);
        var view = result.ToDebugView();

        view.Rules.Count.ShouldBe(1);
        var debugRule = view.Rules[0];
        debugRule.Matched.ShouldBeTrue();
        debugRule.Name.ShouldBe("FraudCheck");

        var aiNode = debugRule.Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        aiNode.IsAi.ShouldBeTrue();
        aiNode.AiEvaluated.ShouldBeTrue();
        aiNode.AiFailed.ShouldBeFalse();
        aiNode.AiPrompt.ShouldBe("Is this transaction suspicious?");
        aiNode.AiReason.ShouldBe("High amount + unknown supplier");
        aiNode.AiConfidence.ShouldBe(0.82);
        aiNode.Result.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_AI_Disabled_Produces_Skipped_DebugAiConditionLeaf()
    {
        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            aiEvaluator: null,
            enableAiConditions: false);

        var conditionNode = new AiConditionNode { Prompt = "Check fraud risk" };
        var rule = Rule<Order>.For("FraudCheck")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(100m, "Books"), ruleSet);
        var view = result.ToDebugView();

        var aiNode = view.Rules[0].Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        aiNode.AiEvaluated.ShouldBeFalse();
        aiNode.AiFailed.ShouldBeFalse();
        aiNode.AiPrompt.ShouldBe("Check fraud risk");
        aiNode.Result.ShouldBeFalse();
    }

    [Fact]
    public async Task Pipeline_AI_Exception_Produces_Failed_DebugAiConditionLeaf()
    {
        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            new ThrowingAiEvaluator<Order>(),
            enableAiConditions: true);

        var conditionNode = new AiConditionNode { Prompt = "Check fraud risk" };
        var rule = Rule<Order>.For("FraudCheck")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(100m, "Books"), ruleSet);
        var view = result.ToDebugView();

        var aiNode = view.Rules[0].Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        aiNode.AiEvaluated.ShouldBeTrue();
        aiNode.AiFailed.ShouldBeTrue();
        aiNode.Result.ShouldBeFalse();
    }

    [Fact]
    public async Task Pipeline_Deterministic_Leaf_Produces_DebugConditionLeaf_In_DebugView()
    {
        var evaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter());

        var conditionNode = new ConditionLeaf
        {
            Field = "Amount",
            Operator = "greater_than",
            Value = 1000m
        };
        var rule = Rule<Order>.For("HighValueCheck")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(5000m, "Luxury"), ruleSet);
        var view = result.ToDebugView();

        view.Rules[0].Matched.ShouldBeTrue();
        var leaf = view.Rules[0].Condition.ShouldBeOfType<DebugConditionLeaf>();
        leaf.Field.ShouldBe("Amount");
        leaf.Operator.ShouldBe("greater_than");
        leaf.Result.ShouldBeTrue();
        // Verify via base type reference that the returned condition is not an AI node
        DebugConditionNode debugNode = leaf;
        (debugNode is DebugAiConditionLeaf).ShouldBeFalse();
    }

    [Fact]
    public async Task Pipeline_Mixed_Group_Has_Both_Deterministic_And_Ai_Nodes()
    {
        var aiEvaluator = new FixedAiEvaluator(result: true, reason: "Looks suspicious", confidence: 0.75);
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
                new AiConditionNode { Prompt = "Is category suspicious?" }
            ]
        };

        var rule = Rule<Order>.For("FraudRule")
            .WithStructuredCondition(conditionNode, evaluator)
            .Then(_ => { });

        var ruleSet = RuleSet<Order>.For("Orders").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(1000m, "Unknown"), ruleSet);
        var view = result.ToDebugView();

        var group = view.Rules[0].Condition.ShouldBeOfType<DebugConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Children.Count.ShouldBe(2);

        group.Children[0].ShouldBeOfType<DebugConditionLeaf>();
        var aiChild = group.Children[1].ShouldBeOfType<DebugAiConditionLeaf>();
        aiChild.AiEvaluated.ShouldBeTrue();
        aiChild.AiReason.ShouldBe("Looks suspicious");
    }

    // ── RuleDefinitionMapper pipeline ─────────────────────────────────────────

    [Fact]
    public async Task RuleDefinitionMapper_With_AI_Node_Captures_DebugTree()
    {
        var aiEvaluator = new FixedAiEvaluator(
            result: true, reason: "Risk detected", confidence: 0.9);

        var conditionEvaluator = new ConditionEvaluator<Order>(
            new ReflectionFieldResolver<Order>(),
            new DefaultOperatorRegistry(),
            new DefaultValueConverter(),
            aiEvaluator,
            enableAiConditions: true);

        var definition = new RuleDefinition
        {
            Name = "RiskCheck",
            Condition = new AiConditionNode { Prompt = "Is this a risk?" },
            ActionKeys = []
        };

        var registry = new RuleRegistry<Order>();
        var mapper = new RuleDefinitionMapper<Order>(registry, conditionEvaluator);
        var rule = mapper.MapRule(definition);

        var ruleSet = RuleSet<Order>.For("RS").Add(rule);
        var result = await new RuleEngine().EvaluateAsync(new Order(500m, "Electronics"), ruleSet);
        var view = result.ToDebugView();

        view.Rules.Count.ShouldBe(1);
        var aiNode = view.Rules[0].Condition.ShouldBeOfType<DebugAiConditionLeaf>();
        aiNode.AiEvaluated.ShouldBeTrue();
        aiNode.AiPrompt.ShouldBe("Is this a risk?");
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

    private sealed class ThrowingAiEvaluator<T> : IAiConditionEvaluator<T>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, T input, CancellationToken ct)
            => throw new InvalidOperationException("Simulated AI failure");
    }
}
