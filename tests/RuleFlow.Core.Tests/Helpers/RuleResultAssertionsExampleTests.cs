using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Helpers;

/// <summary>
/// Example tests demonstrating the use of RuleResultAssertions helpers.
/// These tests showcase how test helpers improve readability and reduce verbosity.
/// </summary>
public class RuleResultAssertionsExampleTests
{
    private class Order
    {
        public decimal Amount { get; set; }
        public string? Status { get; set; }
    }

    [Fact]
    public void ShouldHaveMatched_MakesTestsMoreReadable()
    {
        // Arrange
        var order = new Order { Amount = 2000 };
        var rule = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "Flagged");

        var ruleSet = RuleSet<Order>.For("Approval Rules").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert
        // OLD WAY: result.AppliedRules.ShouldContain("Check High Amount");
        // NEW WAY:
        result.ShouldHaveMatched("Check High Amount");
    }

    [Fact]
    public void ShouldNotHaveMatched_ClearslyExpressesNegativeCondition()
    {
        // Arrange
        var order = new Order { Amount = 500 };
        var rule = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "Flagged");

        var ruleSet = RuleSet<Order>.For("Approval Rules").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert
        // OLD WAY: result.AppliedRules.ShouldNotContain("Check High Amount");
        // NEW WAY:
        result.ShouldNotHaveMatched("Check High Amount");
    }

    [Fact]
    public void ShouldHaveExecuted_DistinguishesExecutedFromMatched()
    {
        // Arrange
        var order = new Order { Amount = 500 };
        var rule = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "Flagged");

        var ruleSet = RuleSet<Order>.For("Approval Rules").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert
        // The rule was executed (evaluated), but it did not match (condition was false)
        result.ShouldHaveExecuted("Check High Amount");
        result.ShouldNotHaveMatched("Check High Amount");
    }

    [Fact]
    public void ShouldHaveMatchedRules_AssertsCardinality()
    {
        // Arrange
        var order = new Order { Amount = 2000 };
        var rule1 = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "Flagged");
        var rule2 = Rule<Order>.For("Check Very High Amount")
            .When(o => o.Amount > 5000)
            .Then(o => o.Status = "Escalated");

        var ruleSet = RuleSet<Order>.For("Approval Rules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert
        result.ShouldHaveMatchedRules(1);
        result.ShouldHaveMatched("Check High Amount");
        result.ShouldNotHaveMatched("Check Very High Amount");
    }

    [Fact]
    public void MultipleHelpersWorkTogether()
    {
        // Arrange
        var order = new Order { Amount = 7000 };
        var rule1 = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "Flagged");
        var rule2 = Rule<Order>.For("Check Very High Amount")
            .When(o => o.Amount > 5000)
            .Then(o => o.Status = "Escalated");

        var ruleSet = RuleSet<Order>.For("Approval Rules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert - Clear intent, no magic, just readable assertions
        result.ShouldHaveExecutedRules(2);
        result.ShouldHaveMatchedRules(2);
        result.ShouldHaveMatched("Check High Amount");
        result.ShouldHaveMatched("Check Very High Amount");
        result.ShouldNotHaveExecuted("Non-Existent Rule");
    }
}
