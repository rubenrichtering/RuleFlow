using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Core.Context;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Rules;

public class WhenAIFluentTests
{
    // ── Test model ─────────────────────────────────────────────────────────────

    private sealed record Transaction(decimal Amount, string Supplier, string Country);

    private static readonly DefaultRuleContext Context = DefaultRuleContext.Instance;

    // ── Fluent API construction tests ──────────────────────────────────────────

    [Fact]
    public void WhenAI_returns_same_rule_instance_for_chaining()
    {
        var rule = Rule<Transaction>.For("Fraud check");

        var returned = rule.WhenAI("Is this suspicious?");

        returned.ShouldBeSameAs(rule);
    }

    [Fact]
    public void WhenAI_with_selector_returns_same_rule_instance_for_chaining()
    {
        var rule = Rule<Transaction>.For("Fraud check");

        var returned = rule.WhenAI("Is this suspicious?", x => new { x.Amount, x.Supplier });

        returned.ShouldBeSameAs(rule);
    }

    [Fact]
    public void WhenAI_builds_AiConditionNode_as_fluent_condition()
    {
        var rule = Rule<Transaction>.For("Fraud check")
            .WhenAI("Is this suspicious?");

        rule.HasFluentCondition.ShouldBeTrue();
        rule.FluentConditionNode.ShouldBeOfType<AiConditionNode>();
        ((AiConditionNode)rule.FluentConditionNode!).Prompt.ShouldBe("Is this suspicious?");
    }

    [Fact]
    public void WhenAI_with_selector_stores_selector_on_node()
    {
        Func<Transaction, object> selector = x => new { x.Amount };
        var rule = Rule<Transaction>.For("Fraud check")
            .WhenAI("Is this suspicious?", selector);

        var aiNode = (AiConditionNode)rule.FluentConditionNode!;
        aiNode.InputSelector.ShouldNotBeNull();
    }

    [Fact]
    public void When_then_WhenAI_creates_AND_group()
    {
        var rule = Rule<Transaction>.For("Fraud check")
            .When(x => x.Amount > 1000)
            .WhenAI("Is this suspicious?");

        rule.HasFluentCondition.ShouldBeTrue();
        var group = rule.FluentConditionNode.ShouldBeOfType<ConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Conditions.Count.ShouldBe(2);
        group.Conditions[1].ShouldBeOfType<AiConditionNode>();
    }

    [Fact]
    public void WhenAI_multiple_times_appends_to_AND_group()
    {
        var rule = Rule<Transaction>.For("Fraud check")
            .WhenAI("Is this suspicious?")
            .WhenAI("Is this high value?");

        var group = rule.FluentConditionNode.ShouldBeOfType<ConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Conditions.Count.ShouldBe(2);
        ((AiConditionNode)group.Conditions[0]).Prompt.ShouldBe("Is this suspicious?");
        ((AiConditionNode)group.Conditions[1]).Prompt.ShouldBe("Is this high value?");
    }

    [Fact]
    public void WithAiEvaluator_returns_same_rule_instance_for_chaining()
    {
        var rule = Rule<Transaction>.For("Fraud check");
        var evaluator = new StubAiEvaluator(true);

        var returned = rule.WithAiEvaluator(evaluator);

        returned.ShouldBeSameAs(rule);
    }

    // ── Validation tests ───────────────────────────────────────────────────────

