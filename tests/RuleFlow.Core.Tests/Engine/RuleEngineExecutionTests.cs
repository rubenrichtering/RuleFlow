using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
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

    private sealed class CustomRule : IRule<TestObject>
    {
        private readonly Func<TestObject, IRuleContext, bool> _condition;
        private readonly Action<TestObject, IRuleContext> _action;

        public CustomRule(
            string name,
            Func<TestObject, IRuleContext, bool> condition,
            Action<TestObject, IRuleContext> action)
        {
            Name = name;
            _condition = condition;
            _action = action;
        }

        public string Name { get; }
        public string? Reason => null;
        public int Priority => 0;
        public bool StopProcessing => false;
        public IReadOnlyDictionary<string, object?> Metadata => new Dictionary<string, object?>();

        public bool Evaluate(TestObject input, IRuleContext context) => _condition(input, context);

        public Task<bool> EvaluateAsync(TestObject input, IRuleContext context)
            => Task.FromResult(_condition(input, context));

        public void Execute(TestObject input, IRuleContext context) => _action(input, context);

        public Task ExecuteAsync(TestObject input, IRuleContext context)
        {
            _action(input, context);
            return Task.CompletedTask;
        }
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

    [Fact]
    public void Should_execute_actions_for_custom_irule_implementations_with_explainability_enabled()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var actionCount = 0;

        var customRule = new CustomRule(
            "Custom Rule",
            condition: (x, _) => x.Value > 5,
            action: (x, _) =>
            {
                actionCount++;
                x.Flag = true;
            });

        var ruleSet = RuleSet<TestObject>.For("Custom Set")
            .Add(customRule);

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet);

        // Assert
        actionCount.ShouldBe(1);
        obj.Flag.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Custom Rule");
    }

    [Fact]
    public void Should_execute_actions_for_custom_irule_implementations_with_explainability_disabled()
    {
        // Arrange
        var obj = new TestObject { Value = 10 };
        var actionCount = 0;

        var customRule = new CustomRule(
            "Custom Rule",
            condition: (x, _) => x.Value > 5,
            action: (_, _) => actionCount++);

        var ruleSet = RuleSet<TestObject>.For("Custom Set")
            .Add(customRule);

        var options = new RuleExecutionOptions<TestObject>
        {
            EnableExplainability = false
        };

        var engine = new RuleEngine();

        // Act
        var result = engine.Evaluate(obj, ruleSet, options);

        // Assert
        actionCount.ShouldBe(1);
        result.Root.ShouldBeNull();
        result.AppliedRules.ShouldContain("Custom Rule");
    }
}
