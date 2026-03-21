using RuleFlow.Abstractions;
using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using RuleFlow.Core.Tests.Helpers;
using Shouldly;
using Xunit;

namespace RuleFlow.Core.Tests.Engine;

internal class TestOrder
{
    public decimal Amount { get; set; }
    public bool RequiresApproval { get; set; }
    public bool LogProcessed { get; set; }
    public TestCustomer? Customer { get; set; }
    public bool IsValid { get; set; }
}

internal class TestCustomer
{
    public bool IsPremium { get; set; }
}

public class ConditionalChainsTests
{
    [Fact]
    public void SingleThen_ShouldExecute()
    {
        // Arrange
        var order = new TestOrder { Amount = 500 };
        var actionExecuted = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Test rule")
                .When(o => o.Amount > 100)
                .Then(o => actionExecuted = true));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        actionExecuted.ShouldBeTrue();
        result.ShouldHaveMatched("Test rule");
    }

    [Fact]
    public void MultipleThen_ShouldExecuteInOrder()
    {
        // Arrange
        var order = new TestOrder { Amount = 500 };
        var executionOrder = new List<string>();

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Multi-step rule")
                .When(o => o.Amount > 100)
                .Then(o => executionOrder.Add("Step1"))
                .Then(o => executionOrder.Add("Step2"))
                .Then(o => executionOrder.Add("Step3")));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        executionOrder.ShouldBe(new[] { "Step1", "Step2", "Step3" });
        result.ShouldHaveMatched("Multi-step rule");
    }

    [Fact]
    public void ThenIf_ShouldExecuteConditionally()
    {
        // Arrange
        var order = new TestOrder { Amount = 1500, Customer = new TestCustomer { IsPremium = true } };
        var premiumNotified = false;
        var standardNotified = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Notification rule")
                .When(o => o.Amount > 1000)
                .ThenIf(o => o.Customer?.IsPremium == true, o => premiumNotified = true)
                .ThenIf(o => o.Customer?.IsPremium == false, o => standardNotified = true)
                .Then(o => o.LogProcessed = true));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        premiumNotified.ShouldBeTrue("Premium condition should be true");
        standardNotified.ShouldBeFalse("Standard condition should be false");
        order.LogProcessed.ShouldBeTrue("Then should always execute");
        result.ShouldHaveMatched("Notification rule");
    }

    [Fact]
    public void ThenIfAsync_ShouldExecuteConditionally()
    {
        // Arrange
        var order = new TestOrder { Amount = 1500 };
        var asyncActionExecuted = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Async conditional rule")
                .When(o => o.Amount > 1000)
                .ThenIfAsync(
                    async o =>
                    {
                        await Task.Delay(10);
                        return o.Amount > 1200;
                    },
                    async o =>
                    {
                        await Task.Delay(10);
                        asyncActionExecuted = true;
                    }));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules); // Note: Calling sync Evaluate on async rules

        // Assert
        asyncActionExecuted.ShouldBeTrue();
        result.ShouldHaveMatched("Async conditional rule");
    }

    [Fact]
    public async Task ThenIfAsync_ShouldExecuteConditionally_Async()
    {
        // Arrange
        var order = new TestOrder { Amount = 1500 };
        var asyncActionExecuted = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Async conditional rule")
                .When(o => o.Amount > 1000)
                .ThenIfAsync(
                    async o =>
                    {
                        await Task.Delay(10);
                        return o.Amount > 1200;
                    },
                    async o =>
                    {
                        await Task.Delay(10);
                        asyncActionExecuted = true;
                    }));

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(order, rules);

        // Assert
        asyncActionExecuted.ShouldBeTrue();
        result.ShouldHaveMatched("Async conditional rule");
    }

    [Fact]
    public void ThenIf_WithContext_ShouldExecuteConditionally()
    {
        // Arrange
        var order = new TestOrder { Amount = 1500 };
        var contextUsed = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Context-aware conditional")
                .When(o => o.Amount > 1000)
                .ThenIf(
                    (o, ctx) => o.Amount > 1200,
                    (o, ctx) =>
                    {
                        contextUsed = true;
                        // Context can be used for rule execution context
                    }));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        contextUsed.ShouldBeTrue();
        result.ShouldHaveMatched("Context-aware conditional");
    }

    [Fact]
    public void MixedSyncAndAsync_ShouldExecuteInOrder()
    {
        // Arrange
        var order = new TestOrder { Amount = 500 };
        var executionOrder = new List<string>();

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Mixed sync/async")
                .When(o => o.Amount > 100)
                .Then(o => executionOrder.Add("SyncStep1"))
                .ThenAsync(async o =>
                {
                    await Task.Delay(10);
                    executionOrder.Add("AsyncStep2");
                })
                .Then(o => executionOrder.Add("SyncStep3"))
                .ThenAsync(async o =>
                {
                    await Task.Delay(10);
                    executionOrder.Add("AsyncStep4");
                }));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        executionOrder.ShouldBe(new[] { "SyncStep1", "AsyncStep2", "SyncStep3", "AsyncStep4" });
    }

    [Fact]
    public void NoActionSteps_ShouldMaintainBackwardCompatibility()
    {
        // Arrange
        var order = new TestOrder { Amount = 500 };

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Old-style rule")
                .When(o => o.Amount > 100)
                .Then(o => o.IsValid = true)); // Old-style single Then

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        order.IsValid.ShouldBeTrue();
        result.ShouldHaveMatched("Old-style rule");
    }

    [Fact]
    public void ThenIf_WhenPredicateFalse_ShouldNotExecute()
    {
        // Arrange
        var order = new TestOrder { Amount = 500 }; // Doesn't meet premium threshold
        var premiumAction = false;

        var rules = RuleSet.For<TestOrder>("Test")
            .Add(Rule.For<TestOrder>("Premium check")
                .When(o => o.Amount > 0)
                .ThenIf(o => o.Amount > 1000, o => premiumAction = true)
                .Then(o => order.IsValid = true));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, rules);

        // Assert
        premiumAction.ShouldBeFalse("Premium action should be skipped");
        order.IsValid.ShouldBeTrue("Unconditional Then should execute");
    }
}