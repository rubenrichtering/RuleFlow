using RuleFlow.Abstractions;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Rules;

public class RuleSetTests
{
    private class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Should_create_ruleset_with_name()
    {
        // Act
        var ruleSet = RuleSet<TestObject>.For("My Rules");

        // Assert
        ruleSet.Name.ShouldBe("My Rules");
    }

    [Fact]
    public void Should_add_rule_to_ruleset()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { });

        // Act
        var fluent = ruleSet.Add(rule);

        // Assert
        fluent.ShouldBeSameAs(ruleSet); // Fluent API returns self
        ruleSet.Rules.Count.ShouldBe(1);
        ruleSet.Rules.First().ShouldBe(rule);
    }

    [Fact]
    public void Should_add_multiple_rules()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => true).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => true).Then(x => { });
        var rule3 = Rule<TestObject>.For("Rule 3").When(x => true).Then(x => { });

        // Act
        ruleSet.Add(rule1).Add(rule2).Add(rule3);

        // Assert
        ruleSet.Rules.Count.ShouldBe(3);
        ruleSet.Rules.ShouldContain(rule1);
        ruleSet.Rules.ShouldContain(rule2);
        ruleSet.Rules.ShouldContain(rule3);
    }

    [Fact]
    public void Should_return_rules_as_readonly_list()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");
        var rule = Rule<TestObject>.For("Test Rule").When(x => true).Then(x => { });
        ruleSet.Add(rule);

        // Act
        var rules = ruleSet.Rules;

        // Assert
        rules.ShouldNotBeNull();
        rules.ShouldBeAssignableTo<IReadOnlyList<IRule<TestObject>>>();
        rules.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_throw_on_null_rule()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ruleSet.Add(null!));
    }

    [Fact]
    public void Should_allow_duplicate_rules()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");
        var rule = Rule<TestObject>.For("Test Rule").When(x => true).Then(x => { });

        // Act
        ruleSet.Add(rule).Add(rule);

        // Assert
        ruleSet.Rules.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_preserve_insertion_order()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Test Set");
        var rule1 = Rule<TestObject>.For("First").When(x => true).Then(x => { });
        var rule2 = Rule<TestObject>.For("Second").When(x => true).Then(x => { });
        var rule3 = Rule<TestObject>.For("Third").When(x => true).Then(x => { });

        // Act
        ruleSet.Add(rule1).Add(rule2).Add(rule3);

        // Assert
        ruleSet.Rules[0].Name.ShouldBe("First");
        ruleSet.Rules[1].Name.ShouldBe("Second");
        ruleSet.Rules[2].Name.ShouldBe("Third");
    }

    [Fact]
    public void Should_initialize_with_empty_rules()
    {
        // Act
        var ruleSet = RuleSet<TestObject>.For("Empty Set");

        // Assert
        ruleSet.Rules.ShouldBeEmpty();
        ruleSet.Rules.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_allow_building_complex_ruleset()
    {
        // Act
        var ruleSet = RuleSet<TestObject>.For("Complex Rules")
            .Add(Rule<TestObject>.For("Rule 1")
                .When(x => x.Value > 0)
                .Then(x => x.Value += 1)
                .Because("Increment")
                .WithPriority(10))
            .Add(Rule<TestObject>.For("Rule 2")
                .When(x => x.Value < 100)
                .Then(x => x.Value *= 2)
                .Because("Double")
                .WithPriority(5));

        // Assert
        ruleSet.Rules.Count.ShouldBe(2);
        ruleSet.Rules[0].Name.ShouldBe("Rule 1");
        ruleSet.Rules[0].Priority.ShouldBe(10);
        ruleSet.Rules[1].Name.ShouldBe("Rule 2");
        ruleSet.Rules[1].Priority.ShouldBe(5);
    }
}