    [Fact]
    public void WhenAI_null_prompt_throws_ArgumentException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentException>(() => rule.WhenAI(null!));
    }

    [Fact]
    public void WhenAI_empty_prompt_throws_ArgumentException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentException>(() => rule.WhenAI(""));
    }

    [Fact]
    public void WhenAI_whitespace_prompt_throws_ArgumentException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentException>(() => rule.WhenAI("   "));
    }

    [Fact]
    public void WhenAI_with_null_selector_throws_ArgumentNullException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentNullException>(() => rule.WhenAI("Prompt", null!));
    }

    [Fact]
    public void WithAiEvaluator_null_evaluator_throws_ArgumentNullException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentNullException>(() => rule.WithAiEvaluator(null!));
    }

    // ── Execution: AI disabled ─────────────────────────────────────────────────

    [Fact]
    public async Task WhenAI_skips_and_returns_false_when_no_evaluator_registered()
    {
        var rule = Rule<Transaction>.For("Fraud check")
            .WhenAI("Is this suspicious?")
            .Then(_ => { });

        var result = await rule.EvaluateAsync(new Transaction(500m, "Acme", "US"), Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WhenAI_skips_and_returns_false_when_evaluator_disabled()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub, enabled: false)
            .WhenAI("Is this suspicious?")
            .Then(_ => { });

        var result = await rule.EvaluateAsync(new Transaction(500m, "Acme", "US"), Context);

        result.ShouldBeFalse();
        stub.CallCount.ShouldBe(0);
    }

    // ── Execution: AI enabled ──────────────────────────────────────────────────

    [Fact]
    public async Task WhenAI_evaluates_correctly_when_ai_returns_true()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenAI("Is this suspicious?")
            .Then(_ => { });

        var result = await rule.EvaluateAsync(new Transaction(500m, "Acme", "US"), Context);

        result.ShouldBeTrue();
        stub.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task WhenAI_evaluates_correctly_when_ai_returns_false()
    {
        var stub = new StubAiEvaluator(returnResult: false);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenAI("Is this suspicious?")
            .Then(_ => { });

        var result = await rule.EvaluateAsync(new Transaction(500m, "Acme", "US"), Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WhenAI_passes_correct_prompt_to_ai_evaluator()
    {
        var recording = new RecordingAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(recording)
            .WhenAI("Check fraud risk for supplier");

        await rule.EvaluateAsync(new Transaction(500m, "Acme", "US"), Context);

        recording.LastPrompt.ShouldBe("Check fraud risk for supplier");
    }

    [Fact]
    public async Task WhenAI_passes_full_input_to_ai_evaluator()
    {
        var recording = new RecordingAiEvaluator(returnResult: true);
        var input = new Transaction(2500m, "ShellCo", "RU");
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(recording)
            .WhenAI("Is this suspicious?");

        await rule.EvaluateAsync(input, Context);

        recording.LastInput.ShouldBe(input);
    }

    // ── Execution: Mixed When + WhenAI ─────────────────────────────────────────

    [Fact]
    public async Task When_and_WhenAI_both_true_returns_true()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .When(x => x.Amount > 1000)
            .WhenAI("Is this suspicious?")
            .Then(_ => { });

        var result = await rule.EvaluateAsync(new Transaction(2000m, "ShellCo", "RU"), Context);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task When_false_and_WhenAI_true_returns_false()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .When(x => x.Amount > 1000)
            .WhenAI("Is this suspicious?");

        var result = await rule.EvaluateAsync(new Transaction(50m, "Acme", "US"), Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task When_true_and_WhenAI_false_returns_false()
    {
        var stub = new StubAiEvaluator(returnResult: false);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .When(x => x.Amount > 1000)
            .WhenAI("Is this suspicious?");

        var result = await rule.EvaluateAsync(new Transaction(2000m, "Acme", "US"), Context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task When_lambda_short_circuits_before_calling_ai_when_false()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .When(x => x.Amount > 9999)  // never true
            .WhenAI("Is this suspicious?");

        await rule.EvaluateAsync(new Transaction(50m, "Acme", "US"), Context);

        // Lambda false → short-circuit; AI evaluator is never called
        stub.CallCount.ShouldBe(0);
    }

    // ── WhenGroup tests ────────────────────────────────────────────────────────

    [Fact]
    public void WhenGroup_returns_same_rule_instance_for_chaining()
    {
        var rule = Rule<Transaction>.For("Test");

        var returned = rule.WhenGroup(g => g.WhenAI("Suspicious?"));

        returned.ShouldBeSameAs(rule);
    }

    [Fact]
    public void WhenGroup_builds_ConditionGroup_with_AND_default()
    {
        var rule = Rule<Transaction>.For("Test")
            .WhenGroup(g => g
                .When(x => x.Amount > 100)
                .WhenAI("Is this suspicious?"));

        var group = rule.FluentConditionNode.ShouldBeOfType<ConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Conditions.Count.ShouldBe(2);
    }

    [Fact]
    public void WhenGroup_builds_ConditionGroup_with_OR_when_specified()
    {
        var rule = Rule<Transaction>.For("Test")
            .WhenGroup(g => g
                .Or()
                .When(x => x.Amount > 5000)
                .WhenAI("Is this high risk?"));

        var group = rule.FluentConditionNode.ShouldBeOfType<ConditionGroup>();
        group.Operator.ShouldBe("OR");
    }

    [Fact]
    public async Task WhenGroup_AND_evaluates_correctly()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenGroup(g => g
                .When(x => x.Amount > 1000)
                .WhenAI("Is this suspicious?"));

        // Both true
        (await rule.EvaluateAsync(new Transaction(2000m, "ShellCo", "RU"), Context)).ShouldBeTrue();
    }

    [Fact]
    public async Task WhenGroup_AND_returns_false_when_lambda_false()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenGroup(g => g
                .When(x => x.Amount > 1000)
                .WhenAI("Is this suspicious?"));

        // Lambda false → group false
        (await rule.EvaluateAsync(new Transaction(50m, "Acme", "US"), Context)).ShouldBeFalse();
    }

    [Fact]
    public async Task WhenGroup_OR_returns_true_when_lambda_true_and_ai_false()
    {
        var stub = new StubAiEvaluator(returnResult: false);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenGroup(g => g
                .Or()
                .When(x => x.Amount > 1000)
                .WhenAI("Is this suspicious?"));

        // Lambda true → OR is true
        (await rule.EvaluateAsync(new Transaction(2000m, "Acme", "US"), Context)).ShouldBeTrue();
    }

    [Fact]
    public async Task WhenGroup_OR_returns_true_when_lambda_false_and_ai_true()
    {
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenGroup(g => g
                .Or()
                .When(x => x.Amount > 9999)  // never true
                .WhenAI("Is this suspicious?"));

        // AI true → OR is true
        (await rule.EvaluateAsync(new Transaction(50m, "Acme", "US"), Context)).ShouldBeTrue();
    }

    [Fact]
    public void WhenGroup_null_configure_throws_ArgumentNullException()
    {
        var rule = Rule<Transaction>.For("Test");

        Should.Throw<ArgumentNullException>(() => rule.WhenGroup(null!));
    }

    // ── Debug output tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task WhenAI_produces_DebugAiConditionLeaf_in_debug_tree()
    {
        DebugConditionNode? capturedTree = null;
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenAI("Is this suspicious?");

        await rule.EvaluateWithDebugAsync(
            new Transaction(500m, "Acme", "US"),
            Context,
            tree => capturedTree = tree);

        var leaf = capturedTree.ShouldBeOfType<DebugAiConditionLeaf>();
        leaf.AiPrompt.ShouldBe("Is this suspicious?");
        leaf.AiEvaluated.ShouldBeTrue();
        leaf.Result.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenAI_disabled_produces_DebugAiConditionLeaf_with_AiEvaluated_false()
    {
        DebugConditionNode? capturedTree = null;
        var rule = Rule<Transaction>.For("Fraud check")
            .WhenAI("Is this suspicious?");  // no evaluator registered

        await rule.EvaluateWithDebugAsync(
            new Transaction(500m, "Acme", "US"),
            Context,
            tree => capturedTree = tree);

        var leaf = capturedTree.ShouldBeOfType<DebugAiConditionLeaf>();
        leaf.AiEvaluated.ShouldBeFalse();
        leaf.Result.ShouldBeFalse();
    }

    [Fact]
    public async Task When_and_WhenAI_produce_DebugConditionGroup_with_lambda_and_ai_nodes()
    {
        DebugConditionNode? capturedTree = null;
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .When(x => x.Amount > 1000)
            .WhenAI("Is this suspicious?");

        await rule.EvaluateWithDebugAsync(
            new Transaction(2000m, "ShellCo", "RU"),
            Context,
            tree => capturedTree = tree);

        var group = capturedTree.ShouldBeOfType<DebugConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Children.Count.ShouldBe(2);
        group.Children[0].ShouldBeOfType<DebugLambdaConditionLeaf>();
        group.Children[1].ShouldBeOfType<DebugAiConditionLeaf>();
        group.Result.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenGroup_produces_DebugConditionGroup_with_all_children()
    {
        DebugConditionNode? capturedTree = null;
        var stub = new StubAiEvaluator(returnResult: true);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenGroup(g => g
                .When(x => x.Amount > 1000)
                .WhenAI("Is this suspicious?"));

        await rule.EvaluateWithDebugAsync(
            new Transaction(2000m, "ShellCo", "RU"),
            Context,
            tree => capturedTree = tree);

        var group = capturedTree.ShouldBeOfType<DebugConditionGroup>();
        group.Operator.ShouldBe("AND");
        group.Children.Count.ShouldBe(2);
        group.Children[0].ShouldBeOfType<DebugLambdaConditionLeaf>();
        group.Children[1].ShouldBeOfType<DebugAiConditionLeaf>();
    }

    [Fact]
    public async Task WhenAI_ai_metadata_appears_in_debug_leaf()
    {
        DebugConditionNode? capturedTree = null;
        var stub = new StubAiEvaluator(returnResult: true, reason: "Pattern match", confidence: 0.92);
        var rule = Rule<Transaction>.For("Fraud check")
            .WithAiEvaluator(stub)
            .WhenAI("Is this suspicious?");

        await rule.EvaluateWithDebugAsync(
            new Transaction(500m, "Acme", "US"),
            Context,
            tree => capturedTree = tree);

        var leaf = capturedTree.ShouldBeOfType<DebugAiConditionLeaf>();
        leaf.AiReason.ShouldBe("Pattern match");
        leaf.AiConfidence.ShouldBe(0.92);
    }

    // ── Test doubles ───────────────────────────────────────────────────────────

    private sealed class StubAiEvaluator(
        bool returnResult,
        string? reason = null,
        double? confidence = null) : IAiConditionEvaluator<Transaction>
    {
        public int CallCount { get; private set; }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new AiConditionResult
            {
                Result = returnResult,
                Reason = reason,
                Confidence = confidence
            });
        }
    }

    private sealed class RecordingAiEvaluator(bool returnResult) : IAiConditionEvaluator<Transaction>
    {
        public string? LastPrompt { get; private set; }
        public Transaction? LastInput { get; private set; }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            LastPrompt = prompt;
            LastInput = input;
            return Task.FromResult(new AiConditionResult { Result = returnResult });
        }
    }
}
