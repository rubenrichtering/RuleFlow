using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;
using Xunit;

namespace RuleFlow.Core.Tests.Engine;

/// <summary>
/// Real-world scenario tests for production use cases.
/// These tests validate that RuleFlow handles complex, realistic workflows correctly.
/// </summary>
public class ScenarioTests
{
    #region Test Objects

    private class Order
    {
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";
        public bool RequiresApproval { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public string? ApprovalReason { get; set; }
        public int DiscountPercent { get; set; }
    }

    private class Customer
    {
        public string Name { get; set; } = string.Empty;
        public int VIPStatus { get; set; } // 0=regular, 1=gold, 2=platinum
        public int OrderCount { get; set; }
        public bool IsBlacklisted { get; set; }
        public Customer? ReferredBy { get; set; }
    }

    private class FlexibleObject
    {
        public Dictionary<string, object?> Properties { get; set; } = new();

        public object? GetProperty(string name) => Properties.TryGetValue(name, out var value) ? value : null;
    }

    #endregion

    #region Scenario 1: Order Approval Flow (Standard Business Logic)

    [Fact]
    public void Scenario_1A_Order_Approval_Basic()
    {
        // ARRANGE: Order approval rules
        // - Amount > 1000 → Requires approval
        // - Amount > 5000 → Requires manager approval
        var order = new Order { Amount = 3000 };

        var rule1 = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .Because("Amount exceeds approval threshold");

        var rule2 = Rule<Order>.For("Check Very High Amount")
            .When(o => o.Amount > 5000)
            .Then(o => o.RequiresManagerApproval = true)
            .Because("Amount requires manager approval");

        var ruleSet = RuleSet<Order>.For("ApprovalRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, ruleSet);

        // ASSERT
        order.RequiresApproval.ShouldBeTrue();
        order.RequiresManagerApproval.ShouldBeFalse();
        result.AppliedRules.ShouldContain("Check High Amount");
        result.AppliedRules.ShouldNotContain("Check Very High Amount");
    }

    [Fact]
    public void Scenario_1B_Order_Approval_With_Priority()
    {
        // Rules execute in priority order; highest priority first
        var order = new Order { Amount = 6000 };

        var rule1 = Rule<Order>.For("Check Very High Amount")
            .When(o => o.Amount > 5000)
            .Then(o => { order.Status = "RequiresManager"; order.RequiresManagerApproval = true; })
            .WithPriority(100); // Executes first

        var rule2 = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o => { order.Status = "RequiresApproval"; order.RequiresApproval = true; })
            .WithPriority(50); // Executes second

        var ruleSet = RuleSet<Order>.For("ApprovalRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, ruleSet);

        // ASSERT - Both rules should match and execute in priority order
        order.RequiresApproval.ShouldBeTrue();
        order.RequiresManagerApproval.ShouldBeTrue();
        order.Status.ShouldBe("RequiresApproval"); // Last rule to execute sets the final status
        result.AppliedRules.Count().ShouldBe(2);
    }

    [Fact]
    public void Scenario_1C_Order_Approval_With_StopProcessing()
    {
        // When StopIfMatched is true and a rule matches, processing stops
        var order = new Order { Amount = 6000, RequiresApproval = false };
        var executionOrder = new List<string>();

        var rule1 = Rule<Order>.For("Check Very High Amount")
            .When(o => o.Amount > 5000)
            .Then(o =>
            {
                executionOrder.Add("rule1");
                order.RequiresManagerApproval = true;
            })
            .StopIfMatched();

        var rule2 = Rule<Order>.For("Check High Amount")
            .When(o => o.Amount > 1000)
            .Then(o =>
            {
                executionOrder.Add("rule2");
                order.RequiresApproval = true;
            });

        var ruleSet = RuleSet<Order>.For("ApprovalRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, ruleSet);

        // ASSERT
        executionOrder.ShouldContain("rule1");
        executionOrder.ShouldNotContain("rule2"); // rule2 never executes due to stop
        order.RequiresManagerApproval.ShouldBeTrue();
        order.RequiresApproval.ShouldBeFalse(); // rule2 didn't execute
        result.AppliedRules.ShouldContain("Check Very High Amount");
        result.AppliedRules.ShouldNotContain("Check High Amount");
    }

    #endregion

    #region Scenario 2: Nested Rule Groups (Complex Business Logic)

    [Fact]
    public void Scenario_2A_Nested_Groups_Basic()
    {
        // Multiple groups of rules nested together
        // Group A: High-value orders > 5000
        // Group B: VIP customer special handling
        var order = new Order { Amount = 6000, Quantity = 10 }; // Initialize Quantity

        var mainRuleSet = RuleSet<Order>.For("MainProcessing")
            .AddGroup("HighValueRules", g => g
                .Add(Rule<Order>.For("Premium Processing")
                    .When(o => o.Amount > 5000)
                    .Then(o => o.Status = "PremiumProcessing")))
            .AddGroup("GeneralRules", g => g
                .Add(Rule<Order>.For("Validate Quantity")
                    .When(o => o.Quantity > 0)
                    .Then(o => o.Status = "QuantityOk")));

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, mainRuleSet);

        // ASSERT
        result.AppliedRules.ShouldContain("Premium Processing");
        result.AppliedRules.ShouldContain("Validate Quantity");
        // Both rules execute; last execution sets the status
        order.Status.ShouldBe("QuantityOk");
    }

    [Fact]
    public void Scenario_2B_Nested_Groups_With_Execution_Filter()
    {
        // Only execute rules from specific group
        var order = new Order { Amount = 6000 };

        var mainRuleSet = RuleSet<Order>.For("MainProcessing")
            .AddGroup("HighValueRules", g => g
                .Add(Rule<Order>.For("Premium Processing")
                    .When(o => o.Amount > 5000)
                    .Then(o => order.Status = "PremiumProcessing")))
            .AddGroup("GeneralRules", g => g
                .Add(Rule<Order>.For("Validate Quantity")
                    .When(o => true)
                    .Then(o => order.Status = "QuantityOk")));

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order>
        {
            IncludeGroups = new[] { "HighValueRules" }
        };

        // ACT
        var result = engine.Evaluate(order, mainRuleSet, options);

        // ASSERT - Only HighValueRules group should execute
        result.AppliedRules.ShouldContain("Premium Processing");
        result.AppliedRules.ShouldNotContain("Validate Quantity");
        order.Status.ShouldBe("PremiumProcessing");
    }

    #endregion

    #region Scenario 3: Dynamic Conditions

    [Fact]
    public void Scenario_3A_Dynamic_Condition_FieldToValue()
    {
        // Using dynamic conditions to compare field values against constants
        var obj = new FlexibleObject
        {
            Properties = new Dictionary<string, object?>
            {
                { "Priority", 10 }
            }
        };

        var rule = Rule<FlexibleObject>.For("High Priority")
            .When(x => x.GetProperty("Priority") is int p && p > 5)
            .Then(x => { /* action */ });

        var ruleSet = RuleSet<FlexibleObject>.For("DynamicRules")
            .Add(rule);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(obj, ruleSet);

        // ASSERT
        result.AppliedRules.ShouldContain("High Priority");
    }

    [Fact]
    public void Scenario_3B_Dynamic_Condition_NestedProperties()
    {
        // Accessing nested properties dynamically
        var customer = new Customer { Name = "Alice", VIPStatus = 2 };
        var executed = false;

        var rule = Rule<Customer>.For("VIP Customer")
            .When(c => c.VIPStatus >= 1)
            .Then(c => { executed = true; });

        var ruleSet = RuleSet<Customer>.For("CustomerRules")
            .Add(rule);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(customer, ruleSet);

        // ASSERT
        executed.ShouldBeTrue();
        result.AppliedRules.ShouldContain("VIP Customer");
    }

    [Fact]
    public void Scenario_3C_Dynamic_Condition_Null_Navigation()
    {
        // Safely handling null intermediate properties
        var customer = new Customer { Name = "Bob", ReferredBy = null };
        var reached = false;

        var rule = Rule<Customer>.For("Has Referrer")
            .When(c => c.ReferredBy != null && c.ReferredBy.VIPStatus > 0)
            .Then(c => { reached = true; });

        var ruleSet = RuleSet<Customer>.For("ReferralRules")
            .Add(rule);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(customer, ruleSet);

        // ASSERT
        reached.ShouldBeFalse();
        result.AppliedRules.ShouldNotContain("Has Referrer");
    }

    #endregion

    #region Scenario 4: Conditional Action Chains

    [Fact]
    public void Scenario_4A_Multiple_Actions_Then_Chain()
    {
        var order = new Order { Amount = 3000 };

        var rule = Rule<Order>.For("Process High Order")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .Then(o => o.ApprovalReason = "High amount")
            .Then(o => o.Status = "Pending Approval");

        var ruleSet = RuleSet<Order>.For("ProcessingRules")
            .Add(rule);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, ruleSet);

        // ASSERT
        order.RequiresApproval.ShouldBeTrue();
        order.ApprovalReason.ShouldBe("High amount");
        order.Status.ShouldBe("Pending Approval");
        result.AppliedRules.ShouldContain("Process High Order");
    }

