using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Observability;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Observability;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Engine;

/// <summary>
/// Tests for the observability layer integrated into RuleEngine.
/// Verifies metrics collection, observer callbacks, timing, and performance characteristics.
/// </summary>
public class ObservabilityTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public bool Flag { get; set; }
        public List<string> Changes { get; } = new();
    }

    [Fact]
    public void Disabled_Observability_Should_Not_Populate_Metrics()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = false
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldBeNull();
    }

    [Fact]
    public void Enabled_Observability_Should_Populate_Metrics()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalRulesEvaluated.ShouldBe(1);
        result.Metrics.RulesMatched.ShouldBe(1);
    }

    [Fact]
    public void Observability_Should_Track_Multiple_Rules()
    {
        // Arrange
        var obj = new TestObject { Value = 15 };

        var rule1 = Rule<TestObject>.For("Rule 1")
            .When(x => x.Value > 10)
            .Then(x => x.Flag = true);

        var rule2 = Rule<TestObject>.For("Rule 2")
            .When(x => x.Value < 20)
            .Then(x => x.Changes.Add("changed"));

        var rule3 = Rule<TestObject>.For("Rule 3")
            .When(x => x.Value == 999)
            .Then(x => { });

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule1)
            .Add(rule2)
            .Add(rule3);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalRulesEvaluated.ShouldBe(3);
        result.Metrics.RulesMatched.ShouldBe(2);
    }

    [Fact]
    public async Task Observability_Should_Work_With_Async_Rules()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var asyncRule = Rule<TestObject>.For("Async Rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(5);
                return x.Value > 5;
            })
            .ThenAsync(async x =>
            {
                await Task.Delay(5);
                x.Flag = true;
            });

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(asyncRule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = await engine.EvaluateAsync(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalRulesEvaluated.ShouldBe(1);
        result.Metrics.RulesMatched.ShouldBe(1);
    }

    [Fact]
    public void Observability_Should_Track_Groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var ruleSet = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Root rule")
                .When(x => true)
                .Then(x => x.Flag = true))
            .AddGroup("Group1", g => g
                .Add(Rule<TestObject>.For("Group rule 1")
                    .When(x => true)
                    .Then(x => { })))
            .AddGroup("Group2", g => g
                .Add(Rule<TestObject>.For("Group rule 2")
                    .When(x => true)
                    .Then(x => { })));

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.GroupsTraversed.ShouldBe(2);
    }

    [Fact]
    public void Observability_Should_Track_Stop_Processing()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var ruleSet = RuleSet<TestObject>.For("Main")
            .Add(Rule<TestObject>.For("Stopping Rule")
                .When(x => true)
                .Then(x => x.Flag = true)
                .StopIfMatched())
            .Add(Rule<TestObject>.For("Should not execute")
                .When(x => true)
                .Then(x => { }));

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Executions.Any(e => e.StoppedProcessing).ShouldBeTrue();
    }

    [Fact]
    public void Observability_Should_Use_Built_In_Observer_By_Default()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            Observer = null // Not providing a custom observer
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalRulesEvaluated.ShouldBe(1);
    }

    [Fact]
    public void Observability_Should_Support_Custom_Observer()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var callLog = new List<string>();

        var customObserver = new TestObserver(callLog);

        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            Observer = customObserver
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        callLog.Count.ShouldBeGreaterThan(0);
        callLog.ShouldContain("OnRuleEvaluating");
        callLog.ShouldContain("OnRuleMatched");
        callLog.ShouldContain("OnRuleExecuted");
        callLog.ShouldContain("OnExecutionCompleted");
    }

    [Fact]
    public void Observability_Should_Call_Observer_In_Correct_Order()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var callOrder = new List<string>();

        var orderedObserver = new TestObserver(callOrder);

        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            Observer = orderedObserver
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        var evaluatingIndex = callOrder.IndexOf("OnRuleEvaluating");
        var matchedIndex = callOrder.IndexOf("OnRuleMatched");
        var executedIndex = callOrder.IndexOf("OnRuleExecuted");
        var completedIndex = callOrder.IndexOf("OnExecutionCompleted");

        evaluatingIndex.ShouldBeLessThan(matchedIndex);
        matchedIndex.ShouldBeLessThan(executedIndex);
        executedIndex.ShouldBeLessThan(completedIndex);
    }

    [Fact]
    public void Observability_Should_Tolerate_Observer_Exceptions()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var throwingObserver = new ThrowingObserver();

        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            Observer = throwingObserver
        };

        var engine = new RuleEngine();

        // Act & Assert - Should not throw despite observer throwing
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert execution succeeded
        result.AppliedRules.ShouldContain("Test rule");
        obj.Flag.ShouldBeTrue();
    }

    [Fact]
    public void Observability_With_Detailed_Timing_Should_Include_Duration()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("Test rule")
            .WhenAsync(async x =>
            {
                await Task.Delay(10);
                return x.Value > 5;
            })
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            EnableDetailedTiming = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalElapsedMilliseconds.ShouldNotBeNull();
        result.Metrics.TotalElapsedMilliseconds!.Value.ShouldBeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Observability_Without_Detailed_Timing_Should_Not_Include_Duration()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };

        var rule = Rule<TestObject>.For("Test rule")
            .When(x => x.Value > 5)
            .Then(x => x.Flag = true);

        var ruleSet = RuleSet<TestObject>.For("Test")
            .Add(rule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true,
            EnableDetailedTiming = false
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.TotalElapsedMilliseconds.ShouldBeNull();
    }

    [Fact]
    public void Observability_Should_Track_Nested_Groups()
    {
        // Arrange
        var obj = new TestObject { Value = 0 };

        var ruleSet = RuleSet<TestObject>.For("Main")
            .AddGroup("Level1", g => g
                .Add(Rule<TestObject>.For("L1 rule")
                    .When(x => true)
                    .Then(x => { }))
                .AddGroup("Level2", sub => sub
                    .Add(Rule<TestObject>.For("L2 rule")
                        .When(x => true)
                        .Then(x => { }))));

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableObservability = true
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        result.Metrics.ShouldNotBeNull();
        result.Metrics!.GroupsTraversed.ShouldBe(2);
    }

    // Helper test observer that logs callback invocations
    private sealed class TestObserver : IRuleObserver<TestObject>
    {
        private readonly List<string> _log;

        public TestObserver(List<string> log)
        {
            _log = log;
        }

        public void OnRuleEvaluating(RuleEvaluationContext<TestObject> context)
        {
            _log.Add("OnRuleEvaluating");
        }

        public void OnRuleMatched(RuleMatchContext<TestObject> context)
        {
            _log.Add("OnRuleMatched");
        }

        public void OnRuleExecuted(RuleExecutionContext<TestObject> context)
        {
            _log.Add("OnRuleExecuted");
        }

        public void OnExecutionCompleted(RuleExecutionSummary summary)
        {
            _log.Add("OnExecutionCompleted");
        }
    }

    // Observer that throws to test exception handling
    private sealed class ThrowingObserver : IRuleObserver<TestObject>
    {
        public void OnRuleEvaluating(RuleEvaluationContext<TestObject> context)
        {
            throw new InvalidOperationException("Observer error");
        }

        public void OnRuleMatched(RuleMatchContext<TestObject> context)
        {
            throw new InvalidOperationException("Observer error");
        }

        public void OnRuleExecuted(RuleExecutionContext<TestObject> context)
        {
            throw new InvalidOperationException("Observer error");
        }

        public void OnExecutionCompleted(RuleExecutionSummary summary)
        {
            throw new InvalidOperationException("Observer error");
        }
    }
}
