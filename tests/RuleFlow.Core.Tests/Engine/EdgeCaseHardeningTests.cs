using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;
using Xunit;

namespace RuleFlow.Core.Tests.Engine;

/// <summary>
/// Edge case and error handling tests for production hardening.
/// Tests boundary conditions, invalid states, and error recovery.
/// </summary>
public class EdgeCaseHardeningTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public TestObject? Nested { get; set; }
    }

    private class Order
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public int ItemCount { get; set; }
        public string? Status { get; set; }
    }

    #region Validation & Null Handling

    [Fact]
    public void Should_throw_when_input_is_null()
    {
        // Arrange
        TestObject? nullObject = null;
        var rule = Rule<TestObject>.For("Test Rule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => engine.Evaluate(nullObject!, ruleSet));
        ex.ParamName.ShouldBe("input");
    }

    [Fact]
    public void Should_throw_when_ruleset_is_null()
    {
        // Arrange
        var obj = new TestObject();
        IRuleSet<TestObject>? nullRuleSet = null;
        var engine = new RuleEngine();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => engine.Evaluate(obj, nullRuleSet!));
        ex.ParamName.ShouldBe("ruleSet");
    }

    [Fact]
    public void Should_handle_null_intermediate_property()
    {
        // Arrange
        var obj = new TestObject { Nested = null };
        var conditionWasEvaluated = false;

        var rule = Rule<TestObject>.For("Nested Check")
            .When(x =>
            {
                conditionWasEvaluated = true;
                return x.Nested != null && x.Nested.Value > 5;
            })
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        conditionWasEvaluated.ShouldBeTrue();
        result.AppliedRules.ShouldNotContain("Nested Check");
    }

    [Fact]
    public void Should_handle_null_string_property()
    {
        // Arrange
        var obj = new TestObject { Name = null };

        var rule = Rule<TestObject>.For("Name Check")
            .When(x => x.Name?.Length > 5)
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldNotContain("Name Check");
    }

    [Fact]
    public void Should_allow_rule_without_condition()
    {
        // Arrange - Rule with no When() clause defaults to true
        var executed = false;
        var obj = new TestObject();

        var rule = Rule<TestObject>.For("No Condition")
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("No Condition");
    }

    [Fact]
    public void Should_allow_rule_without_action()
    {
        // Arrange - Rule with no action should not crash
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("No Action")
            .When(x => x.Value > 5);

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldContain("No Action");
        obj.Value.ShouldBe(10); // Object unchanged
    }

    [Fact]
    public void Should_handle_empty_ruleset()
    {
        // Arrange
        var obj = new TestObject();
        var ruleSet = RuleSet<TestObject>.For("EmptySet");
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldBeEmpty();
    }

    #endregion

    #region Exception Handling

    [Fact]
    public void Should_propagate_exception_in_condition()
    {
        // Arrange
        var obj = new TestObject { Name = null };

        var rule = Rule<TestObject>.For("Bad Condition")
            .When(x => x.Name!.Length > 5) // Will throw NullReferenceException
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => engine.Evaluate(obj, ruleSet));
    }

    [Fact]
    public void Should_propagate_exception_in_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("Bad Action")
            .When(x => x.Value > 5)
            .Then(x => throw new InvalidOperationException("Test error"));

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => engine.Evaluate(obj, ruleSet));
        ex.Message.ShouldBe("Test error");
    }

    [Fact]
    public void Should_stop_processing_on_exception()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule2Executed = false;

        var rule1 = Rule<TestObject>.For("Throws Exception")
            .When(x => x.Value > 5)
            .Then(x => throw new InvalidOperationException("Error in rule1"));

        var rule2 = Rule<TestObject>.For("Should Not Execute")
            .When(x => x.Value > 0)
            .Then(x => { rule2Executed = true; });

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.Evaluate(obj, ruleSet));
        // rule2 should not execute because rule1 threw an exception
        rule2Executed.ShouldBeFalse();
    }

    #endregion

    #region Multiple Rules & Priority Edge Cases

    [Fact]
    public void Should_execute_all_matching_rules_with_same_priority()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule1Executed = false;
        var rule2Executed = false;

        var rule1 = Rule<TestObject>.For("Rule 1")
            .When(x => x.Value > 5)
            .Then(x => { rule1Executed = true; })
            .WithPriority(100);

        var rule2 = Rule<TestObject>.For("Rule 2")
            .When(x => x.Value > 5)
            .Then(x => { rule2Executed = true; })
            .WithPriority(100);

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        rule1Executed.ShouldBeTrue();
        rule2Executed.ShouldBeTrue();
        result.AppliedRules.Count().ShouldBe(2);
    }

    [Fact]
    public void Should_handle_negative_priorities()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executionOrder = new List<int>();

        var rule1 = Rule<TestObject>.For("Negative Priority")
            .When(x => true)
            .Then(x => executionOrder.Add(1))
            .WithPriority(-10);

        var rule2 = Rule<TestObject>.For("Positive Priority")
            .When(x => true)
            .Then(x => executionOrder.Add(2))
            .WithPriority(10);

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executionOrder[0].ShouldBe(2); // Higher priority (10) executes first
        executionOrder[1].ShouldBe(1); // Lower priority (-10) executes second
    }

    [Fact]
    public void Should_preserve_insertion_order_for_same_priority()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executionOrder = new List<string>();

        var rule1 = Rule<TestObject>.For("First Added").When(x => true).Then(x => executionOrder.Add("1"));
        var rule2 = Rule<TestObject>.For("Second Added").When(x => true).Then(x => executionOrder.Add("2"));
        var rule3 = Rule<TestObject>.For("Third Added").When(x => true).Then(x => executionOrder.Add("3"));

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executionOrder.ShouldBe(new[] { "1", "2", "3" });
    }

    #endregion

    #region Stop Processing Edge Cases

    [Fact]
    public void Should_stop_after_first_matching_stop_rule()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executionCount = 0;

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(Rule<TestObject>.For("Stop Rule")
                .When(x => x.Value > 5)
                .Then(x => executionCount++)
                .StopIfMatched())
            .Add(Rule<TestObject>.For("After Stop")
                .When(x => true)
                .Then(x => executionCount++));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executionCount.ShouldBe(1);
        result.AppliedRules.ShouldNotContain("Continue Rule");
        result.AppliedRules.ShouldNotContain("After Stop");
    }

    [Fact]
    public void Should_not_stop_if_stop_rule_condition_is_false()
    {
        // Arrange
        var obj = new TestObject { Value = 2 };
        var executionCount = 0;

        var ruleSet = RuleSet<TestObject>.For("TestSet")
            .Add(Rule<TestObject>.For("Conditional Stop")
                .When(x => x.Value > 5) // False, so rule doesn't match
                .Then(x => executionCount++)
                .StopIfMatched())
            .Add(Rule<TestObject>.For("Continue Rule")
                .When(x => true)
                .Then(x => executionCount++));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executionCount.ShouldBe(1);
        result.AppliedRules.ShouldNotContain("Conditional Stop");
        result.AppliedRules.ShouldContain("Continue Rule");
    }

    #endregion

    #region Metadata & Filtering

    [Fact]
    public void Should_filter_rules_by_metadata()
    {
        // Arrange
        var obj = new TestObject();
        var rule1Executed = false;
        var rule2Executed = false;

        var rule1 = Rule<TestObject>.For("Approval Rule")
            .When(x => true)
            .Then(x => { rule1Executed = true; })
            .WithMetadata("type", "approval");

        var rule2 = Rule<TestObject>.For("Audit Rule")
            .When(x => true)
            .Then(x => { rule2Executed = true; })
            .WithMetadata("type", "audit");

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule1).Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<TestObject>
        {
            MetadataFilter = r => r.Metadata.TryGetValue("type", out var t) && t?.ToString() == "approval"
        };

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        rule1Executed.ShouldBeTrue();
        rule2Executed.ShouldBeFalse();
    }

    [Fact]
    public void Should_handle_null_metadata_value()
    {
        // Arrange
        var obj = new TestObject();

        var rule = Rule<TestObject>.For("No Metadata")
            .When(x => true)
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<TestObject>
        {
            MetadataFilter = r =>
            {
                // Should not crash if metadata is empty
                return r.Metadata.TryGetValue("nonexistent", out _);
            }
        };

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.AppliedRules.ShouldBeEmpty(); // Filter returned false
    }

    #endregion

    #region Type Mismatches & Generics

    [Fact]
    public void Should_handle_type_mismatch_gracefully()
    {
        // Arrange
        var rule = Rule<Order>.For("Type Check")
            .When(o => o.Amount > 100)
            .Then(o => o.Status = "Processed");

        var ruleSet = RuleSet<Order>.For("OrderRules").Add(rule);
        var engine = new RuleEngine();

        var order = new Order { Amount = 50 };

        // Act
        var result = engine.Evaluate(order, ruleSet);

        // Assert
        order.Status.ShouldBeNull(); // Rule didn't match
        result.AppliedRules.ShouldBeEmpty();
    }

    #endregion

    #region Reason & Explanation

    [Fact]
    public void Should_capture_rule_reason()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("Value Check")
            .When(x => x.Value > 5)
            .Then(x => x.Value = 20)
            .Because("Value exceeded threshold");

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(20);
        result.AppliedRules.ShouldContain("Value Check");
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Should_be_deterministic_across_multiple_evaluations()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Deterministic Rule")
            .When(x => x.Value > 5)
            .Then(x => { /* no side effects */ });

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act - Run evaluation multiple times
        var result1 = engine.Evaluate(obj, ruleSet);
        var result2 = engine.Evaluate(obj, ruleSet);
        var result3 = engine.Evaluate(obj, ruleSet);

        // Assert - All results should be identical
        result1.AppliedRules.SequenceEqual(result2.AppliedRules).ShouldBeTrue();
        result2.AppliedRules.SequenceEqual(result3.AppliedRules).ShouldBeTrue();
    }

    #endregion

    #region Special Values

    [Fact]
    public void Should_handle_zero_values()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule = Rule<TestObject>.For("Zero Check")
            .When(x => x.Value == 0)
            .Then(x => x.Value = 1);

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(1);
        result.AppliedRules.ShouldContain("Zero Check");
    }

    [Fact]
    public void Should_handle_negative_values()
    {
        // Arrange
        var obj = new TestObject { Value = -100 };

        var rule = Rule<TestObject>.For("Negative Check")
            .When(x => x.Value < 0)
            .Then(x => x.Value = 0);

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(0);
        result.AppliedRules.ShouldContain("Negative Check");
    }

    [Fact]
    public void Should_handle_empty_string()
    {
        // Arrange
        var obj = new TestObject { Name = "" };

        var rule = Rule<TestObject>.For("Empty String Check")
            .When(x => string.IsNullOrEmpty(x.Name))
            .Then(x => x.Name = "Default");

        var ruleSet = RuleSet<TestObject>.For("TestSet").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Name.ShouldBe("Default");
        result.AppliedRules.ShouldContain("Empty String Check");
    }

    #endregion
}