    [Fact]
    public void Scenario_4B_ThenIf_Conditional_Actions()
    {
        var order = new Order { Amount = 5500 };

        var rule = Rule<Order>.For("Apply Discount")
            .When(o => o.Amount > 1000)
            .Then(o => o.DiscountPercent = 10)
            .ThenIf(o => o.Amount > 5000, o => o.DiscountPercent = 20)
            .ThenIf(o => o.Amount > 10000, o => o.DiscountPercent = 30);

        var ruleSet = RuleSet<Order>.For("DiscountRules")
            .Add(rule);

        var engine = new RuleEngine();

        // ACT
        var result = engine.Evaluate(order, ruleSet);

        // ASSERT
        order.DiscountPercent.ShouldBe(20); // Last ThenIf that matched sets the value
        result.AppliedRules.ShouldContain("Apply Discount");
    }

    #endregion

    #region Scenario 5: Execution Options (Metadata Filtering)

    [Fact]
    public void Scenario_5A_Execution_Options_MetadataFilter()
    {
        var order = new Order { Amount = 3000 };
        var rule1Executed = false;
        var rule2Executed = false;

        var rule1 = Rule<Order>.For("Approval Rule")
            .When(o => o.Amount > 1000)
            .Then(o => { rule1Executed = true; })
            .WithMetadata("type", "approval");

        var rule2 = Rule<Order>.For("Audit Rule")
            .When(o => o.Amount > 500)
            .Then(o => { rule2Executed = true; })
            .WithMetadata("type", "audit");

        var ruleSet = RuleSet<Order>.For("ProcessingRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order>
        {
            MetadataFilter = (rule) =>
                rule.Metadata.TryGetValue("type", out var type) && type?.ToString() == "approval"
        };

        // ACT
        var result = engine.Evaluate(order, ruleSet, options);

        // ASSERT
        rule1Executed.ShouldBeTrue();
        rule2Executed.ShouldBeFalse(); // Filtered out
    }

    #endregion

    #region Scenario 6: Explainability Output

    [Fact]
    public void Scenario_6A_Explainability_Shows_Correct_Execution()
    {
        var order = new Order { Amount = 6000 };

        var rule1 = Rule<Order>.For("Check Manager Approval")
            .When(o => o.Amount > 5000)
            .Then(o => o.RequiresManagerApproval = true)
            .Because("Amount exceeds manager threshold")
            .WithPriority(100);

        var rule2 = Rule<Order>.For("Check Standard Approval")
            .When(o => o.Amount > 1000)
            .Then(o => o.RequiresApproval = true)
            .Because("Amount exceeds standard threshold");

        var ruleSet = RuleSet<Order>.For("ApprovalRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // ACT
        var result = engine.Evaluate(order, ruleSet, options);

        // ASSERT
        result.AppliedRules.Count().ShouldBe(2);
        result.AppliedRules.ShouldContain("Check Manager Approval");
        result.AppliedRules.ShouldContain("Check Standard Approval");
        
        // Verify explanation includes reasons
        var explanation = result.Explain();
        explanation.ShouldContain("Check Manager Approval");
        explanation.ShouldContain("Amount exceeds manager threshold");
    }

    [Fact]
    public void Scenario_6B_Explainability_With_StopProcessing()
    {
        // Explainability should reflect stop processing behavior
        var order = new Order { Amount = 6000 };
        var rule2Executed = false;

        var rule1 = Rule<Order>.For("Stop Rule")
            .When(o => o.Amount > 5000)
            .Then(o => order.Status = "Stopped")
            .StopIfMatched()
            .Because("Stopping further processing");

        var rule2 = Rule<Order>.For("Never Executed Rule")
            .When(o => o.Amount > 1000)
            .Then(o => { rule2Executed = true; });

        var ruleSet = RuleSet<Order>.For("ProcessingRules")
            .Add(rule1)
            .Add(rule2);

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // ACT
        var result = engine.Evaluate(order, ruleSet, options);

        // ASSERT
        rule2Executed.ShouldBeFalse();
        result.AppliedRules.ShouldContain("Stop Rule");
        result.AppliedRules.ShouldNotContain("Never Executed Rule");
        
        var explanation = result.Explain();
        explanation.ShouldContain("Stop Rule");
        explanation.ShouldNotContain("Never Executed Rule");
    }

    #endregion

    #region Scenario 7: Complex Multi-Condition Workflows

    [Fact]
    public void Scenario_7_Complex_Order_Processing_Workflow()
    {
        // A realistic order processing scenario combining multiple features
        var order = new Order
        {
            Amount = 7500,
            Quantity = 100,
            Status = "New"
        };

        var rules = RuleSet<Order>.For("CompleteOrderProcessing")
            .Add(Rule<Order>.For("Validate Quantity")
                .When(o => o.Quantity > 0)
                .Then(o => o.Status = "QuantityValid")
                .WithPriority(100)
                .Because("Quantity must be positive"))
            
            .Add(Rule<Order>.For("Check Low Amount")
                .When(o => o.Amount > 100 && o.Amount <= 5000)
                .Then(o => o.RequiresApproval = true)
                .WithPriority(90)
                .Because("Amount requires standard approval"))
            
            .Add(Rule<Order>.For("Check High Amount")
                .When(o => o.Amount > 5000)
                .Then(o => { o.RequiresApproval = true; o.RequiresManagerApproval = true; })
                .WithPriority(95)
                .Because("Amount requires manager approval"))
            
            .Add(Rule<Order>.For("Apply Bulk Discount")
                .When(o => o.Quantity >= 50)
                .Then(o => o.DiscountPercent = 15)
                .WithPriority(50)
                .Because("Bulk orders get discount"));

        var engine = new RuleEngine();
        var options = new RuleExecutionOptions<Order> { EnableExplainability = true };

        // ACT
        var result = engine.Evaluate(order, rules, options);

        // ASSERT
        order.Status.ShouldBe("QuantityValid");
        order.RequiresApproval.ShouldBeTrue();
        order.RequiresManagerApproval.ShouldBeTrue();
        order.DiscountPercent.ShouldBe(15);

        result.AppliedRules.Count().ShouldBe(3);
        result.AppliedRules.ShouldContain("Validate Quantity");
        result.AppliedRules.ShouldContain("Check High Amount");
        result.AppliedRules.ShouldContain("Apply Bulk Discount");
    }

    #endregion
}
