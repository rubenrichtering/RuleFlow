using RuleFlow.Abstractions.Results;
using Shouldly;

namespace RuleFlow.Core.Tests.Helpers;

/// <summary>
/// Extension methods for RuleResult to provide readable, intent-clear assertions.
/// These helpers reduce test verbosity while maintaining explicit, understandable behavior.
/// </summary>
public static class RuleResultAssertions
{
    /// <summary>
    /// Asserts that a rule matched (was executed and condition evaluated to true).
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="ruleName">The name of the rule that should have matched</param>
    public static void ShouldHaveMatched(this RuleResult result, string ruleName)
    {
        result.AppliedRules.ShouldContain(ruleName);
    }

    /// <summary>
    /// Asserts that a rule did not match (was executed but condition evaluated to false, or was not executed).
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="ruleName">The name of the rule that should not have matched</param>
    public static void ShouldNotHaveMatched(this RuleResult result, string ruleName)
    {
        result.AppliedRules.ShouldNotContain(ruleName);
    }

    /// <summary>
    /// Asserts that a rule was executed (regardless of whether it matched).
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="ruleName">The name of the rule that should have been executed</param>
    public static void ShouldHaveExecuted(this RuleResult result, string ruleName)
    {
        result.Executions.Any(e => e.RuleName == ruleName).ShouldBeTrue(
            $"Rule '{ruleName}' was expected to be executed but was not.");
    }

    /// <summary>
    /// Asserts that a rule was not executed.
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="ruleName">The name of the rule that should not have been executed</param>
    public static void ShouldNotHaveExecuted(this RuleResult result, string ruleName)
    {
        result.Executions.Any(e => e.RuleName == ruleName).ShouldBeFalse(
            $"Rule '{ruleName}' was not expected to be executed but was.");
    }

    /// <summary>
    /// Asserts the expected count of rules that matched.
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="expectedCount">The expected number of matched rules</param>
    public static void ShouldHaveMatchedRules(this RuleResult result, int expectedCount)
    {
        result.AppliedRules.Count().ShouldBe(expectedCount);
    }

    /// <summary>
    /// Asserts the total count of rules executed.
    /// </summary>
    /// <param name="result">The RuleResult to check</param>
    /// <param name="expectedCount">The expected number of executed rules</param>
    public static void ShouldHaveExecutedRules(this RuleResult result, int expectedCount)
    {
        result.Executions.Count().ShouldBe(expectedCount);
    }
}
