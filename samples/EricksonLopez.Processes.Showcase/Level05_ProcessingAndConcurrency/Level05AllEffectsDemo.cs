// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level05_ProcessingAndConcurrency;

// ---------------------------------------------------------------------------
// Domain model — Payment process with all effect variants
// ---------------------------------------------------------------------------

public sealed record PaymentState(
    string PaymentId,
    decimal Amount,
    string Status,
    bool RefundScheduled) : IProcessState;

public sealed record PaymentInitiatedEvent(Guid PaymentId, decimal Amount);
public sealed record PaymentExpiryTimeoutTrigger(Guid PaymentId);

// Compensation payload stored when a charge step succeeds
public sealed record ChargeCompensationPayload(Guid PaymentId, decimal Amount, string ChargeReference);

public sealed class PaymentProcess :
    IProcess<PaymentState>,
    IProcessHandler<PaymentState, PaymentInitiatedEvent>
{
    public ProcessType Type => ProcessType.From("payment.effects-showcase");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<PaymentState>> HandleAsync(
        PaymentState state,
        PaymentInitiatedEvent evt,
        ProcessContext context)
    {
        var updated = state with { Status = "Processing" };

        // Effect 1: Command — triggers a downstream service
        var commandEffect = ProcessEffect.CreateCommand(
            new { Action = "ChargeCard", PaymentId = evt.PaymentId, Amount = evt.Amount },
            commandType: "ChargeCardCommand");

        // Effect 2: Event — publishes a domain integration event
        var eventEffect = ProcessEffect.CreateEvent(
            new { EventName = "PaymentInitiated", PaymentId = evt.PaymentId },
            eventType: "PaymentInitiatedIntegrationEvent");

        // Effect 3: ScheduleTimeout — requests a deferred wake-up after 30 minutes
        var timeoutEffect = ProcessEffect.CreateTimeout(
            delay: TimeSpan.FromMinutes(30),
            trigger: new PaymentExpiryTimeoutTrigger(evt.PaymentId),
            triggerType: "PaymentExpiryTimeout");

        // Effect 4: Compensation — records a step for potential LIFO rollback
        var compensationPayload = new ChargeCompensationPayload(evt.PaymentId, evt.Amount, "CHG-DEMO-001");
        var compensationEffect = ProcessEffect.CreateCompensation<ChargeCompensationPayload>(
            "ChargeCard",
            compensationPayload);

        var step = CompensationStep.Create(
            "ChargeCard",
            compensationPayload,
            context.Now);

        return ValueTask.FromResult(ProcessTransitionResult<PaymentState>.Advance(
            updated,
            effects: [commandEffect, eventEffect, timeoutEffect, compensationEffect],
            recordedCompensations: [step]));
    }
}

public sealed class PaymentCorrelation : IProcessCorrelation<PaymentInitiatedEvent>
{
    public ProcessId ExtractProcessId(PaymentInitiatedEvent @event) => ProcessId.From(@event.PaymentId);
    public CorrelationId ExtractCorrelationId(PaymentInitiatedEvent @event) => CorrelationId.From(@event.PaymentId.ToString());
}

