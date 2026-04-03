using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates AI-powered condition support with resilience, observability, and logging.
/// Covers: fraud detection, invoice validation, support ticket classification, vendor risk assessment.
/// Uses a stub AI evaluator to simulate real AI responses without requiring an actual AI service.
/// </summary>
public class AiConditionsScenario : IScenario
{
    public string Name => "AI Conditions (Phase 4)";
    public string Description => "AI-powered rules with timeout, failure strategy, logging, caching, and observability";

    public async Task Run()
    {
        await RunFraudDetection();
        await RunInvoiceValidation();
        await RunSupportTicketClassification();
        await RunVendorRiskAssessment();
        await RunResilienceDemo();
    }

    // ── Models ────────────────────────────────────────────────────────────────

    private sealed class Transaction
    {
        public decimal Amount { get; set; }
        public string Supplier { get; set; } = "";
        public string Country { get; set; } = "";
        public bool IsFlagged { get; set; }
    }

    private sealed class Invoice
    {
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public bool RequiresManualReview { get; set; }
    }

    private sealed class SupportTicket
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "Normal";
    }

    private sealed class Vendor
    {
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public bool IsHighRisk { get; set; }
    }

    // ── Stub AI evaluators ────────────────────────────────────────────────────

    private sealed class FraudAiEvaluator : IAiConditionEvaluator<Transaction>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            // Simulated AI: flag if high amount + unknown supplier
            var suspicious = input.Amount > 5000 || input.Country is "NG" or "RU";
            return Task.FromResult(new AiConditionResult
            {
                Result = suspicious,
                Reason = suspicious
                    ? $"High-risk transaction: amount={input.Amount}, country={input.Country}"
                    : "Transaction appears normal",
                Confidence = suspicious ? 0.87 : 0.12
            });
        }
    }

    private sealed class InvoiceAiEvaluator : IAiConditionEvaluator<Invoice>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, Invoice input, CancellationToken ct)
        {
            var unusual = input.Amount > 10000 || input.Description.Contains("unspecified", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(new AiConditionResult
            {
                Result = unusual,
                Reason = unusual ? "Invoice description is vague or amount is unusually high" : "Invoice looks normal",
                Confidence = unusual ? 0.75 : 0.95
            });
        }
    }

    private sealed class SupportAiEvaluator : IAiConditionEvaluator<SupportTicket>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, SupportTicket input, CancellationToken ct)
        {
            var urgent = input.Title.Contains("down", StringComparison.OrdinalIgnoreCase)
                      || input.Description.Contains("urgent", StringComparison.OrdinalIgnoreCase)
                      || input.Description.Contains("critical", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(new AiConditionResult
            {
                Result = urgent,
                Reason = urgent ? "Keywords suggest urgency" : "No urgency indicators detected",
                Confidence = urgent ? 0.91 : 0.82
            });
        }
    }

    private sealed class VendorAiEvaluator : IAiConditionEvaluator<Vendor>
    {
        public Task<AiConditionResult> EvaluateAsync(string prompt, Vendor input, CancellationToken ct)
        {
            var risky = input.Country is "KP" or "IR" or "BY";
            return Task.FromResult(new AiConditionResult
            {
                Result = risky,
                Reason = risky ? $"Vendor located in high-risk jurisdiction: {input.Country}" : "Vendor country appears acceptable",
                Confidence = risky ? 0.95 : 0.60
            });
        }
    }

    private sealed class TimeoutAiEvaluator : IAiConditionEvaluator<Transaction>
    {
        public async Task<AiConditionResult> EvaluateAsync(string prompt, Transaction input, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct); // Always times out
            return new AiConditionResult { Result = true };
        }
    }

    private sealed class ConsoleAiLogger : IAiExecutionLogger
    {
        public void OnEvaluating(string prompt, object input)
            => Console.WriteLine($"  [AI →] Evaluating: \"{prompt}\"");

        public void OnEvaluated(string prompt, AiConditionResult result, TimeSpan duration)
            => Console.WriteLine($"  [AI ✅] Result={result.Result} | Confidence={result.Confidence:P0} | Reason=\"{result.Reason}\" | {duration.TotalMilliseconds:F0}ms");

        public void OnFailure(string prompt, Exception? ex)
            => Console.WriteLine($"  [AI ❌] Failure for \"{prompt}\": {ex?.Message ?? "timeout"}");
    }

    // ── Scenarios ─────────────────────────────────────────────────────────────

    private static async Task RunFraudDetection()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("📌 Scenario 1: Fraud Detection");
        Console.WriteLine("   Combines deterministic + AI conditions");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var engine = new RuleEngine();

        var ruleSet = RuleSet.For<Transaction>("FraudDetection")
            .Add(Rule.For<Transaction>("High-Value Fraud Check")
                .WithAiEvaluator(new FraudAiEvaluator())
                .When(t => t.Amount > 1000)               // Deterministic: amount threshold
                .WhenAI("Is this transaction suspicious?", t => new { t.Amount, t.Supplier, t.Country })
                .Then(t => t.IsFlagged = true)
                .Because("Transaction is high-value AND AI flagged as suspicious"));

        var options = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromSeconds(5),
            AiFailureStrategy = AiFailureStrategy.ReturnFalse,
            AiLogger = new ConsoleAiLogger(),
            EnableObservability = true,
        };

        var normalTx = new Transaction { Amount = 500, Supplier = "ACME Corp", Country = "US" };
        var suspiciousTx = new Transaction { Amount = 12000, Supplier = "Unknown LLC", Country = "NG" };

        Console.WriteLine("\n→ Normal transaction ($500, US):");
        var r1 = await engine.EvaluateAsync(normalTx, ruleSet, options);
        Console.WriteLine($"  Flagged: {normalTx.IsFlagged} | Matched: {r1.Executions[0].Matched}");

        Console.WriteLine("\n→ Suspicious transaction ($12,000, NG):");
        var r2 = await engine.EvaluateAsync(suspiciousTx, ruleSet, options);
        Console.WriteLine($"  Flagged: {suspiciousTx.IsFlagged} | Matched: {r2.Executions[0].Matched}");

        if (r2.Metrics != null)
        {
            Console.WriteLine($"\n  AI Metrics: Evaluations={r2.Metrics.AiEvaluations}, Failures={r2.Metrics.AiFailures}");
        }

        Console.WriteLine("\n  Debug output:");
        Console.WriteLine(r2.ToDebugString());
    }

    private static async Task RunInvoiceValidation()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("📌 Scenario 2: Invoice Validation");
        Console.WriteLine("   AI identifies unusual invoices for review");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var engine = new RuleEngine();

        var ruleSet = RuleSet.For<Invoice>("InvoiceReview")
            .Add(Rule.For<Invoice>("Unusual Invoice Check")
                .WithAiEvaluator(new InvoiceAiEvaluator())
                .WhenAI("Does this invoice look unusual or suspicious?", i => new { i.Description, i.Amount })
                .Then(i => i.RequiresManualReview = true)
                .Because("AI detected unusual pattern in invoice"));

        var options = new RuleExecutionOptions<Invoice>
        {
            EnableAiConditions = true,
            AiLogger = new ConsoleAiLogger(),
        };

        var normalInvoice = new Invoice { Description = "Office supplies - Q1", Amount = 349.99m };
        var unusualInvoice = new Invoice { Description = "Unspecified services", Amount = 45000m };

        Console.WriteLine("\n→ Normal invoice ($349.99):");
        await engine.EvaluateAsync(normalInvoice, ruleSet, options);
        Console.WriteLine($"  RequiresManualReview: {normalInvoice.RequiresManualReview}");

        Console.WriteLine("\n→ Unusual invoice ($45,000 - vague description):");
        var result = await engine.EvaluateAsync(unusualInvoice, ruleSet, options);
        Console.WriteLine($"  RequiresManualReview: {unusualInvoice.RequiresManualReview}");
        Console.WriteLine("\n  Debug output:");
        Console.WriteLine(result.ToDebugString());
    }

    private static async Task RunSupportTicketClassification()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("📌 Scenario 3: Support Ticket Classification");
        Console.WriteLine("   AI classifies urgency for routing");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var engine = new RuleEngine();

        var ruleSet = RuleSet.For<SupportTicket>("TicketRouting")
            .Add(Rule.For<SupportTicket>("Urgent Ticket Detection")
                .WithAiEvaluator(new SupportAiEvaluator())
                .WhenAI("Is this an urgent support request needing immediate attention?",
                    t => new { t.Title, t.Description })
                .Then(t => t.Priority = "Critical")
                .Because("AI classified as urgent"));

        var options = new RuleExecutionOptions<SupportTicket>
        {
            EnableAiConditions = true,
            AiLogger = new ConsoleAiLogger(),
        };

        var routine = new SupportTicket { Title = "Update billing address", Description = "I need to change my billing address." };
        var urgent = new SupportTicket { Title = "Production database DOWN", Description = "Critical: primary DB is down, all users affected. Urgent fix needed!" };

        Console.WriteLine("\n→ Routine ticket:");
        await engine.EvaluateAsync(routine, ruleSet, options);
        Console.WriteLine($"  Priority: {routine.Priority}");

        Console.WriteLine("\n→ Urgent ticket:");
        await engine.EvaluateAsync(urgent, ruleSet, options);
        Console.WriteLine($"  Priority: {urgent.Priority}");
    }

    private static async Task RunVendorRiskAssessment()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("📌 Scenario 4: Vendor Risk Assessment");
        Console.WriteLine("   AI evaluates geopolitical risk of supplier");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var engine = new RuleEngine();

        var ruleSet = RuleSet.For<Vendor>("VendorRisk")
            .Add(Rule.For<Vendor>("High-Risk Vendor")
                .WithAiEvaluator(new VendorAiEvaluator())
                .WhenAI("Is this supplier located in a high-risk jurisdiction?",
                    v => new { v.Name, v.Country })
                .Then(v => v.IsHighRisk = true)
                .Because("AI identified vendor as high-risk"));

        var options = new RuleExecutionOptions<Vendor>
        {
            EnableAiConditions = true,
            AiLogger = new ConsoleAiLogger(),
            EnableAiCaching = true,  // Cache: same vendor/prompt = same AI call
        };

        var safe = new Vendor { Name = "Reliable Parts GmbH", Country = "DE" };
        var risky = new Vendor { Name = "Compact Electronics", Country = "KP" };

        Console.WriteLine("\n→ German vendor:");
        await engine.EvaluateAsync(safe, ruleSet, options);
        Console.WriteLine($"  IsHighRisk: {safe.IsHighRisk}");

        Console.WriteLine("\n→ High-risk jurisdiction vendor:");
        await engine.EvaluateAsync(risky, ruleSet, options);
        Console.WriteLine($"  IsHighRisk: {risky.IsHighRisk}");
    }

    private static async Task RunResilienceDemo()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("📌 Scenario 5: Resilience — Timeout + Failure Strategy");
        Console.WriteLine("   AI times out; pipeline continues safely");
        Console.WriteLine("═══════════════════════════════════════════════════════");

        var engine = new RuleEngine();
        var tx = new Transaction { Amount = 8000, Supplier = "Slow AI Corp", Country = "US" };
        var matched = false;

        var ruleSet = RuleSet.For<Transaction>("ResilienceTest")
            .Add(Rule.For<Transaction>("AI With Timeout")
                .WithAiEvaluator(new TimeoutAiEvaluator())
                .WhenAI("Is this suspicious?")
                .Then(t => { matched = true; })
                .Because("AI flagged as suspicious"));

        Console.WriteLine("\n→ ReturnFalse strategy (AI times out → condition = false):");
        var optFalse = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(100),
            AiFailureStrategy = AiFailureStrategy.ReturnFalse,
            AiLogger = new ConsoleAiLogger(),
        };
        matched = false;
        await engine.EvaluateAsync(tx, ruleSet, optFalse);
        Console.WriteLine($"  Matched: {matched} (expected: false — timeout → ReturnFalse)");

        Console.WriteLine("\n→ ReturnTrue strategy (AI times out → condition = true):");
        var optTrue = new RuleExecutionOptions<Transaction>
        {
            EnableAiConditions = true,
            AiTimeout = TimeSpan.FromMilliseconds(100),
            AiFailureStrategy = AiFailureStrategy.ReturnTrue,
            AiLogger = new ConsoleAiLogger(),
        };
        matched = false;
        await engine.EvaluateAsync(tx, ruleSet, optTrue);
        Console.WriteLine($"  Matched: {matched} (expected: true — timeout → ReturnTrue)");

        Console.WriteLine("\n⚠  AI is advisory — always combine with deterministic rules in critical systems.");
    }
}
