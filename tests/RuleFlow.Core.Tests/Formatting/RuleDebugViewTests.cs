using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Formatting;

/// <summary>
/// Tests for <c>result.ToDebugView()</c> — verifies structural mapping from RuleResult to the Debug DTO.
/// </summary>
public class RuleDebugViewTests
{
    private class TestObject
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public bool Flag { get; set; }
    }

    // ── Null / empty input ───────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_With_Null_Result_Returns_Empty_View()
    {
        RuleResult? result = null;

        var view = result.ToDebugView();

        view.ShouldNotBeNull();
        view.RuleSetName.ShouldBe(string.Empty);
        view.Groups.ShouldBeEmpty();
        view.Rules.ShouldBeEmpty();
        view.Metrics.ShouldBeNull();
    }

    // ── Single rule mapping ──────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_Single_Matched_Rule_Maps_Correctly()
    {
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Check Value")
            .When(x => x.Value > 0)
            .Then(x => x.Flag = true);
        var ruleSet = RuleSet<TestObject>.For("MyRuleSet").Add(rule);

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        view.RuleSetName.ShouldBe("MyRuleSet");
        view.Rules.Count.ShouldBe(1);
        view.Rules[0].Name.ShouldBe("Check Value");
        view.Rules[0].Matched.ShouldBeTrue();
        view.Rules[0].Executed.ShouldBeTrue();
        view.Rules[0].Skipped.ShouldBeFalse();
    }

    [Fact]
    public void ToDebugView_Single_Not_Matched_Rule_Has_Matched_False()
    {
        var obj = new TestObject { Value = 5 };
        var rule = Rule<TestObject>.For("High Value").When(x => x.Value > 100).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS").Add(rule);

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        view.Rules[0].Matched.ShouldBeFalse();
        view.Rules[0].Executed.ShouldBeTrue();
    }

    // ── StopProcessing ───────────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_StopProcessing_Sets_StoppedProcessing_Flag()
    {
        var obj = new TestObject { Value = 50 };
        var stopRule = Rule<TestObject>.For("Stop Rule").When(x => x.Value > 0).Then(x => { }).StopIfMatched();
        var afterRule = Rule<TestObject>.For("After Rule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("StopTest").Add(stopRule).Add(afterRule);

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        var stopRuleView = view.Rules.Single(r => r.Name == "Stop Rule");
        stopRuleView.StoppedProcessing.ShouldBeTrue();
    }

    // ── Action count ─────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_ActionsExecuted_Count_Matches_Executed_Actions()
    {
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Multi Action")
            .When(x => x.Value > 0)
            .Then(x => x.Flag = true)
            .Then(x => x.Name = "set");
        var ruleSet = RuleSet<TestObject>.For("RS").Add(rule);

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        view.Rules[0].ActionsExecuted.ShouldBe(2);
        view.Rules[0].Actions.Count.ShouldBe(2);
        view.Rules[0].Actions.ShouldAllBe(a => a.Executed);
    }

    // ── Group structure ──────────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_Group_With_Rules_Has_Correct_Structure()
    {
        var obj = new TestObject { Value = 50 };
        var innerRule = Rule<TestObject>.For("Inner Rule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS")
            .AddGroup("GroupA", g => g.Add(innerRule));

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        view.Groups.Count.ShouldBe(1);
        view.Groups[0].Name.ShouldBe("GroupA");
        view.Groups[0].FullPath.ShouldBe("RS/GroupA");
        view.Groups[0].Rules.Count.ShouldBe(1);
        view.Groups[0].Rules[0].Name.ShouldBe("Inner Rule");
    }

    [Fact]
    public void ToDebugView_Three_Level_Nested_Groups_Have_Correct_FullPaths()
    {
        // Arrange: RS → L1 → L2 → L3 → DeepRule
        var obj = new TestObject { Value = 50 };
        var deepRule = Rule<TestObject>.For("Deep Rule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS")
            .AddGroup("L1", g1 => g1
                .AddGroup("L2", g2 => g2
                    .AddGroup("L3", g3 => g3
                        .Add(deepRule))));

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        var l1 = view.Groups.ShouldHaveSingleItem();
        l1.FullPath.ShouldBe("RS/L1");

        var l2 = l1.Groups.ShouldHaveSingleItem();
        l2.FullPath.ShouldBe("RS/L1/L2");

        var l3 = l2.Groups.ShouldHaveSingleItem();
        l3.FullPath.ShouldBe("RS/L1/L2/L3");

        l3.Rules.ShouldHaveSingleItem().Name.ShouldBe("Deep Rule");
    }

    [Fact]
    public void ToDebugView_Duplicate_Group_Names_Get_Distinct_FullPaths()
    {
        // Two groups both named "Validation" but at different hierarchy levels
        var obj = new TestObject { Value = 50 };
        var rule1 = Rule<TestObject>.For("Rule 1").When(x => true).Then(x => { });
        var rule2 = Rule<TestObject>.For("Rule 2").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS")
            .AddGroup("Validation", g => g.Add(rule1))
            .AddGroup("Processing", g => g
                .AddGroup("Validation", g2 => g2.Add(rule2)));

        var result = new RuleEngine().Evaluate(obj, ruleSet);
        var view = result.ToDebugView();

        var topValidation = view.Groups.Single(g => g.Name == "Validation");
        var nestedValidation = view.Groups.Single(g => g.Name == "Processing").Groups[0];

        topValidation.FullPath.ShouldBe("RS/Validation");
        nestedValidation.FullPath.ShouldBe("RS/Processing/Validation");
        topValidation.FullPath.ShouldNotBe(nestedValidation.FullPath);
    }

    // ── Metrics ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_Without_Observability_Has_Null_Metrics()
    {
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("Rule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS").Add(rule);
        var options = new RuleExecutionOptions<TestObject> { EnableObservability = false };

        var result = new RuleEngine().Evaluate(obj, ruleSet, options);
        var view = result.ToDebugView();

        view.Metrics.ShouldBeNull();
    }

    [Fact]
    public void ToDebugView_With_Observability_Populates_Metrics()
    {
        var obj = new TestObject { Value = 50 };
        var r1 = Rule<TestObject>.For("R1").When(x => x.Value > 0).Then(x => { });
        var r2 = Rule<TestObject>.For("R2").When(x => x.Value < 0).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS").Add(r1).Add(r2);
        var options = new RuleExecutionOptions<TestObject> { EnableObservability = true };

        var result = new RuleEngine().Evaluate(obj, ruleSet, options);
        var view = result.ToDebugView();

        view.Metrics.ShouldNotBeNull();
        view.Metrics!.RulesEvaluated.ShouldBe(2);
        view.Metrics.RulesMatched.ShouldBe(1);
    }

    // ── Flat execution fallback (explainability disabled) ────────────────────

    [Fact]
    public void ToDebugView_Without_Explainability_Returns_Valid_View()
    {
        var obj = new TestObject { Value = 50 };
        var rule = Rule<TestObject>.For("FlatRule").When(x => true).Then(x => { });
        var ruleSet = RuleSet<TestObject>.For("RS").Add(rule);
        var options = new RuleExecutionOptions<TestObject> { EnableExplainability = false };

        var result = new RuleEngine().Evaluate(obj, ruleSet, options);

        // Should not throw and should return a valid (possibly flat) view
        Should.NotThrow(() =>
        {
            var view = result.ToDebugView();
            view.ShouldNotBeNull();
        });
    }

    // ── Null safety ───────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugView_Never_Throws_Even_On_Direct_Call()
    {
        // Ensure no exception is raised regardless of result state
        var view = ((RuleResult?)null).ToDebugView();
        view.ShouldNotBeNull();
    }
}
