using System.Diagnostics;
using System.Text.Json;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Debug;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Lightweight condition evaluator for trees built by the fluent API.
/// Handles <see cref="LambdaConditionNode{T}"/>, <see cref="AiConditionNode"/>,
/// and <see cref="ConditionGroup"/> nodes composed of the above.
/// Does not handle <see cref="ConditionLeaf"/> — use <see cref="ConditionEvaluator{T}"/>
/// for persistence-based structured conditions.
/// </summary>
internal sealed class FluentConditionEvaluator<T>
{
    private readonly IAiConditionEvaluator<T>? _aiEvaluator;
    private readonly bool _enableAiConditions;
    private readonly TimeSpan? _aiTimeout;
    private readonly AiFailureStrategy _aiFailureStrategy;
    private readonly bool _enableAiCaching;
    private readonly IAiExecutionLogger? _aiLogger;
    private readonly AiMetricsTracker? _metricsTracker;
    private readonly Dictionary<string, bool>? _cache;

    internal FluentConditionEvaluator(
        IAiConditionEvaluator<T>? aiEvaluator,
        bool enableAiConditions,
        TimeSpan? aiTimeout = null,
        AiFailureStrategy aiFailureStrategy = AiFailureStrategy.ReturnFalse,
        bool enableAiCaching = false,
        IAiExecutionLogger? aiLogger = null,
        AiMetricsTracker? metricsTracker = null)
    {
        _aiEvaluator = aiEvaluator;
        _enableAiConditions = enableAiConditions;
        _aiTimeout = aiTimeout;
        _aiFailureStrategy = aiFailureStrategy;
        _enableAiCaching = enableAiCaching;
        _aiLogger = aiLogger;
        _metricsTracker = metricsTracker;

        if (enableAiCaching)
            _cache = new Dictionary<string, bool>();
    }

    // ── Evaluation ────────────────────────────────────────────────────────────

    internal async Task<bool> EvaluateAsync(T input, ConditionNode node, CancellationToken ct = default)
    {
        return node switch
        {
            LambdaConditionNode<T> lambda => lambda.Evaluate(input),
            AiConditionNode ai            => await EvaluateAiAsync(input, ai, ct),
            ConditionGroup group          => await EvaluateGroupAsync(input, group, ct),
            _ => throw new InvalidOperationException($"Unsupported fluent condition node: {node.GetType().Name}")
        };
    }

    private async Task<bool> EvaluateAiAsync(T input, AiConditionNode ai, CancellationToken ct)
    {
        if (!_enableAiConditions || _aiEvaluator == null)
        {
            _metricsTracker?.RecordSkipped();
            return false;
        }

        object effectiveInput = ai.InputSelector != null
            ? ai.InputSelector(input!)
            : (object)input!;

        string? cacheKey = null;
        if (_cache != null)
        {
            cacheKey = ComputeCacheKey(ai.Prompt, effectiveInput);
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        SafeInvokeLogger(() => _aiLogger?.OnEvaluating(ai.Prompt, effectiveInput));

        CancellationTokenSource? timeoutCts = null;
        CancellationToken effectiveCt = ct;

        if (_aiTimeout.HasValue)
        {
            timeoutCts = ct.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : new CancellationTokenSource();
            timeoutCts.CancelAfter(_aiTimeout.Value);
            effectiveCt = timeoutCts.Token;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            AiConditionResult aiResult;
            try
            {
                aiResult = await _aiEvaluator.EvaluateAsync(ai.Prompt, input, effectiveCt);
            }
            finally
            {
                timeoutCts?.Dispose();
            }

            sw.Stop();
            var elapsed = sw.Elapsed;
            _metricsTracker?.RecordEvaluation(elapsed);
            SafeInvokeLogger(() => _aiLogger?.OnEvaluated(ai.Prompt, aiResult, elapsed));

            if (cacheKey != null)
                _cache![cacheKey] = aiResult.Result;

            return aiResult.Result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var elapsed = sw.Elapsed;
            _metricsTracker?.RecordFailure(elapsed);

            var logEx = ex is OperationCanceledException && _aiTimeout.HasValue && !ct.IsCancellationRequested
                ? null
                : ex;
            SafeInvokeLogger(() => _aiLogger?.OnFailure(ai.Prompt, logEx));

            return _aiFailureStrategy == AiFailureStrategy.ReturnTrue;
        }
    }

    private async Task<bool> EvaluateGroupAsync(T input, ConditionGroup group, CancellationToken ct)
    {
        var isAnd = group.Operator.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase);

        if (isAnd)
        {
            foreach (var child in group.Conditions)
            {
                if (!await EvaluateAsync(input, child, ct))
                    return false;
            }
            return true;
        }

        foreach (var child in group.Conditions)
        {
            if (await EvaluateAsync(input, child, ct))
                return true;
        }
        return false;
    }

    // ── Debug evaluation (non-short-circuiting) ───────────────────────────────

