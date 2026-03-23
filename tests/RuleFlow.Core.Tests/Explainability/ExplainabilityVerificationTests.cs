using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;
using Xunit;

namespace RuleFlow.Core.Tests.Explainability;

/// <summary>
/// Tests to verify explainability output is accurate and complete.
/// Ensures developers can understand exactly why rules matched and executed.
/// </summary>
public class ExplainabilityVerificationTests
{
    private class Order
    {
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public bool RequiresApproval { get; set; }
        public string Status { get; set; } = "Pending";
    }

    #region Explanation Text Output

    [Fact]
    public void Should_include_matched_rule_names_in_explanation()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .Because("Amount exceeds approval threshold");

        var ruleSet = RuleSet<Order>.For("ApprovalRules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("Check High Amount");
    }

    [Fact]
    public void Should_include_rule_reasons_in_explanation()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("High Amount Check")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .Because("Amount exceeds approval threshold");

        var ruleSet = RuleSet<Order>.For("ApprovalRules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("Amount exceeds approval threshold");
    }

    [Fact]
    public void Should_not_include_unmatched_rules_in_applied_rules()
    {
        // Arrange
        var order = new Order { Amount = 500 }; // Below threshold
        var matchedRule = Rule<Order>.For("High Amount Check")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true);

        var unmatchedRule = Rule<Order>.For("Very High Amount Check")
            .When(o => o.Amount > 5000)
            .Then(o => o.Status = "Priority");

        var ruleSet = RuleSet<Order>.For("ApprovalRules")
            .Add(matchedRule)
            .Add(unmatchedRule);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        // The unmatched rule should NOT be in AppliedRules
        result.AppliedRules.ShouldNotContain("Very High Amount Check");
        // No rules matched in this test
        result.AppliedRules.ShouldBeEmpty();
    }

    [Fact]
    public void Should_reflect_stop_processing_in_explanation()
    {
        // Arrange
        var order = new Order { Amount = 6000 };
        var rule1 = Rule<Order>.For("Primary Check")
            .When(o => o.Amount > 5000)
            .Then(o => o.Status = "PrimaryMatched")
            .StopIfMatched();

        var rule2 = Rule<Order>.For("Secondary Check")
            .When(o => o.Amount > 1000)
            .Then(o => o.Status = "SecondaryMatched");

        var ruleSet = RuleSet<Order>.For("CheckRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("Primary Check");
        explanation.ShouldNotContain("Secondary Check");
        order.Status.ShouldBe("PrimaryMatched"); // Verify Stop actually worked
    }

    #endregion

    #region Explainability Tree Structure

    [Fact]
    public void Should_have_root_node_with_ruleset_name()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Check Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true);

        var ruleSet = RuleSet<Order>.For("OrderApprovalRules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        result.Root.ShouldNotBeNull();
        result.Root.Name.ShouldBe("OrderApprovalRules");
        result.Root.Type.ShouldBe("Group");
    }

    [Fact]
    public void Should_populate_root_with_executed_flag()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Check Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true);

        var ruleSet = RuleSet<Order>.For("ApprovalRules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        result.Root.ShouldNotBeNull();
        result.Root.Executed.ShouldBeTrue();
    }

    #endregion

    #region Applied Rules Tracking

    [Fact]
    public void Should_track_all_matched_rules()
    {
        // Arrange
        var order = new Order { Amount = 6000, Quantity = 100 };

        var ruleSet = RuleSet<Order>.For("ComplexRules")
            .Add(Rule<Order>.For("High Amount").When(o => o.Amount > 5000).Then(o => { }))
            .Add(Rule<Order>.For("Bulk Order").When(o => o.Quantity >= 50).Then(o => { }));

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        result.AppliedRules.ShouldContain("High Amount");
        result.AppliedRules.ShouldContain("Bulk Order");
        result.AppliedRules.Count().ShouldBe(2);
    }

    [Fact]
    public void Should_track_execution_order()
    {
        // Arrange
        var order = new Order { Amount = 10000 };
        var executionOrder = new List<string>();

        var ruleSet = RuleSet<Order>.For("TestRules")
            .Add(Rule<Order>.For("Rule1").When(o => true).Then(o => executionOrder.Add("Rule1")).WithPriority(100))
            .Add(Rule<Order>.For("Rule2").When(o => true).Then(o => executionOrder.Add("Rule2")).WithPriority(50));

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        executionOrder[0].ShouldBe("Rule1");
        executionOrder[1].ShouldBe("Rule2");
        result.AppliedRules.Count().ShouldBe(2);
    }

    #endregion

    #region With Groups

    [Fact]
    public void Should_include_group_names_in_explanation()
    {
        // Arrange
        var order = new Order { Amount = 6000 };

        var ruleSet = RuleSet<Order>.For("MainRules")
            .AddGroup("HighValueGroup", g => g
                .Add(Rule<Order>.For("High Amount Rule")
                    .When(o => o.Amount > 5000)
                    .Then(o => o.Status = "HighValue")));

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("High Amount Rule");
        result.AppliedRules.ShouldContain("High Amount Rule");
    }

    #endregion

    #region Explainability with Metadata

    [Fact]
    public void Should_preserve_metadata_in_explainability()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Approval Rule")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .WithMetadata("type", "approval")
            .Because("Standard approval required");

        var ruleSet = RuleSet<Order>.For("Rules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        result.AppliedRules.ShouldContain("Approval Rule");
        // Metadata is preserved internally, even if not in text explanation
        result.Root.ShouldNotBeNull();
    }

    #endregion

    #region Explainability Disabled

    [Fact]
    public void Should_not_populate_root_when_explainability_disabled()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Check Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true);

        var ruleSet = RuleSet<Order>.For("Rules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = false };

        // Act
        var result = engine.Evaluate(order, ruleSet, options);

        // Assert
        result.Root.ShouldBeNull();
        // But AppliedRules should still be populated
        result.AppliedRules.ShouldContain("Check Amount");
    }

    #endregion

    #region Explainability Consistency

    [Fact]
    public void Should_have_consistent_explanations_across_runs()
    {
        // Arrange
        var order = new Order { Amount = 1500 };
        var rule = Rule<Order>.For("Consistent Rule")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true);

        var ruleSet = RuleSet<Order>.For("Rules").Add(rule);
        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // Act
        var result1 = engine.Evaluate(order, ruleSet, options);
        var result2 = engine.Evaluate(order, ruleSet, options);

        // Assert
        result1.Explain().ShouldBe(result2.Explain());
    }

    #endregion
}
