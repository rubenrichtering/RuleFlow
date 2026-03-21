using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class EdgeCaseAndErrorTests
{
    private class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Should_throw_on_null_input()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => engine.Evaluate(null!, ruleSet));
    }

    [Fact]
    public void Should_throw_on_null_ruleset()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };
        var engine = new RuleEngine();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => engine.Evaluate(obj, null!));
    }

    [Fact]
    public void Should_use_default_context_when_none_provided()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Test")
            .When(x => x.Value > 0)
            .Then(x => x.Value = 20);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert - should not throw when context is null
        Should.NotThrow(() => engine.Evaluate(obj, ruleSet, null));

        // And rule should still execute
        obj.Value.ShouldBe(20);
    }

    [Fact]
    public void Should_handle_rule_without_explicit_condition()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };
        var executed = false;

        // Create rule without explicit When clause
        var rule = Rule<TestObject>.For("Default Condition")
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert - default condition is true, so should execute
        executed.ShouldBeTrue();
    }

    [Fact]
    public void Should_handle_rule_without_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("No Action")
            .When(x => x.Value > 0);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert - should not throw
        var result = Should.NotThrow(() => engine.Evaluate(obj, ruleSet));

        // And rule should match
        result.AppliedRules.ShouldContain("No Action");
    }

    [Fact]
    public void Should_not_modify_input_if_no_action_matches()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Never Matches")
            .When(x => x.Value > 100)
            .Then(x => x.Value = 999);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(10); // Unchanged
    }

    [Fact]
    public void Should_handle_condition_that_throws()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };
        var rule = Rule<TestObject>.For("Throws")
            .When(x => throw new InvalidOperationException("Intentional"));

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => engine.Evaluate(obj, ruleSet));
    }

    [Fact]
    public void Should_handle_action_that_throws()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Throws")
            .When(x => true)
            .Then(x => throw new InvalidOperationException("Intentional"));

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => engine.Evaluate(obj, ruleSet));
    }

    [Fact]
    public void Should_execute_later_rules_even_if_earlier_match()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rule1 = Rule<TestObject>.For("Rule 1")
            .When(x => true)
            .Then(x => x.Value += 10);

        var rule2 = Rule<TestObject>.For("Rule 2")
            .When(x => true)
            .Then(x => x.Value += 5);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2);
        var engine = new RuleEngine();

        // Act
        engine.Evaluate(obj, ruleSet);

        // Assert
        obj.Value.ShouldBe(15); // Both rules executed
    }

    [Fact]
    public void Should_handle_large_ruleset()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };
        var ruleSet = RuleSet<TestObject>.For("Large");

        // Add many rules
        for (int i = 0; i < 100; i++)
        {
            var idx = i;
            ruleSet.Add(Rule<TestObject>.For($"Rule {i}")
                .When(x => true)
                .Then(x => x.Value += 1));
        }

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(100);
        result.AppliedRules.Count().ShouldBe(100);
        obj.Value.ShouldBe(100); // All 100 rules executed
    }

    [Fact]
    public void Should_handle_complex_object_types()
    {
        // Arrange
        var obj = new ComplexObject { Items = new List<string> { "a", "b", "c" } };

        var rule = Rule<ComplexObject>.For("List Rule")
            .When(x => x.Items.Count > 2)
            .Then(x => x.Items.Add("d"));

        var ruleSet = RuleSet<ComplexObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldContain("List Rule");
        obj.Items.Count.ShouldBe(4);
    }

    [Fact]
    public void Should_maintain_deterministic_behavior()
    {
        // Arrange
        var rule1 = Rule<TestObject>.For("A").When(x => true).Then(x => x.Value += 1).WithPriority(5);
        var rule2 = Rule<TestObject>.For("B").When(x => true).Then(x => x.Value += 2).WithPriority(3);
        var rule3 = Rule<TestObject>.For("C").When(x => true).Then(x => x.Value += 4).WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2).Add(rule3);
        var engine = new RuleEngine();

        // Act - run multiple times
        var results = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var obj = new TestObject { Value = 0 };
            engine.Evaluate(obj, ruleSet);
            results.Add(obj.Value);
        }

        // Assert - all results should be the same
        results.ShouldAllBe(x => x == 7); // Priority: 5,5,3 = A(1) + C(4) + B(2) = 7
    }

    private class ComplexObject
    {
        public List<string> Items { get; set; } = new();
    }
}