    internal async Task<(bool Result, DebugConditionNode Tree)> EvaluateWithDebugAsync(
        T input, ConditionNode node, CancellationToken ct = default)
    {
        return node switch
        {
            LambdaConditionNode<T> lambda => EvaluateLambdaWithDebug(input, lambda),
            AiConditionNode ai            => await EvaluateAiWithDebugAsync(input, ai, ct),
            ConditionGroup group          => await EvaluateGroupWithDebugAsync(input, group, ct),
            _ => throw new InvalidOperationException($"Unsupported fluent condition node: {node.GetType().Name}")
        };
    }

    private static (bool Result, DebugConditionNode Tree) EvaluateLambdaWithDebug(
        T input, LambdaConditionNode<T> lambda)
    {
        var result = lambda.Evaluate(input);
        return (result, new DebugLambdaConditionLeaf { Result = result });
    }

    private async Task<(bool Result, DebugConditionNode Tree)> EvaluateAiWithDebugAsync(
        T input, AiConditionNode ai, CancellationToken ct)
    {
        if (!_enableAiConditions || _aiEvaluator == null)
        {
            _metricsTracker?.RecordSkipped();
            return (false, new DebugAiConditionLeaf
            {
                Result = false,
                AiPrompt = ai.Prompt,
                AiEvaluated = false
            });
        }

        object effectiveInput = ai.InputSelector != null
            ? ai.InputSelector(input!)
            : (object)input!;

        string? cacheKey = null;
        if (_cache != null)
        {
            cacheKey = ComputeCacheKey(ai.Prompt, effectiveInput);
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return (cached, new DebugAiConditionLeaf
                {
                    Result = cached,
                    AiPrompt = ai.Prompt,
                    AiEvaluated = true
                });
            }
        }

        SafeInvokeLogger(() => _aiLogger?.OnEvaluating(ai.Prompt, effectiveInput));

        CancellationTokenSource? timeoutCts = null;
        CancellationToken effectiveCt = ct;

        if (_aiTimeout.HasValue)
        {
            timeoutCts = ct.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : new CancellationTokenSource();
            timeoutCts.CancelAfter(_aiTimeout.Value);
            effectiveCt = timeoutCts.Token;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            AiConditionResult aiResult;
            try
            {
                aiResult = await _aiEvaluator.EvaluateAsync(ai.Prompt, input, effectiveCt);
            }
            finally
            {
                timeoutCts?.Dispose();
            }

            sw.Stop();
            var elapsed = sw.Elapsed;
            _metricsTracker?.RecordEvaluation(elapsed);
            SafeInvokeLogger(() => _aiLogger?.OnEvaluated(ai.Prompt, aiResult, elapsed));

            if (cacheKey != null)
                _cache![cacheKey] = aiResult.Result;

            return (aiResult.Result, new DebugAiConditionLeaf
            {
                Result = aiResult.Result,
                AiPrompt = ai.Prompt,
                AiEvaluated = true,
                AiReason = aiResult.Reason,
                AiConfidence = aiResult.Confidence
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            var elapsed = sw.Elapsed;
            _metricsTracker?.RecordFailure(elapsed);

            var logEx = ex is OperationCanceledException && _aiTimeout.HasValue && !ct.IsCancellationRequested
                ? null
                : ex;
            SafeInvokeLogger(() => _aiLogger?.OnFailure(ai.Prompt, logEx));

            var fallback = _aiFailureStrategy == AiFailureStrategy.ReturnTrue;
            return (fallback, new DebugAiConditionLeaf
            {
                Result = fallback,
                AiPrompt = ai.Prompt,
                AiEvaluated = true,
                AiFailed = true
            });
        }
    }

    private static string ComputeCacheKey(string prompt, object? input)
    {
        string serialized;
        try
        {
            serialized = JsonSerializer.Serialize(input);
        }
        catch
        {
            serialized = input?.ToString() ?? string.Empty;
        }
        return $"{prompt}::{serialized}";
    }

    private static void SafeInvokeLogger(Action logAction)
    {
        try { logAction(); }
        catch { }
    }

    private async Task<(bool Result, DebugConditionNode Tree)> EvaluateGroupWithDebugAsync(
        T input, ConditionGroup group, CancellationToken ct)
    {
        var isAnd = group.Operator.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase);
        var children = new List<DebugConditionNode>();
        bool groupResult = isAnd; // AND starts true, OR starts false

        // Evaluate ALL children — no short-circuit — for full debug coverage.
        foreach (var child in group.Conditions)
        {
            var (childResult, childTree) = await EvaluateWithDebugAsync(input, child, ct);
            children.Add(childTree);
            groupResult = isAnd
                ? groupResult && childResult
                : groupResult || childResult;
        }

        return (groupResult, new DebugConditionGroup
        {
            Result = groupResult,
            Operator = group.Operator,
            Children = children
        });
    }
}
