using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Rules;

public class RuleCreationTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public void Should_create_rule_with_name()
    {
        // Act
        var rule = Rule<TestObject>.For("Test Rule");

        // Assert
        rule.Name.ShouldBe("Test Rule");
    }

    [Fact]
    public void Should_set_condition_with_when()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");

        // Act
        var condition = rule.When(x => x.Value > 10);

        // Assert
        condition.ShouldBeSameAs(rule); // Fluent API returns self
    }

    [Fact]
    public void Should_set_action_with_then()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");

        // Act
        var fluent = rule.Then(x => x.Value = 20);

        // Assert
        fluent.ShouldBeSameAs(rule); // Fluent API returns self
    }

    [Fact]
    public void Should_set_reason_with_because()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");
        var reason = "This is why the rule exists";

        // Act
        var fluent = rule.Because(reason);

        // Assert
        fluent.ShouldBeSameAs(rule);
        rule.Reason.ShouldBe(reason);
    }

    [Fact]
    public void Should_set_priority_with_withpriority()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");

        // Act
        var fluent = rule.WithPriority(42);

        // Assert
        fluent.ShouldBeSameAs(rule);
        rule.Priority.ShouldBe(42);
    }

    [Fact]
    public void Should_build_complete_rule_with_fluent_api()
    {
        // Act
        var rule = Rule<TestObject>.For("Complete Rule")
            .When(x => x.Value > 0)
            .Then(x => x.Name = "Updated")
            .Because("Testing fluent API")
            .WithPriority(5);

        // Assert
        rule.Name.ShouldBe("Complete Rule");
        rule.Reason.ShouldBe("Testing fluent API");
        rule.Priority.ShouldBe(5);
    }

    [Fact]
    public void Should_throw_on_null_condition()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => rule.When((Func<TestObject, bool>)null!));
    }

    [Fact]
    public void Should_throw_on_null_action()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Test Rule");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => rule.Then((Action<TestObject>)null!));
    }

    [Fact]
    public void Should_initialize_with_default_priority_zero()
    {
        // Act
        var rule = Rule<TestObject>.For("Default Priority");

        // Assert
        rule.Priority.ShouldBe(0);
    }

    [Fact]
    public void Should_initialize_without_reason()
    {
        // Act
        var rule = Rule<TestObject>.For("No Reason");

        // Assert
        rule.Reason.ShouldBeNull();
    }
}

public class RuleEvaluationTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public void Should_evaluate_condition_and_return_true()
    {
        // Arrange
        var obj = new TestObject { Value = 100 };
        var rule = Rule<TestObject>.For("Test")
            .When(x => x.Value > 50);

        // Act
        var result = rule.Evaluate(obj, null!);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Should_evaluate_condition_and_return_false()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Test")
            .When(x => x.Value > 50);

        // Act
        var result = rule.Evaluate(obj, null!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Should_execute_action_on_input()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };
        var rule = Rule<TestObject>.For("Test")
            .Then(x => x.Value = 42);

        // Act
        rule.Execute(obj, null!);

        // Assert
        obj.Value.ShouldBe(42);
    }

    [Fact]
    public void Should_handle_rule_without_explicit_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("No Action");

        // Act & Assert - should not throw
        Should.NotThrow(() => rule.Execute(obj, null!));
    }

    [Fact]
    public void Should_handle_complex_condition()
    {
        // Arrange
        var obj = new TestObject { Value = 25, Name = "Test" };
        var rule = Rule<TestObject>.For("Complex")
            .When(x => x.Value >= 20 && x.Value <= 30 && x.Name == "Test");

        // Act
        var result = rule.Evaluate(obj, null!);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Should_handle_complex_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Complex Action")
            .Then(x =>
            {
                x.Value += 5;
                x.Name = $"Value is {x.Value}";
            });

        // Act
        rule.Execute(obj, null!);

        // Assert
        obj.Value.ShouldBe(15);
        obj.Name.ShouldBe("Value is 15");
    }
}
