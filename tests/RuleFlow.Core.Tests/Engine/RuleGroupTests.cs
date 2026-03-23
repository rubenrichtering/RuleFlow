using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class RuleGroupTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public List<string> ExecutionOrder { get; } = new();
    }

    [Fact]
    public void Should_execute_rules_in_group()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Approval", group => group
                .Add(Rule<TestObject>.For("High amount")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("High amount"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "High amount" });
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].GroupName.ShouldBe("Approval");
    }

    [Fact]
    public void Should_execute_root_rules_before_groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Base rule")
                .When(x => true)
                .Then(x => x.ExecutionOrder.Add("Base")))
            .AddGroup("Approval", group => group
                .Add(Rule<TestObject>.For("High amount")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("High amount"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Base", "High amount" });
    }

    [Fact]
    public void Should_execute_multiple_groups_in_insertion_order()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("First Group", group => group
                .Add(Rule<TestObject>.For("First rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("First"))))
            .AddGroup("Second Group", group => group
                .Add(Rule<TestObject>.For("Second rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Second"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "First", "Second" });
        result.Executions[0].GroupName.ShouldBe("First Group");
        result.Executions[1].GroupName.ShouldBe("Second Group");
    }

    [Fact]
    public void Should_stop_processing_across_groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("First Group", group => group
                .Add(Rule<TestObject>.For("Stop Rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Stop"))
                    .StopIfMatched()))
            .AddGroup("Second Group", group => group
                .Add(Rule<TestObject>.For("Should not execute")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Should not execute"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Stop" });
        result.Executions.Count.ShouldBe(1);
        result.Executions[0].StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public void Should_stop_processing_inside_group()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Approval", group => group
                .Add(Rule<TestObject>.For("Stop Rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Stop"))
                    .StopIfMatched())
                .Add(Rule<TestObject>.For("Should not execute")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Should not execute"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Stop" });
        result.Executions.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_handle_empty_group()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Base rule")
                .When(x => true)
                .Then(x => x.ExecutionOrder.Add("Base")))
            .AddGroup("Empty Group", group => group) // Empty group
            .Add(Rule<TestObject>.For("Another rule")
                .When(x => true)
                .Then(x => x.ExecutionOrder.Add("Another")));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Base", "Another" });
    }

    [Fact]
    public void Should_respect_priority_within_group()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Approval", group => group
                .Add(Rule<TestObject>.For("Low Priority")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Low"))
                    .WithPriority(1))
                .Add(Rule<TestObject>.For("High Priority")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("High"))
                    .WithPriority(10)));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "High", "Low" });
    }

    [Fact]
    public void Should_not_merge_priority_across_groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Root Low Priority")
                .When(x => true)
                .Then(x => x.ExecutionOrder.Add("RootLow"))
                .WithPriority(1))
            .AddGroup("ApprovalGroup", group => group
                .Add(Rule<TestObject>.For("Group High Priority")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("GroupHigh"))
                    .WithPriority(100))); // Higher priority than root rules, but group executes after root rules

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        // Root rules execute first (in priority order), then groups
        obj.ExecutionOrder.ShouldBe(new[] { "RootLow", "GroupHigh" });
    }

    [Fact]
    public void Should_track_group_name_in_execution()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Root rule")
                .When(x => true)
                .Then(x => { }))
            .AddGroup("TestGroup", group => group
                .Add(Rule<TestObject>.For("Group rule")
                    .When(x => true)
                    .Then(x => { })));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        result.Executions.Count.ShouldBe(2);
        result.Executions[0].RuleName.ShouldBe("Root rule");
        result.Executions[0].GroupName.ShouldBeNull();
        result.Executions[1].RuleName.ShouldBe("Group rule");
        result.Executions[1].GroupName.ShouldBe("TestGroup");
    }

    [Fact]
    public void Should_handle_non_matching_rules_in_group()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Approval", group => group
                .Add(Rule<TestObject>.For("Non-matching")
                    .When(x => x.Value > 100) // Won't match
                    .Then(x => x.ExecutionOrder.Add("Should not execute")))
                .Add(Rule<TestObject>.For("Matching")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Matching"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "Matching" });
        result.Executions.Count.ShouldBe(2); // Both evaluated, but only one matched
        result.Executions[0].Matched.ShouldBeFalse();
        result.Executions[1].Matched.ShouldBeTrue();
    }

    [Fact]
    public void Should_support_deeply_nested_groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Level1", group => group
                .Add(Rule<TestObject>.For("Level 1 rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("L1")))
                .AddGroup("Level2", subgroup => subgroup
                    .Add(Rule<TestObject>.For("Level 2 rule")
                        .When(x => true)
                        .Then(x => x.ExecutionOrder.Add("L2")))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "L1", "L2" });
        result.Executions[0].GroupName.ShouldBe("Level1");
        result.Executions[1].GroupName.ShouldBe("Level1/Level2");
    }

    [Fact]
    public void Should_filter_nested_groups_by_full_path_when_names_are_duplicated()
    {
        // Arrange
        var obj = new TestObject();

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("ParentA", group => group
                .AddGroup("Shared", nested => nested
                    .Add(Rule<TestObject>.For("RuleA")
                        .When(_ => true)
                        .Then(x => x.ExecutionOrder.Add("A")))))
            .AddGroup("ParentB", group => group
                .AddGroup("Shared", nested => nested
                    .Add(Rule<TestObject>.For("RuleB")
                        .When(_ => true)
                        .Then(x => x.ExecutionOrder.Add("B")))));

        var options = new RuleExecutionOptions<TestObject>
        {
            IncludeGroups = new[] { "ParentA/Shared" }
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules, options);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "A" });
        result.AppliedRules.ShouldContain("RuleA");
        result.AppliedRules.ShouldNotContain("RuleB");
    }

    [Fact]
    public void Should_support_legacy_leaf_name_group_filtering()
    {
        // Arrange
        var obj = new TestObject();

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("ParentA", group => group
                .AddGroup("Shared", nested => nested
                    .Add(Rule<TestObject>.For("RuleA")
                        .When(_ => true)
                        .Then(x => x.ExecutionOrder.Add("A")))))
            .AddGroup("ParentB", group => group
                .AddGroup("Shared", nested => nested
                    .Add(Rule<TestObject>.For("RuleB")
                        .When(_ => true)
                        .Then(x => x.ExecutionOrder.Add("B")))));

        var options = new RuleExecutionOptions<TestObject>
        {
            IncludeGroups = new[] { "Shared" }
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules, options);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "A", "B" });
        result.AppliedRules.ShouldContain("RuleA");
        result.AppliedRules.ShouldContain("RuleB");
    }

    [Fact]
    public void Should_stop_at_all_levels_when_nested()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var rules = RuleSet<TestObject>.For("Main")
            .AddGroup("Level1", group => group
                .Add(Rule<TestObject>.For("Level 1 rule")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("L1")))
                .AddGroup("Level2", subgroup => subgroup
                    .Add(Rule<TestObject>.For("Stop rule")
                        .When(x => true)
                        .Then(x => x.ExecutionOrder.Add("L2Stop"))
                        .StopIfMatched())
                    .Add(Rule<TestObject>.For("Should not execute")
                        .When(x => true)
                        .Then(x => x.ExecutionOrder.Add("Should not")))))
            .AddGroup("Level1B", group => group
                .Add(Rule<TestObject>.For("Should not execute")
                    .When(x => true)
                    .Then(x => x.ExecutionOrder.Add("Should not"))));

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, rules);

        // Assert
        obj.ExecutionOrder.ShouldBe(new[] { "L1", "L2Stop" });
    }
}
