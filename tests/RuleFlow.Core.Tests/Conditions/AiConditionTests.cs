using RuleFlow.Abstractions.Conditions;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Conditions.Operators;
using RuleFlow.Core.Context;
using Shouldly;

namespace RuleFlow.Core.Tests.Conditions;

public class AiConditionTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed record Order(decimal Amount, string Category);

    private static readonly DefaultRuleContext Context = DefaultRuleContext.Instance;

    private static ConditionEvaluator<Order> BuildEvaluator(
        IAiConditionEvaluator<Order>? aiEvaluator = null,
        bool enableAiConditions = false)
    {
        var resolver = new ReflectionFieldResolver<Order>();
        var registry = new DefaultOperatorRegistry();
        var converter = new DefaultValueConverter();
        return new ConditionEvaluator<Order>(resolver, registry, converter, aiEvaluator, enableAiConditions);
    }

    // ── AiConditionNode construction ───────────────────────────────────────────

    [Fact]
    public void AiConditionNode_stores_prompt()
    {
        var node = new AiConditionNode { Prompt = "Is the order high-value?" };
        node.Prompt.ShouldBe("Is the order high-value?");
    }

    [Fact]
    public void AiConditionNode_stores_input_selector()
    {
        Func<object, object> selector = x => x;
        var node = new AiConditionNode { Prompt = "p", InputSelector = selector };
        node.InputSelector.ShouldBeSameAs(selector);
    }

    // ── ConditionValidator ─────────────────────────────────────────────────────

    [Fact]
    public void ConditionValidator_accepts_valid_ai_node()
    {
        var node = new AiConditionNode { Prompt = "Is fraudulent?" };
        Should.NotThrow(() => ConditionValidator.Validate(node));
    }

    [Fact]
    public void ConditionValidator_rejects_ai_node_with_empty_prompt()
    {
        var node = new AiConditionNode { Prompt = "   " };
        Should.Throw<InvalidOperationException>(() => ConditionValidator.Validate(node));
    }

    // ── AI Disabled (no evaluator / disabled flag) ─────────────────────────────

    [Fact]
    public async Task AI_disabled_by_null_evaluator_returns_false_without_call()
    {
        var evaluator = BuildEvaluator(aiEvaluator: null, enableAiConditions: false);
        var node = new AiConditionNode { Prompt = "Is the order fraudulent?" };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, node, Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AI_disabled_flag_prevents_call_even_when_evaluator_is_present()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: false);
        var node = new AiConditionNode { Prompt = "Is the order fraudulent?" };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, node, Context);

        result.ShouldBeFalse();
        mockEvaluator.CallCount.ShouldBe(0);
    }

    // ── AI Enabled ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AI_enabled_calls_evaluator_and_returns_true_result()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);
        var node = new AiConditionNode { Prompt = "Is the order high-value?" };
        var input = new Order(1000m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, node, Context);

        result.ShouldBeTrue();
        mockEvaluator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task AI_enabled_calls_evaluator_and_returns_false_result()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: false);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);
        var node = new AiConditionNode { Prompt = "Is the order suspicious?" };
        var input = new Order(10m, "Books");

        var result = await evaluator.EvaluateAsync(input, node, Context);

        result.ShouldBeFalse();
        mockEvaluator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task AI_enabled_passes_correct_prompt_to_evaluator()
    {
        var mockEvaluator = new RecordingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);
        var node = new AiConditionNode { Prompt = "Check fraud risk" };
        var input = new Order(500m, "Jewelry");

        await evaluator.EvaluateAsync(input, node, Context);

        mockEvaluator.LastPrompt.ShouldBe("Check fraud risk");
        mockEvaluator.LastInput.ShouldBe(input);
    }

    // ── Exception handling ─────────────────────────────────────────────────────

    [Fact]
    public async Task AI_evaluator_exception_is_swallowed_and_returns_false()
    {
        var throwingEvaluator = new ThrowingAiEvaluator();
        var evaluator = BuildEvaluator(aiEvaluator: throwingEvaluator, enableAiConditions: true);
        var node = new AiConditionNode { Prompt = "Will this throw?" };
        var input = new Order(100m, "Sports");

        var result = await evaluator.EvaluateAsync(input, node, Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AI_evaluator_exception_does_not_affect_subsequent_conditions()
    {
        var throwingEvaluator = new ThrowingAiEvaluator();
        var evaluator = BuildEvaluator(aiEvaluator: throwingEvaluator, enableAiConditions: true);

        // Group: AI (throws → false) OR deterministic-true → should be true overall
        var group = new ConditionGroup
        {
            Operator = "OR",
            Conditions =
            [
                new AiConditionNode { Prompt = "Will throw" },
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 50 }
            ]
        };
        var input = new Order(100m, "Sports");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeTrue();
    }

    // ── Mixed conditions (AI + deterministic) ──────────────────────────────────

    [Fact]
    public async Task AND_group_with_AI_true_and_deterministic_true_is_true()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);

        var group = new ConditionGroup
        {
            Operator = "AND",
            Conditions =
            [
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 100 },
                new AiConditionNode { Prompt = "Is the category high-risk?" }
            ]
        };
        var input = new Order(500m, "Jewelry");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeTrue();
        mockEvaluator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task AND_group_short_circuits_before_AI_when_deterministic_is_false()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);

        var group = new ConditionGroup
        {
            Operator = "AND",
            Conditions =
            [
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 10000 },
                new AiConditionNode { Prompt = "Should not be called" }
            ]
        };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeFalse();
        mockEvaluator.CallCount.ShouldBe(0); // short-circuited
    }

    [Fact]
    public async Task OR_group_with_AI_false_and_deterministic_true_is_true()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: false);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);

        var group = new ConditionGroup
        {
            Operator = "OR",
            Conditions =
            [
                new AiConditionNode { Prompt = "Returns false" },
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 100 }
            ]
        };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task OR_group_short_circuits_before_deterministic_when_AI_is_true()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var recordingLeaf = new CountingAiEvaluator(returnResult: false); // used for counting AI calls only
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);

        var group = new ConditionGroup
        {
            Operator = "OR",
            Conditions =
            [
                new AiConditionNode { Prompt = "Returns true — should short-circuit" },
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 99999 } // would be false
            ]
        };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeTrue();
        mockEvaluator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Nested_group_with_AI_is_evaluated_correctly()
    {
        var mockEvaluator = new CountingAiEvaluator(returnResult: true);
        var evaluator = BuildEvaluator(aiEvaluator: mockEvaluator, enableAiConditions: true);

        // (Amount > 100 AND AI=true) OR (Amount > 99999)
        var group = new ConditionGroup
        {
            Operator = "OR",
            Conditions =
            [
                new ConditionGroup
                {
                    Operator = "AND",
                    Conditions =
                    [
                        new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 100 },
                        new AiConditionNode { Prompt = "Nested AI check" }
                    ]
                },
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 99999 }
            ]
        };
        var input = new Order(500m, "Electronics");

        var result = await evaluator.EvaluateAsync(input, group, Context);

        result.ShouldBeTrue();
        mockEvaluator.CallCount.ShouldBe(1);
    }

    // ── AiConditionResult ──────────────────────────────────────────────────────

    [Fact]
    public void AiConditionResult_is_never_deterministic()
    {
        var result = new AiConditionResult { Result = true, Reason = "Looks fine", Confidence = 0.9 };
        result.IsDeterministic.ShouldBeFalse();
    }

    [Fact]
    public void AiConditionResult_stores_all_fields()
    {
        var result = new AiConditionResult
        {
            Result = true,
            Reason = "High confidence match",
            Confidence = 0.95
        };

        result.Result.ShouldBeTrue();
        result.Reason.ShouldBe("High confidence match");
        result.Confidence.ShouldBe(0.95);
    }

    // ── Test doubles ───────────────────────────────────────────────────────────

    private sealed class CountingAiEvaluator(bool returnResult) : IAiConditionEvaluator<Order>
    {
        public int CallCount { get; private set; }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Order input, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new AiConditionResult { Result = returnResult });
        }
    }

    private sealed class RecordingAiEvaluator(bool returnResult) : IAiConditionEvaluator<Order>
    {
        public string? LastPrompt { get; private set; }
        public Order? LastInput { get; private set; }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Order input, CancellationToken ct)
        {
            LastPrompt = prompt;
            LastInput = input;
            return Task.FromResult(new AiConditionResult { Result = returnResult });
        }
    }

    private sealed class ThrowingAiEvaluator : IAiConditionEvaluator<Order>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, Order input, CancellationToken ct)
            => throw new InvalidOperationException("AI service unavailable");
    }
}
