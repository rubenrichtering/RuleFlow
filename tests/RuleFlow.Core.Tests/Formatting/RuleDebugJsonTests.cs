using System.Text.Json;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Formatting;

/// <summary>
/// Tests for <c>result.ToDebugJson()</c> — verifies JSON output stability, structure, and null-safety.
/// </summary>
public class RuleDebugJsonTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public bool Flag { get; set; }
    }

    // ── Null / empty input ───────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_With_Null_Result_Returns_Empty_Json_Object()
    {
        RuleResult? result = null;

        var json = result.ToDebugJson();

        json.ShouldBe("{}");
    }

    [Fact]
    public void ToDebugJson_With_Null_View_Returns_Empty_Json_Object()
    {
        RuleExecutionDebugView? view = null;

        var json = view.ToDebugJson();

        json.ShouldBe("{}");
    }

    // ── Valid JSON ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Produces_Valid_Parseable_Json()
    {
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Check Value").When(x => x.Value > 0).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("MyRuleSet").Add(rule);
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var json = result.ToDebugJson();

        Should.NotThrow(() => JsonDocument.Parse(json));
    }

    [Fact]
    public void ToDebugJson_Contains_RuleSetName()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("OrderApproval")
            .Add(Rule<TestObject>.For("R").When(x => true).Then(x => { }));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var doc = JsonDocument.Parse(result.ToDebugJson());

        doc.RootElement.GetProperty("ruleSetName").GetString().ShouldBe("OrderApproval");
    }

    [Fact]
    public void ToDebugJson_Contains_Rule_With_Matched_Field()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("ActiveCheck").When(x => x.Value > 0).Then(x => { }));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var doc = JsonDocument.Parse(result.ToDebugJson());
        var firstRule = doc.RootElement.GetProperty("rules")[0];

        firstRule.GetProperty("name").GetString().ShouldBe("ActiveCheck");
        firstRule.GetProperty("matched").GetBoolean().ShouldBeTrue();
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Is_Deterministic_Across_Multiple_Calls()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("DeterminismTest")
            .Add(Rule<TestObject>.For("R1").When(x => x.Value > 0).Then(x => { }))
            .Add(Rule<TestObject>.For("R2").When(x => x.Value < 100).Then(x => { }))
            .Add(Rule<TestObject>.For("R3").When(x => x.Value == 999).Then(x => { }));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var json1 = result.ToDebugJson();
        var json2 = result.ToDebugJson();
        var json3 = result.ToDebugJson();

        json2.ShouldBe(json1);
        json3.ShouldBe(json1);
    }

    // ── Metrics ───────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Without_Observability_Omits_Metrics_Property()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("R").When(x => true).Then(x => { }));
        var options = new RuleExecutionOptions<TestObject> { EnableObservability = false };
        var result = new RuleEngine().Evaluate(obj, ruleSet, options);

        var doc = JsonDocument.Parse(result.ToDebugJson());

        doc.RootElement.TryGetProperty("metrics", out _).ShouldBeFalse();
    }

    [Fact]
    public void ToDebugJson_With_Observability_Includes_Metrics()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("MatchedRule").When(x => x.Value > 0).Then(x => { }))
            .Add(Rule<TestObject>.For("MissedRule").When(x => x.Value < 0).Then(x => { }));
        var options = new RuleExecutionOptions<TestObject> { EnableObservability = true };
        var result = new RuleEngine().Evaluate(obj, ruleSet, options);

        var doc = JsonDocument.Parse(result.ToDebugJson());
        var metrics = doc.RootElement.GetProperty("metrics");

        metrics.GetProperty("rulesEvaluated").GetInt32().ShouldBe(2);
        metrics.GetProperty("rulesMatched").GetInt32().ShouldBe(1);
    }

    // ── StopProcessing ────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_StopProcessing_Rule_Has_StoppedProcessing_True()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("StopRule").When(x => x.Value > 0).Then(x => { }).StopIfMatched())
            .Add(Rule<TestObject>.For("NeverReached").When(x => true).Then(x => { }));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var doc = JsonDocument.Parse(result.ToDebugJson());
        var stopRule = doc.RootElement.GetProperty("rules")[0];

        stopRule.GetProperty("name").GetString().ShouldBe("StopRule");
        stopRule.GetProperty("stoppedProcessing").GetBoolean().ShouldBeTrue();
    }

    // ── Nested groups ─────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Nested_Groups_Have_Correct_FullPath()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .AddGroup("Outer", g => g
                .AddGroup("Inner", g2 => g2
                    .Add(Rule<TestObject>.For("DeepRule").When(x => true).Then(x => { }))));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var doc = JsonDocument.Parse(result.ToDebugJson());
        var outerGroup = doc.RootElement.GetProperty("groups")[0];
        var innerGroup = outerGroup.GetProperty("groups")[0];
        var deepRule = innerGroup.GetProperty("rules")[0];

        outerGroup.GetProperty("fullPath").GetString().ShouldBe("RS/Outer");
        innerGroup.GetProperty("fullPath").GetString().ShouldBe("RS/Outer/Inner");
        deepRule.GetProperty("name").GetString().ShouldBe("DeepRule");
    }

    // ── Condition tree serialization ──────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Condition_Leaf_Serializes_With_Kind_Discriminator()
    {
        var view = new RuleExecutionDebugView
        {
            RuleSetName = "ConditionTest",
            Rules =
            [
                new DebugRule
                {
                    Name = "LeafRule",
                    Executed = true,
                    Matched = true,
                    Condition = new DebugConditionLeaf
                    {
                        Field = "Amount",
                        Operator = "greater_than",
                        Expected = 1000,
                        Result = true
                    }
                }
            ]
        };

        var json = view.ToDebugJson();
        var doc = JsonDocument.Parse(json);
        var condition = doc.RootElement.GetProperty("rules")[0].GetProperty("condition");

        condition.GetProperty("kind").GetString().ShouldBe("leaf");
        condition.GetProperty("field").GetString().ShouldBe("Amount");
        condition.GetProperty("operator").GetString().ShouldBe("greater_than");
        condition.GetProperty("result").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void ToDebugJson_Condition_And_Or_Tree_Serializes_With_Correct_Structure()
    {
        var view = new RuleExecutionDebugView
        {
            RuleSetName = "ConditionTreeTest",
            Rules =
            [
                new DebugRule
                {
                    Name = "ComplexRule",
                    Executed = true,
                    Matched = true,
                    Condition = new DebugConditionGroup
                    {
                        Operator = "AND",
                        Result = true,
                        Children =
                        [
                            new DebugConditionLeaf { Field = "Value", Operator = "gt", Expected = 100, Result = true },
                            new DebugConditionGroup
                            {
                                Operator = "OR",
                                Result = true,
                                Children =
                                [
                                    new DebugConditionLeaf { Field = "Name", Operator = "eq", Expected = "VIP", Result = false },
                                    new DebugConditionLeaf { Field = "Flag", Operator = "eq", Expected = true, Result = true }
                                ]
                            }
                        ]
                    }
                }
            ]
        };

        var json = view.ToDebugJson();
        var doc = JsonDocument.Parse(json);
        var condition = doc.RootElement.GetProperty("rules")[0].GetProperty("condition");

        condition.GetProperty("kind").GetString().ShouldBe("group");
        condition.GetProperty("operator").GetString().ShouldBe("AND");

        var children = condition.GetProperty("children");
        children.GetArrayLength().ShouldBe(2);
        children[0].GetProperty("kind").GetString().ShouldBe("leaf");

        var nestedOr = children[1];
        nestedOr.GetProperty("kind").GetString().ShouldBe("group");
        nestedOr.GetProperty("operator").GetString().ShouldBe("OR");
        nestedOr.GetProperty("children").GetArrayLength().ShouldBe(2);
    }

    // ── Null safety ───────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugJson_Without_Explainability_Does_Not_Throw()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("R").When(x => true).Then(x => { }));
        var options = new RuleExecutionOptions<TestObject> { EnableExplainability = false };
        var result = new RuleEngine().Evaluate(obj, ruleSet, options);

        string json = null!;
        Should.NotThrow(() => json = result.ToDebugJson());

        json.ShouldNotBeNull();
        Should.NotThrow(() => JsonDocument.Parse(json));
    }

    [Fact]
    public void ToDebugJson_Is_Indented()
    {
        var obj = new TestObject { Value = 50 };
        var ruleSet = RuleSet<TestObject>.For("RS")
            .Add(Rule<TestObject>.For("R").When(x => true).Then(x => { }));
        var result = new RuleEngine().Evaluate(obj, ruleSet);

        var json = result.ToDebugJson();

        // Indented JSON always contains newlines
        json.ShouldContain(Environment.NewLine);
    }
}
