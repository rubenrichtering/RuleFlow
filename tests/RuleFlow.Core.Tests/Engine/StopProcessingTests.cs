using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class StopProcessingTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public List<string> ExecutionOrder { get; } = new();
    }

    [Fact]
    public void Should_stop_after_matching_rule_with_stop_processing()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Stop Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("StopRule"))
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("Second Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("SecondRule"));

        var rule3 = Rule<TestObject>.For("Third Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("ThirdRule"));

        var ruleSet = RuleSet<TestObject>.For("Stop Processing Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "StopRule" });
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public void Should_not_stop_when_rule_does_not_match()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Stop Rule (No Match)")
            .When(x => x.Value > 100) // Won't match
            .Then(x => x.ExecutionOrder.Add("StopRule"))
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("Second Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("SecondRule"));

        var rule3 = Rule<TestObject>.For("Third Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("ThirdRule"));

        var ruleSet = RuleSet<TestObject>.For("Conditional Stop Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.Count.ShouldBe(2);
        obj.ExecutionOrder.ShouldBe(new[] { "SecondRule", "ThirdRule" });
        result.Executions.Count.ShouldBe(3);
        result.Executions[0].StoppedProcessing.ShouldBeFalse();
    }

    [Fact]
    public void Should_continue_when_no_stop_processing_flag()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Normal Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Rule1"));
            // No StopIfMatched()

        var rule2 = Rule<TestObject>.For("Second Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Rule2"));

        var rule3 = Rule<TestObject>.For("Third Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Rule3"));

        var ruleSet = RuleSet<TestObject>.For("Normal Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.Count.ShouldBe(3);
        obj.ExecutionOrder.ShouldBe(new[] { "Rule1", "Rule2", "Rule3" });
        result.Executions.All(e => !e.StoppedProcessing).ShouldBeTrue();
    }

    [Fact]
    public void Should_work_with_priority_ordering()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Low Priority Stop")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("LowPriorityStop"))
            .WithPriority(1)
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("High Priority Normal")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("HighPriorityNormal"))
            .WithPriority(10);

        var rule3 = Rule<TestObject>.For("Medium Priority")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("MediumPriority"))
            .WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Priority and Stop Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        //High priority executes first, then medium, then low (which stops)
        obj.ExecutionOrder.ShouldBe(new[] { "HighPriorityNormal", "MediumPriority", "LowPriorityStop" });
        result.Executions.Count.ShouldBe(3);
        result.Executions[2].StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public void Should_stop_immediately_when_high_priority_rule_stops()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Low Priority (would run later)")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("LowPriority"));

        var rule2 = Rule<TestObject>.For("High Priority Stop")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("HighPriorityStop"))
            .WithPriority(10)
            .StopIfMatched();

        var ruleSet = RuleSet<TestObject>.For("High Priority Stop Set")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.Count.ShouldBe(1);
        obj.ExecutionOrder.ShouldBe(new[] { "HighPriorityStop" });
        result.Executions.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_track_stopped_processing_in_execution_result()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Stopping Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Stopped"))
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("Never Runs")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("NeverRuns"));

        var ruleSet = RuleSet<TestObject>.For("Stop Result Set")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].RuleName.ShouldBe("Stopping Rule");
        result.Executions[0].Matched.ShouldBeTrue();
        result.Executions[0].StoppedProcessing.ShouldBeTrue();
        result.AppliedRules.Count().ShouldBe(1);
        result.AppliedRules.ShouldContain("Stopping Rule");
    }

    [Fact]
    public void Should_not_mark_stopped_processing_if_no_match()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Non-matching Stop Rule")
            .When(x => x.Value > 100)
            .Then(x => x.ExecutionOrder.Add("NeverExecutes"))
            .WithPriority(10) // Higher priority so it runs first
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("Second Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Executes"))
            .WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Non-matching Stop Set")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        result.Executions.Count.ShouldBe(2);
        result.Executions[0].Matched.ShouldBeFalse();
        result.Executions[0].StoppedProcessing.ShouldBeFalse();
        result.Executions[1].Matched.ShouldBeTrue();
        result.Executions[1].StoppedProcessing.ShouldBeFalse();
    }

    [Fact]
    public void Should_allow_fluent_api_chaining_with_stop_if_matched()
    {
        // Arrange
        var obj = new TestObject { Value = 50 };

        var rule = Rule<TestObject>.For("Fluent Chain Rule")
            .When(x => x.Value > 40)
            .Then(x => x.ExecutionOrder.Add("Executed"))
            .Because("Value is greater than 40")
            .WithPriority(5)
            .StopIfMatched();

        rule.StopProcessing.ShouldBeTrue();
        rule.Priority.ShouldBe(5);
        rule.Reason.ShouldBe("Value is greater than 40");

        var ruleSet = RuleSet<TestObject>.For("Chain Test")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Executed" });
        result.Executions[0].StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public void Should_handle_multiple_stop_rules_respecting_priority()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule1 = Rule<TestObject>.For("Low Stop Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Low"))
            .WithPriority(1)
            .StopIfMatched();

        var rule2 = Rule<TestObject>.For("High Stop Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("High"))
            .WithPriority(10)
            .StopIfMatched();

        var rule3 = Rule<TestObject>.For("Medium Rule")
            .When(x => true)
            .Then(x => x.ExecutionOrder.Add("Medium"))
            .WithPriority(5);

        var ruleSet = RuleSet<TestObject>.For("Multiple Stops")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        // High priority stop should execute first and stop
        obj.ExecutionOrder.ShouldBe(new[] { "High" });
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public void StopProcessing_should_default_to_false()
    {
        // Arrange
        var rule = Rule<TestObject>.For("Normal Rule")
            .When(x => true)
            .Then(x => { });

        // Assert
        rule.StopProcessing.ShouldBeFalse();
    }
}
