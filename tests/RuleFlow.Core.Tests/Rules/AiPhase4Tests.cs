using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using Shouldly;

namespace RuleFlow.Core.Tests.Rules;

/// <summary>
/// Phase 4: AI condition resilience — timeout, failure strategy, caching, logging, metrics.
/// </summary>
public class AiPhase4Tests
{
    private sealed record Transaction(decimal Amount, string Supplier, string Country);
    private static readonly DefaultRuleContext Ctx = DefaultRuleContext.Instance;

    // ────────────────────────────────────────────────────────────────────────
    // Test doubles
    // ────────────────────────────────────────────────────────────────────────

    private sealed class StubAiEvaluator : IAiConditionEvaluator<Transaction>
    {
        private readonly bool _result;
        private readonly string? _reason;
        private readonly double? _confidence;
        public int CallCount { get; private set; }

        public StubAiEvaluator(bool result = true, string? reason = null, double? confidence = null)
        {
            _result = result;
            _reason = reason;
            _confidence = confidence;
        }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(new AiConditionResult
            {
                Result = _result,
                Reason = _reason,
                Confidence = _confidence
            });
        }
    }

    private sealed class ThrowingAiEvaluator : IAiConditionEvaluator<Transaction>
    {
        private readonly Exception _ex;
        public int CallCount { get; private set; }

        public ThrowingAiEvaluator(Exception? ex = null)
        {
            _ex = ex ?? new InvalidOperationException("AI service unavailable");
        }

        public Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            CallCount++;
            throw _ex;
        }
    }

    private sealed class DelayingAiEvaluator : IAiConditionEvaluator<Transaction>
    {
        private readonly TimeSpan _delay;
        private readonly bool _result;
        public int CallCount { get; private set; }

        public DelayingAiEvaluator(TimeSpan delay, bool result = true)
        {
            _delay = delay;
            _result = result;
        }

        public async Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            CallCount++;
            await Task.Delay(_delay, ct);
            return new AiConditionResult { Result = _result };
        }
    }

    private sealed class RecordingLogger : IAiExecutionLogger
    {
        public List<string> EvaluatingPrompts { get; } = [];
        public List<(string Prompt, AiConditionResult Result)> EvaluatedCalls { get; } = [];
        public List<(string Prompt, Exception? Ex)> FailureCalls { get; } = [];

        public void OnEvaluating(string prompt, object input) => EvaluatingPrompts.Add(prompt);
        public void OnEvaluated(string prompt, AiConditionResult result, TimeSpan duration)
            => EvaluatedCalls.Add((prompt, result));
        public void OnFailure(string prompt, Exception? ex) => FailureCalls.Add((prompt, ex));
    }

    private sealed class ThrowingLogger : IAiExecutionLogger
    {
        public void OnEvaluating(string prompt, object input) => throw new Exception("logger exploded");
        public void OnEvaluated(string prompt, AiConditionResult result, TimeSpan duration) => throw new Exception("logger exploded");
        public void OnFailure(string prompt, Exception? ex) => throw new Exception("logger exploded");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. Timeout tests
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeout_exceeded_triggers_failure_strategy_ReturnFalse()
    {
        var evaluator = new DelayingAiEvaluator(delay: TimeSpan.FromSeconds(5), result: true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(50),
            AiFailureStrategy = AiFailureStrategy.ReturnFalse
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeFalse();
    }

    [Fact]
    public async Task Timeout_exceeded_triggers_failure_strategy_ReturnTrue()
    {
        var evaluator = new DelayingAiEvaluator(delay: TimeSpan.FromSeconds(5), result: false);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(50),
            AiFailureStrategy = AiFailureStrategy.ReturnTrue
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeTrue();
    }

    [Fact]
    public async Task Timeout_does_not_throw()
    {
        var evaluator = new DelayingAiEvaluator(delay: TimeSpan.FromSeconds(5));
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(50),
        };

        var ex = await Record.ExceptionAsync(() => rule.EvaluateWithOptionsAsync(input, Ctx, options));

        ex.ShouldBeNull();
    }

    [Fact]
    public async Task No_timeout_configured_evaluator_completes_normally()
    {
        var evaluator = new StubAiEvaluator(result: true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            // No AiTimeout
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeTrue();
        evaluator.CallCount.ShouldBe(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. Failure strategy tests
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exception_in_evaluator_returns_false_with_ReturnFalse_strategy()
    {
        var evaluator = new ThrowingAiEvaluator();
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiFailureStrategy = AiFailureStrategy.ReturnFalse
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeFalse();
    }

    [Fact]
    public async Task Exception_in_evaluator_returns_true_with_ReturnTrue_strategy()
    {
        var evaluator = new ThrowingAiEvaluator();
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiFailureStrategy = AiFailureStrategy.ReturnTrue
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeTrue();
    }

    [Fact]
    public async Task Failure_does_not_throw_regardless_of_strategy()
    {
        foreach (var strategy in Enum.GetValues<AiFailureStrategy>())
        {
            var evaluator = new ThrowingAiEvaluator();
            var input = new Transaction(2000, "ACME", "US");

            var rule = Rule<Transaction>.For("Fraud")
                .WithAiEvaluator(evaluator)
                .WhenAI("Is this suspicious?");

            var options = new RuleExecutionOptions<Transaction>
            {
                EnableAiConditions = true,
                AiFailureStrategy = strategy
            };

            var ex = await Record.ExceptionAsync(() => rule.EvaluateWithOptionsAsync(input, Ctx, options));
            ex.ShouldBeNull($"Strategy {strategy} should not throw");
        }
    }

    [Fact]
    public async Task DefaultFailureStrategy_is_ReturnFalse()
    {
        var options = new RuleExecutionOptions<Transaction>();
        options.AiFailureStrategy.ShouldBe(AiFailureStrategy.ReturnFalse);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. Logging hook tests
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_OnEvaluating_called_before_ai_evaluator()
    {
        var log = new RecordingLogger();
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = log
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        log.EvaluatingPrompts.ShouldContain("Is this suspicious?");
    }

    [Fact]
    public async Task Logger_OnEvaluated_called_after_successful_evaluation()
    {
        var log = new RecordingLogger();
        var evaluator = new StubAiEvaluator(true, reason: "High amount");
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = log
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        log.EvaluatedCalls.ShouldHaveSingleItem();
        log.EvaluatedCalls[0].Prompt.ShouldBe("Is this suspicious?");
        log.EvaluatedCalls[0].Result.Reason.ShouldBe("High amount");
        log.FailureCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Logger_OnFailure_called_when_evaluator_throws()
    {
        var log = new RecordingLogger();
        var evaluator = new ThrowingAiEvaluator(new InvalidOperationException("AI down"));
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = log
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        log.FailureCalls.ShouldHaveSingleItem();
        log.FailureCalls[0].Prompt.ShouldBe("Is this suspicious?");
        log.FailureCalls[0].Ex.ShouldBeOfType<InvalidOperationException>();
        log.EvaluatedCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Logger_exceptions_are_suppressed_and_do_not_break_evaluation()
    {
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = new ThrowingLogger()
        };

        var ex = await Record.ExceptionAsync(() => rule.EvaluateWithOptionsAsync(input, Ctx, options));
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task No_logger_registered_evaluation_succeeds()
    {
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = null
        };

        var matched = await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        matched.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. Caching tests
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Caching_disabled_evaluator_called_each_time_per_rule()
    {
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        // Two WhenAI nodes with the same prompt — without caching, evaluator called twice
        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?")
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableAiCaching = false
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        evaluator.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Caching_enabled_same_prompt_and_input_evaluator_called_once()
    {
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        // Two WhenAI nodes with the same prompt — with caching, evaluator should be called once
        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?")
            .WhenAI("Is this suspicious?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableAiCaching = true
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        evaluator.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Caching_enabled_different_prompts_evaluator_called_separately()
    {
        var evaluator = new StubAiEvaluator(true);
        var input = new Transaction(2000, "ACME", "US");

        var rule = Rule<Transaction>.For("Fraud")
            .WithAiEvaluator(evaluator)
            .WhenAI("Is this suspicious?")
            .WhenAI("Is the amount unusual?");

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableAiCaching = true
        };

        await rule.EvaluateWithOptionsAsync(input, Ctx, options);

        evaluator.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Default_caching_is_disabled()
    {
        var options = new RuleExecutionOptions<Transaction>();
        options.EnableAiCaching.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. AI metrics tests (via engine + observability)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Metrics_ai_evaluations_incremented_on_successful_evaluation()
    {
        var evaluator = new StubAiEvaluator(true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics.ShouldNotBeNull();
        result.Metrics!.AiEvaluations.ShouldBe(1);
        result.Metrics!.AiFailures.ShouldBe(0);
    }

    [Fact]
    public async Task Metrics_ai_failures_incremented_on_exception()
    {
        var evaluator = new ThrowingAiEvaluator();
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics!.AiEvaluations.ShouldBe(1);
        result.Metrics!.AiFailures.ShouldBe(1);
    }

    [Fact]
    public async Task Metrics_ai_skipped_incremented_when_ai_disabled()
    {
        var evaluator = new StubAiEvaluator(true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = false, // AI disabled
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics!.AiSkipped.ShouldBe(1);
        result.Metrics!.AiEvaluations.ShouldBe(0);
        evaluator.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Metrics_multiple_ai_evaluations_accumulated()
    {
        var evaluator = new StubAiEvaluator(true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud 1")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            )
            .Add(
                Rule<Transaction>.For("Fraud 2")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is the amount high?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics!.AiEvaluations.ShouldBe(2);
    }

    [Fact]
    public async Task Metrics_zero_when_no_ai_conditions_in_ruleset()
    {
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Standard")
                    .When(x => x.Amount > 1000)
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics!.AiEvaluations.ShouldBe(0);
        result.Metrics!.AiFailures.ShouldBe(0);
        result.Metrics!.AiSkipped.ShouldBe(0);
    }

    [Fact]
    public async Task Metrics_ai_total_duration_populated()
    {
        var evaluator = new DelayingAiEvaluator(TimeSpan.FromMilliseconds(10), result: true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = true
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        result.Metrics!.AiTotalDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task Metrics_not_populated_when_observability_disabled()
    {
        var evaluator = new StubAiEvaluator(true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            EnableObservability = false
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        // Metrics should be null when observability is off
        result.Metrics.ShouldBeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. End-to-end execution via engine
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Engine_respects_AiTimeout_option()
    {
        var evaluator = new DelayingAiEvaluator(TimeSpan.FromSeconds(5), result: true);
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");
        var matched = false;

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { matched = true; })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(50),
            AiFailureStrategy = AiFailureStrategy.ReturnFalse
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        matched.ShouldBeFalse();
        result.Executions[0].Matched.ShouldBeFalse();
    }

    [Fact]
    public async Task Engine_respects_AiFailureStrategy_ReturnTrue()
    {
        var evaluator = new ThrowingAiEvaluator();
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");
        var matched = false;

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { matched = true; })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiFailureStrategy = AiFailureStrategy.ReturnTrue
        };

        var result = await engine.EvaluateAsync(input, ruleSet, options);

        matched.ShouldBeTrue();
        result.Executions[0].Matched.ShouldBeTrue();
    }

    [Fact]
    public async Task Engine_whole_pipeline_never_throws_on_ai_failure()
    {
        var evaluator = new ThrowingAiEvaluator();
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true
        };

        var ex = await Record.ExceptionAsync(() => engine.EvaluateAsync(input, ruleSet, options));
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task Engine_ai_logger_called_during_execution()
    {
        var evaluator = new StubAiEvaluator(true);
        var log = new RecordingLogger();
        var engine = new RuleEngine();
        var input = new Transaction(2000, "ACME", "US");

        var ruleSet = RuleSet.For<Transaction>("Test")
            .Add(
                Rule<Transaction>.For("Fraud")
                    .WithAiEvaluator(evaluator)
                    .WhenAI("Is this suspicious?")
                    .Then(_ => { })
            );

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiLogger = log
        };

        await engine.EvaluateAsync(input, ruleSet, options);

        log.EvaluatingPrompts.ShouldContain("Is this suspicious?");
        log.EvaluatedCalls.ShouldHaveSingleItem();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. AiFailureStrategy enum values
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AiFailureStrategy_has_ReturnFalse_and_ReturnTrue()
    {
        Enum.IsDefined(typeof(AiFailureStrategy), AiFailureStrategy.ReturnFalse).ShouldBeTrue();
        Enum.IsDefined(typeof(AiFailureStrategy), AiFailureStrategy.ReturnTrue).ShouldBeTrue();
    }

    [Fact]
    public void AiFailureStrategy_ReturnFalse_is_zero_default()
    {
        ((int)AiFailureStrategy.ReturnFalse).ShouldBe(0);
    }
}
