using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class RuleEngineExecutionTests
{
    private class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public bool Flag { get; set; }
    }

    [Fact]
    public void Should_execute_rule_when_condition_is_true()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => x.Value > 5)
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Test Rule");
    }

    [Fact]
    public void Should_not_execute_rule_when_condition_is_false()
    {
        // Arrange
        var obj = new TestObject { Value = 3 };
        var executed = false;

        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => x.Value > 5)
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        executed.ShouldBeFalse();
        result.AppliedRules.ShouldNotContain("Test Rule");
    }

    [Fact]
    public void Should_modify_input_when_rule_executes()
    {
        // Arrange
        var obj = new TestObject { Value = 100 };

        var rule = Rule<TestObject>.For("Increment Value")
            .When(x => x.Value > 50)
            .Then(x => { x.Value += 10; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(110);
    }

    [Fact]
    public void Should_evaluate_all_rules_in_ruleset()
    {
        // Arrange
        var obj = new TestObject { Value = 15 };

        var rule1 = Rule<TestObject>.For("Rule 1")
            .When(x => x.Value > 10)
            .Then(x => x.Flag = true);

        var rule2 = Rule<TestObject>.For("Rule 2")
            .When(x => x.Value < 20)
            .Then(x => { /* no-op */ });

        var rule3 = Rule<TestObject>.For("Rule 3")
            .When(x => x.Value == 999)
            .Then(x => { /* no-op */ });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(3);
        result.AppliedRules.Count().ShouldBe(2);
        result.AppliedRules.ShouldContain("Rule 1");
        result.AppliedRules.ShouldContain("Rule 2");
        result.AppliedRules.ShouldNotContain("Rule 3");
    }

    [Fact]
    public void Should_handle_empty_ruleset()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var ruleSet = RuleSet<TestObject>.For("Empty Set");
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.ShouldBeEmpty();
        result.AppliedRules.ShouldBeEmpty();
    }

    [Fact]
    public void Should_return_ruleresult_with_applied_rules_only()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };

        var rule1 = Rule<TestObject>.For("Matching Rule")
            .When(x => x.Value == 50)
            .Then(x => { /* no-op */ });

        var rule2 = Rule<TestObject>.For("Non-matching Rule")
            .When(x => x.Value == 999)
            .Then(x => { /* no-op */ });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(2);
        result.AppliedRules.Count().ShouldBe(1);
        result.AppliedRules.First().ShouldBe("Matching Rule");
    }

    [Fact]
    public void Should_execute_multiple_matching_rules_sequentially()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("Add 10")
            .When(x => true)
            .Then(x => x.Value += 10);

        var rule2 = Rule<TestObject>.For("Add 5")
            .When(x => true)
            .Then(x => x.Value += 5);

        var rule3 = Rule<TestObject>.For("Double")
            .When(x => true)
            .Then(x => x.Value *= 2);

        var ruleSet = RuleSet<TestObject>.For("Sequential Rules")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        // Execution order (without priority): rule1 (10), rule2 (15), rule3 (30)
        obj.Value.ShouldBe(30);
    }
}