/// <summary>
/// Level 5-C: All ProcessEffect Variants and Typed Payload Extraction
/// Demonstrates every ProcessEffect subtype (Command, Event, ScheduleTimeout, Compensation)
/// and their strongly typed payload/trigger accessors: GetPayload&lt;T&gt;(), TryGetPayload&lt;T&gt;(),
/// GetTrigger&lt;T&gt;(), TryGetTrigger&lt;T&gt;(), CompensationStep/Action payload extraction.
/// </summary>
public static class Level05AllEffectsDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 05-C: ALL PROCESSEFFECT VARIANTS & TYPED PAYLOAD EXTRACTION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<PaymentState>();
        var coordinator = new ProcessCoordinator<PaymentState>(store);
        var process = new PaymentProcess();
        var correlation = new PaymentCorrelation();

        var paymentId = Guid.NewGuid();
        var evt = new PaymentInitiatedEvent(paymentId, 499.99m);

        var result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: evt,
            initialStateFactory: e => new PaymentState(e.PaymentId.ToString(), e.Amount, "Initialized", false),
            canInitiate: true);

        Console.WriteLine($"\nProcess '{process.Type}' executed. Effects count: {result.Effects.Count}");
        Console.WriteLine($"Recorded compensations count: {result.Instance.State.Status}");
        Console.WriteLine();

        // -----------------------------------------------------------------------
        // Inspect each emitted effect by type
        // -----------------------------------------------------------------------
        foreach (var effect in result.Effects)
        {
            switch (effect)
            {
                // ---- ProcessEffect.Command ----------------------------------------
                case ProcessEffect.Command cmd:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  [Command Effect]");
                        Console.ResetColor();
                        Console.WriteLine($"    CommandType:   {cmd.CommandType ?? "(null)"}");
                        Console.WriteLine($"    Payload type:  {cmd.CommandPayload.GetType().Name}");

                        // TryGetPayload<T> — safe extraction when type is known
                        if (cmd.TryGetPayload<object>(out var cmdPayload))
                        {
                            Console.WriteLine($"    TryGetPayload: OK → {cmdPayload?.GetType().Name}");
                        }

                        // TryGetPayload<string> — expected to fail (wrong type)
                        if (!cmd.TryGetPayload<string>(out _))
                        {
                            Console.WriteLine("    TryGetPayload<string>: returns false (type mismatch — expected)");
                        }

                        break;
                    }

                // ---- ProcessEffect.Event ------------------------------------------
                case ProcessEffect.Event domainEvt:
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("  [Event Effect]");
                        Console.ResetColor();
                        Console.WriteLine($"    EventType:    {domainEvt.EventType ?? "(null)"}");

                        // GetPayload<object> — direct cast (throws if wrong type)
                        var rawPayload = domainEvt.GetPayload<object>();
                        Console.WriteLine($"    GetPayload:   {rawPayload.GetType().Name}");

                        // TryGetPayload
                        if (domainEvt.TryGetPayload<object>(out var evtPayload))
                        {
                            Console.WriteLine($"    TryGetPayload: OK → {evtPayload?.GetType().Name}");
                        }

                        break;
                    }

                // ---- ProcessEffect.ScheduleTimeout --------------------------------
                case ProcessEffect.ScheduleTimeout timeout:
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("  [ScheduleTimeout Effect]");
                        Console.ResetColor();
                        Console.WriteLine($"    Delay:        {timeout.Delay.TotalMinutes:F0} minutes");
                        Console.WriteLine($"    TriggerType:  {timeout.TriggerType ?? "(null)"}");

                        // GetTrigger<T> — strongly typed access
                        var trigger = timeout.GetTrigger<PaymentExpiryTimeoutTrigger>();
                        Console.WriteLine($"    GetTrigger:   PaymentId = {trigger.PaymentId}");

                        // TryGetTrigger<T>
                        if (timeout.TryGetTrigger<PaymentExpiryTimeoutTrigger>(out var typedTrigger))
                        {
                            Console.WriteLine($"    TryGetTrigger: OK → PaymentId = {typedTrigger!.PaymentId}");
                        }

                        // TryGetTrigger with wrong type
                        if (!timeout.TryGetTrigger<string>(out _))
                        {
                            Console.WriteLine("    TryGetTrigger<string>: returns false (type mismatch — expected)");
                        }

                        break;
                    }

                // ---- ProcessEffect.Compensation -----------------------------------
                case ProcessEffect.Compensation comp:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("  [Compensation Effect]");
                        Console.ResetColor();
                        Console.WriteLine($"    StepName:     {comp.Action.StepName}");

                        // CompensationAction.ExtractPayload<T>
                        var payload = comp.Action.ExtractPayload<ChargeCompensationPayload>();
                        Console.WriteLine($"    ExtractPayload: PaymentId={payload.PaymentId}, Ref={payload.ChargeReference}");

                        // CompensationAction.TryExtractPayload<T>
                        if (comp.Action.TryExtractPayload<ChargeCompensationPayload>(out var typedPayload))
                        {
                            Console.WriteLine($"    TryExtractPayload: OK → Amount={typedPayload!.Amount}");
                        }

                        // TryExtractPayload with wrong type
                        if (!comp.Action.TryExtractPayload<string>(out _))
                        {
                            Console.WriteLine("    TryExtractPayload<string>: returns false (type mismatch — expected)");
                        }

                        break;
                    }
            }

            Console.WriteLine();
        }

        // -----------------------------------------------------------------------
        // Demonstrate CompensationStep APIs (from ProcessTransitionResult)
        // -----------------------------------------------------------------------
        Console.WriteLine("  [CompensationStep APIs — recorded during forward execution]");

        // Access recorded compensations directly from the store
        var storedInstance = await store.GetByIdAsync(ProcessId.From(paymentId));
        // Note: In production, RecordedCompensations would be stored within the state or a separate store.
        // Here we demonstrate the API via a manually constructed step:
        var demoStep = CompensationStep.Create(
            "ChargeCard",
            new ChargeCompensationPayload(paymentId, 499.99m, "CHG-DEMO-001"),
            DateTimeOffset.UtcNow);

        Console.WriteLine($"    StepName:           {demoStep.StepName}");
        Console.WriteLine($"    RecordedAt:         {demoStep.RecordedAt:O}");

        // CompensationStep.ExtractPayload<T>
        var stepPayload = demoStep.ExtractPayload<ChargeCompensationPayload>();
        Console.WriteLine($"    ExtractPayload:     PaymentId={stepPayload.PaymentId}");

        // CompensationStep.TryExtractPayload<T>
        if (demoStep.TryExtractPayload<ChargeCompensationPayload>(out var typedStepPayload))
        {
            Console.WriteLine($"    TryExtractPayload:  OK → ChargeRef={typedStepPayload!.ChargeReference}");
        }

        if (!demoStep.TryExtractPayload<int>(out _))
        {
            Console.WriteLine("    TryExtractPayload<int>: returns false (type mismatch — expected)");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 05-C All ProcessEffect Variants & Typed Extraction demo completed successfully.");
        Console.ResetColor();
    }
}
