using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Formatting;

/// <summary>
/// Comprehensive test suite for RuleExecutionDebugFormatter.
/// Tests debug string output for various execution scenarios.
/// </summary>
public class RuleExecutionDebugFormatterTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public bool Flag { get; set; }
        public List<string> Items { get; } = new();
    }

    [Fact]
    public void ToDebugString_With_Null_Result_Returns_Empty_String()
    {
        // Arrange
        RuleResult? result = null;

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToDebugString_With_Single_Matched_Rule_Shows_Check_Mark()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Check Value")
            .When(x => x.Value > 0)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("SimpleTest")
            .Add(rule);

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("RuleSet: SimpleTest");
        output.ShouldContain("✅ Check Value");
    }

    [Fact]
    public void ToDebugString_With_Single_Not_Matched_Rule_Shows_X_Mark()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Check High Value")
            .When(x => x.Value > 100)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("SimpleTest")
            .Add(rule);

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("RuleSet: SimpleTest");
        output.ShouldContain("❌ Check High Value");
    }

    [Fact]
    public void ToDebugString_With_Multiple_Rules_Shows_All_In_Order()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => x.Value < 100).Then(x => { });
        var rule3 = Rule<TestObject>.For("Rule 3").When(x => x.Value == 999).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("MultiTest")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var output = result.ToDebugString();

        // Assert
        var lines = output.Split(Environment.NewLine);
        var rule1Line = lines.FirstOrDefault(l => l.Contains("Rule 1"));
        var rule2Line = lines.FirstOrDefault(l => l.Contains("Rule 2"));
        var rule3Line = lines.FirstOrDefault(l => l.Contains("Rule 3"));

        rule1Line.ShouldNotBeNull();
        rule2Line.ShouldNotBeNull();
        rule3Line.ShouldNotBeNull();

        output.ShouldContain("✅ Rule 1");
        output.ShouldContain("✅ Rule 2");
        output.ShouldContain("❌ Rule 3");
    }

    [Fact]
    public void ToDebugString_Shows_Rule_Reason()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Premium Check")
            .When(x => x.Value > 40)
            .Then(x => x.Flag = true)
            .Because("Value exceeds premium threshold");

        var ruleSet = RuleSet<TestObject>.For("ReasonTest")
            .Add(rule);

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("Premium Check");
        output.ShouldContain("Reason: Value exceeds premium threshold");
    }

    [Fact]
    public void ToDebugString_With_Observability_Shows_Metrics()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => x.Value < 100).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("MetricsTest")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleFlow.Abstractions.Execution.RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };
        var result = engine.Evaluate(obj, ruleSet, options);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("Execution Summary:");
        output.ShouldContain("Rules evaluated: 2");
        output.ShouldContain("Rules matched: 2");
    }

    [Fact]
    public void ToDebugString_Without_Observability_Has_No_Metrics_Section()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Rule 1").When(x => x.Value > 0).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("NoMetricsTest")
            .Add(rule);

        var engine = new RuleEngine();
        var options = new RuleFlow.Abstractions.Execution.RuleExecutionOptions<TestObject>
        {
            EnableObservability = false
        };
        var result = engine.Evaluate(obj, ruleSet, options);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldNotContain("Execution Summary:");
        output.ShouldNotContain("Rules evaluated:");
    }

    [Fact]
    public void ToDebugString_With_Actions_Shows_Action_Arrow()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Apply Changes")
            .When(x => x.Value > 40)
            .Then(x => x.Items.Add("changed"))
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("ActionsTest")
            .Add(rule);

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, ruleSet);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("→");
    }

    [Fact]
    public void ToDebugString_With_Nested_Groups_Shows_Hierarchy()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };

        var outerRule = Rule<TestObject>.For("Outer Rule")
            .When(x => true)
            .Then(x => { });

        var outerGroup = RuleSet<TestObject>.For("Nested Groups Test")
            .Add(outerRule)
            .AddGroup("Inner Group", g => g
                .Add(Rule<TestObject>.For("Inner Rule")
                    .When(x => x.Value > 0)
                    .Then(x => { })));

        var engine = new RuleEngine();
        var result = engine.Evaluate(obj, outerGroup);

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("Inner Group");
        output.ShouldContain("Inner Rule");
        output.ShouldContain("Outer Rule");
    }

    [Fact]
    public void ToDebugString_Is_Deterministic()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule A").When(x => x.Value > 0).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule B").When(x => x.Value < 100).Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);
        var output1 = result.ToDebugString();
        var output2 = result.ToDebugString();

        // Assert
        output1.ShouldBe(output2);
    }

    [Fact]
    public void ToDebugString_With_Unknown_RuleSet_Uses_Unnamed_Fallback()
    {
        // Arrange
        var result = new RuleResult();

        // Act
        var output = result.ToDebugString();

        // Assert
        output.ShouldContain("RuleSet: (unnamed)");
    }

    [Fact]
    public void ToDebugString_Never_Throws_Exception()
    {
        // Arrange
        RuleResult? result = null;

        // Act
        var action = () => result.ToDebugString();

        // Assert
        action.ShouldNotThrow();
    }
}
