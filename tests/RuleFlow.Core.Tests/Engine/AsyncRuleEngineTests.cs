using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

public class AsyncRuleEngineTests
{
    private class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public bool Flag { get; set; }
        public List<string> Changes { get; } = new();
    }

    [Fact]
    public async Task Should_execute_rule_with_async_condition()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Async Condition Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Async Condition Rule");
    }

    [Fact]
    public async Task Should_execute_rule_with_async_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Async Action Rule")
            .When(x => x.Value > 5)
            .ThenAsync(async x =>
            {
                await Task.Delay(10);
                executed = true;
            });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Async Action Rule");
    }

    [Fact]
    public async Task Should_execute_rule_with_async_condition_and_async_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Async Condition and Action Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .ThenAsync(async x =>
            {
                await Task.Delay(10);
                executed = true;
            });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Async Condition and Action Rule");
    }

    [Fact]
    public async Task Should_support_mixed_sync_and_async_rules()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var syncRule = Rule<TestObject>.For("Sync Rule")
            .When(x => x.Value > 5)
            .Then(x => x.Changes.Add("sync"));

        var asyncRule = Rule<TestObject>.For("Async Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 8;
            })
            .ThenAsync(async x =>
            {
                await Task.Delay(10);
                x.Changes.Add("async");
            });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(syncRule)
            .Add(asyncRule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        result.AppliedRules.ShouldContain("Sync Rule");
        result.AppliedRules.ShouldContain("Async Rule");
        obj.Changes.ShouldContain("sync");
        obj.Changes.ShouldContain("async");
    }

    [Fact]
    public async Task Should_respect_priority_with_async_rules()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executionOrder = new List<string>();

        var lowPriorityRule = Rule<TestObject>.For("Low Priority")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                executionOrder.Add("low");
                return true;
            })
            .WithPriority(0);

        var highPriorityRule = Rule<TestObject>.For("High Priority")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                executionOrder.Add("high");
                return true;
            })
            .WithPriority(1);

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(lowPriorityRule)
            .Add(highPriorityRule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executionOrder[0].ShouldBe("high");
        executionOrder[1].ShouldBe("low");
    }

    [Fact]
    public async Task Should_stop_processing_with_async_rule()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executions = new List<string>();

        var stoppingRule = Rule<TestObject>.For("Stopping Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => executions.Add("stopping"))
            .StopIfMatched();

        var secondRule = Rule<TestObject>.For("Second Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => executions.Add("second"));

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(stoppingRule)
            .Add(secondRule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executions.ShouldContain("stopping");
        executions.ShouldNotContain("second");
        result.Executions.Single(e => e.RuleName == "Stopping Rule").StoppedProcessing.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_support_async_rules_in_groups()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var groupRule = Rule<TestObject>.For("Group Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => { executed = true; });

        var parentSet = RuleSet<TestObject>.For("Parent Set")
            .AddGroup("Test Group", g => g.Add(groupRule));

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, parentSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Group Rule");
    }

    [Fact]
    public async Task Should_not_execute_rule_when_async_condition_is_false()
    {
        // Arrange
        var obj = new TestObject { Value = 3 };
        var executed = false;

        var rule = Rule<TestObject>.For("Async Condition Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeFalse();
        result.AppliedRules.ShouldNotContain("Async Condition Rule");
    }

    [Fact]
    public async Task Should_fallback_to_sync_condition_when_async_not_set()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Sync Condition Rule")
            .When(x => x.Value > 5)
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Sync Condition Rule");
    }

    [Fact]
    public async Task Should_fallback_to_sync_action_when_async_not_set()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Sync Action Rule")
            .When(x => x.Value > 5)
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Sync Action Rule");
    }

    [Fact]
    public async Task Should_handle_multiple_async_rules_in_sequence()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executionOrder = new List<string>();

        var rule1 = Rule<TestObject>.For("Rule 1")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                executionOrder.Add("rule1");
                return true;
            });

        var rule2 = Rule<TestObject>.For("Rule 2")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                executionOrder.Add("rule2");
                return true;
            });

        var rule3 = Rule<TestObject>.For("Rule 3")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                executionOrder.Add("rule3");
                return true;
            });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executionOrder.Count.ShouldBe(3);
        result.AppliedRules.ShouldContain("Rule 1");
        result.AppliedRules.ShouldContain("Rule 2");
        result.AppliedRules.ShouldContain("Rule 3");
    }

    [Fact]
    public async Task Should_support_async_condition_with_sync_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Mixed Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => { executed = true; });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Mixed Rule");
    }

    [Fact]
    public async Task Should_support_sync_condition_with_async_action()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var executed = false;

        var rule = Rule<TestObject>.For("Mixed Rule")
            .When(x => x.Value > 5)
            .ThenAsync(async x =>
            {
                await Task.Delay(10);
                executed = true;
            });

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Mixed Rule");
    }

    [Fact]
    public async Task Should_track_async_rule_execution_in_result()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("Tracked Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Because("Value exceeds threshold");

        var ruleSet = RuleSet<TestObject>.For("Test Set")
            .Add(rule);

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet);

        // Assert
        var execution = result.Executions.SingleOrDefault(e => e.RuleName == "Tracked Rule");
        execution.ShouldNotBeNull();
        execution!.Matched.ShouldBeTrue();
        execution.Reason.ShouldBe("Value exceeds threshold");
    }
}
