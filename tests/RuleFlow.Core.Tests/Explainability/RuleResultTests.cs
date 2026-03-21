using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Explainability;

public class RuleResultTests
{
    private class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Should_contain_all_executed_rules()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => x.Value < 100).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(2);
        result.Executions[0].RuleName.ShouldBe("Rule 1");
        result.Executions[1].RuleName.ShouldBe("Rule 2");
    }

    [Fact]
    public void Should_include_matched_status_in_executions()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Matched").When(x => x.Value == 50).Then(x => { });
        var rule2 = Rule<TestObject>.For("Not Matched").When(x => x.Value == 999).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions[0].Matched.ShouldBeTrue();
        result.Executions[1].Matched.ShouldBeFalse();
    }

    [Fact]
    public void Should_include_reason_in_executions()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { })
            .Because("This is the reason");

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions[0].Reason.ShouldBe("This is the reason");
    }

    [Fact]
    public void Should_include_priority_in_executions()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { })
            .WithPriority(42);

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions[0].Priority.ShouldBe(42);
    }

    [Fact]
    public void Should_only_include_matched_rules_in_appliedrules()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Matched 1").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Not Matched").When(x => x.Value == 999).Then(x => { });
        var rule3 = Rule<TestObject>.For("Matched 2").When(x => x.Value < 100).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule1).Add(rule2).Add(rule3);
        
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.Count().ShouldBe(2);
        result.AppliedRules.ShouldContain("Matched 1");
        result.AppliedRules.ShouldContain("Matched 2");
        result.AppliedRules.ShouldNotContain("Not Matched");
    }

    [Fact]
    public void Should_return_empty_appliedrules_when_no_rules_match()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Never Matches").When(x => x.Value == 999).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldBeEmpty();
    }

    [Fact]
    public void Should_convert_to_string_with_symbols()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Matched").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Not Matched").When(x => x.Value == 999).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var str = result.ToString();

        // Assert
        str.ShouldContain("✔");  // Matched symbol
        str.ShouldContain("✖");  // Not matched symbol
        str.ShouldContain("Matched");
        str.ShouldContain("Not Matched");
    }

    [Fact]
    public void Should_include_reason_in_tostring_when_present()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { })
            .Because("Custom reason");

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var str = result.ToString();

        // Assert
        str.ShouldContain("Custom reason");
    }

    [Fact]
    public void Should_not_include_reason_in_tostring_when_not_present()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { }); // No reason

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var str = result.ToString();

        // Assert
        str.ShouldContain("Test Rule");
        str.ShouldNotContain("(");
    }
}

public class ExplainabilitTests
{
    private class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Should_explain_returns_non_empty_string()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var explanation = result.Explain();

        // Assert
        explanation.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Should_explain_include_rule_names()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("My Important Rule")
            .When(x => true)
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("My Important Rule");
    }

    [Fact]
    public void Should_explain_include_reasons_when_present()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Test Rule")
            .When(x => true)
            .Then(x => { })
            .Because("Because this is why");

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("Because this is why");
    }

    [Fact]
    public void Should_explain_handle_multiple_rules()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => true).Then(x => { }).Because("Reason 1");
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => true).Then(x => { }).Because("Reason 2");

        var ruleSet = RuleSet<TestObject>.For("Test").Add(rule1).Add(rule2);
        var engine = new RuleEngine();

        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var explanation = result.Explain();

        // Assert
        explanation.ShouldContain("Rule 1");
        explanation.ShouldContain("Rule 2");
        explanation.ShouldContain("Reason 1");
        explanation.ShouldContain("Reason 2");
    }

    [Fact]
    public void Should_explain_work_for_empty_result()
    {
        // Arrange
        var ruleSet = RuleSet<TestObject>.For("Empty");
        var engine = new RuleEngine();
        var obj = new TestObject { Value = 0 };

        var result = engine.Evaluate(obj, ruleSet);

        // Act & Assert
        Should.NotThrow(() => result.Explain());
    }
}
