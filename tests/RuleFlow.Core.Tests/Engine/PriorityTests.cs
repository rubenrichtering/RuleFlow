using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class PriorityTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public List<string> ExecutionOrder { get; } = new();
    }

    [Fact]
    public void Should_execute_higher_priority_rules_first()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("Low Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Low"))
            .WithPriority(1);

        var rule2 = Rule<TestObject>.For("High Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("High"))
            .WithPriority(10);

        var rule3 = Rule<TestObject>.For("Medium Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Medium"))
            .WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Priority Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "High", "Medium", "Low" });
    }

    [Fact]
    public void Should_preserve_insertion_order_for_equal_priority()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("First")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("First"))
            .WithPriority(5);

        var rule2 = Rule<TestObject>.For("Second")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Second"))
            .WithPriority(5);

        var rule3 = Rule<TestObject>.For("Third")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Third"))
            .WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Equal Priority Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "First", "Second", "Third" });
    }

    [Fact]
    public void Should_use_default_priority_zero()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("No Priority Set")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Default"));

        rule1.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_handle_negative_priorities()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("Negative Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Negative"))
            .WithPriority(-10);

        var rule2 = Rule<TestObject>.For("Positive Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Positive"))
            .WithPriority(10);

        var ruleSet = RuleSet<TestObject>.For("Mixed Priority Set")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        // Positive priority (10) should execute before negative priority (-10)
        obj.ExecutionOrder.ShouldBe(new[] { "Positive", "Negative" });
    }

    [Fact]
    public void Should_respect_priority_regardless_of_addition_order()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        // Add in different order than priority
        var rule1 = Rule<TestObject>.For("Priority 1")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("1"))
            .WithPriority(1);

        var rule2 = Rule<TestObject>.For("Priority 100")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("100"))
            .WithPriority(100);

        var rule3 = Rule<TestObject>.For("Priority 50")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("50"))
            .WithPriority(50);

        var ruleSet = RuleSet<TestObject>.For("Priority Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "100", "50", "1" });
    }
}
