using System.Text.Json;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Persistence;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Conditions.Operators;
using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Persistence;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Conditions;

public class ConditionSystemTests
{
    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ConditionValidator_rejects_leaf_with_both_compare_field_and_value()
    {
        var leaf = new ConditionLeaf
        {
            Field = "Amount",
            Operator = "equals",
            Value = 1,
            CompareToField = "Other"
        };

        Should.Throw<InvalidOperationException>(() => ConditionValidator.Validate(leaf));
    }

    [Fact]
    public void ConditionValidator_accepts_literal_null_value()
    {
        var leaf = new ConditionLeaf
        {
            Field = "Country",
            Operator = "equals",
            Value = null
        };

        ConditionValidator.Validate(leaf);
    }

    [Fact]
    public void RuleSetDefinition_deserializes_condition_from_json()
    {
        const string json = """
            {"name":"S","rules":[{"name":"R","condition":{"kind":"leaf","field":"Amount","operator":"greater_than","value":1},"actionKeys":["A"]}],"groups":[]}
            """;

        var def = JsonSerializer.Deserialize<RuleSetDefinition>(json, JsonOptions);
        def.ShouldNotBeNull();
        def!.Rules.Count.ShouldBe(1);
        def.Rules[0].Condition.ShouldBeOfType<ConditionLeaf>();
    }

    [Fact]
    public void ConditionNode_round_trips_json_polymorphic()
    {
        var tree = new ConditionGroup
        {
            Operator = "OR",
            Conditions =
            [
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 100m },
                new ConditionLeaf { Field = "Amount", Operator = "less_than", CompareToField = "MaxOrderValue" }
            ]
        };

        var json = JsonSerializer.Serialize<ConditionNode>(tree, JsonOptions);
        var back = JsonSerializer.Deserialize<ConditionNode>(json, JsonOptions);
        back.ShouldBeOfType<ConditionGroup>();
        var g = (ConditionGroup)back!;
        g.Conditions.Count.ShouldBe(2);
        g.Conditions[0].ShouldBeOfType<ConditionLeaf>();
    }

    [Fact]
    public void ReflectionFieldResolver_resolves_simple_properties()
    {
        var r = new ReflectionFieldResolver<SampleDto>();
        var dto = new SampleDto { Amount = 42, MaxOrderValue = 10m };

        r.GetValue(dto, "Amount").ShouldBe(42);
        r.GetFieldType("Amount").ShouldBe(typeof(decimal));
    }

    [Fact]
    public void DefaultOperatorRegistry_resolves_operators_case_insensitive()
    {
        var reg = new DefaultOperatorRegistry();
        reg.Get("EQUALS").Name.ShouldBe("equals");
        reg.Get("Greater_Than").Name.ShouldBe("greater_than");
    }

    [Fact]
    public void ConditionEvaluator_evaluates_leaf_and_nested_groups()
    {
        var field = new ReflectionFieldResolver<SampleDto>();
        var ops = new DefaultOperatorRegistry();
        var conv = new DefaultValueConverter();
        var eval = new ConditionEvaluator<SampleDto>(field, ops, conv);
        var ctx = new DefaultRuleContext();

        var tree = new ConditionGroup
        {
            Operator = "AND",
            Conditions =
            [
                new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 50m },
                new ConditionGroup
                {
                    Operator = "OR",
                    Conditions =
                    [
                        new ConditionLeaf { Field = "Country", Operator = "equals", Value = "US" },
                        new ConditionLeaf { Field = "Country", Operator = "equals", Value = "DE" }
                    ]
                }
            ]
        };

        var dto = new SampleDto { Amount = 100m, Country = "US" };
        eval.Evaluate(dto, tree, ctx).ShouldBeTrue();

        dto.Country = "DE";
        eval.Evaluate(dto, tree, ctx).ShouldBeTrue();

        dto.Amount = 40m;
        dto.Country = "US";
        eval.Evaluate(dto, tree, ctx).ShouldBeFalse();
    }

    [Fact]
    public void ConditionEvaluator_field_to_field_and_between_and_in()
    {
        var field = new ReflectionFieldResolver<SampleDto>();
        var ops = new DefaultOperatorRegistry();
        var conv = new DefaultValueConverter();
        var eval = new ConditionEvaluator<SampleDto>(field, ops, conv);
        var ctx = new DefaultRuleContext();

        var gtField = new ConditionLeaf
        {
            Field = "Amount",
            Operator = "greater_than",
            CompareToField = "MaxOrderValue"
        };

        eval.Evaluate(new SampleDto { Amount = 1500m, MaxOrderValue = 1000m }, gtField, ctx).ShouldBeTrue();
        eval.Evaluate(new SampleDto { Amount = 500m, MaxOrderValue = 1000m }, gtField, ctx).ShouldBeFalse();

        var between = new ConditionLeaf
        {
            Field = "Amount",
            Operator = "between",
            Value = new[] { 100m, 500m }
        };
        eval.Evaluate(new SampleDto { Amount = 250m }, between, ctx).ShouldBeTrue();
        eval.Evaluate(new SampleDto { Amount = 50m }, between, ctx).ShouldBeFalse();

        var inn = new ConditionLeaf
        {
            Field = "Country",
            Operator = "in",
            Value = new[] { "US", "CA" }
        };
        eval.Evaluate(new SampleDto { Country = "US" }, inn, ctx).ShouldBeTrue();
        eval.Evaluate(new SampleDto { Country = "DE" }, inn, ctx).ShouldBeFalse();
    }

    [Fact]
    public void RuleDefinitionMapper_executes_dynamic_condition_via_engine()
    {
        var registry = new RuleRegistry<SampleDto>();
        registry.RegisterAction("Mark", (dto, _) => dto.Flag = true);

        var field = new ReflectionFieldResolver<SampleDto>();
        var eval = new ConditionEvaluator<SampleDto>(field, new DefaultOperatorRegistry(), new DefaultValueConverter());
        var mapper = new RuleDefinitionMapper<SampleDto>(registry, eval);

        var def = new RuleDefinition
        {
            Name = "Dynamic",
            Condition = new ConditionLeaf { Field = "Amount", Operator = "greater_than", Value = 10m },
            ActionKeys = ["Mark"]
        };

        var rule = mapper.MapRule(def);
        var set = RuleSet.For<SampleDto>("S").Add(rule);
        var engine = new RuleEngine();

        var ok = new SampleDto { Amount = 20m };
        engine.Evaluate(ok, set);
        ok.Flag.ShouldBeTrue();

        var no = new SampleDto { Amount = 5m };
        engine.Evaluate(no, set);
        no.Flag.ShouldBeFalse();
    }

    private sealed class SampleDto
    {
        public decimal Amount { get; set; }
        public decimal MaxOrderValue { get; set; }
        public string Country { get; set; } = "US";
        public bool Flag { get; set; }
    }
}
