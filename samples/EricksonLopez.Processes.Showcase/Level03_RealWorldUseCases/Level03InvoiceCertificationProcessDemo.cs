// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level03_RealWorldUseCases;

public sealed record InvoiceState(
    string InvoiceId,
    string IssuerTaxId,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt,
    string? CertificationCode,
    bool IsCertified) : IProcessState
{
    public static InvoiceState Initial(string invoiceId, string issuerTaxId, decimal totalAmount, DateTimeOffset now) =>
        new(invoiceId, issuerTaxId, totalAmount, now, null, false);
}

public sealed record SubmitInvoiceEvent(Guid InvoiceId, string IssuerTaxId, decimal TotalAmount);
public sealed record InvoiceCertifiedEvent(Guid InvoiceId, string CertificationCode);
public sealed record InvoiceExpiredEvent(Guid InvoiceId);

[ProcessDefinition("invoice.certification", 1)]
public sealed class InvoiceCertificationProcess :
    IProcess<InvoiceState>,
    IProcessHandler<InvoiceState, SubmitInvoiceEvent>,
    IProcessHandler<InvoiceState, InvoiceCertifiedEvent>,
    IProcessHandler<InvoiceState, InvoiceExpiredEvent>
{
    public ProcessType Type => ProcessType.From("invoice.certification");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        SubmitInvoiceEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"  [Step 1/2] SubmitInvoice: Invoice '{eventMessage.InvoiceId}' submitted. Emitting 48h timeout intent & suspending...");

        var updated = state with
        {
            InvoiceId = eventMessage.InvoiceId.ToString(),
            IssuerTaxId = eventMessage.IssuerTaxId,
            TotalAmount = eventMessage.TotalAmount,
            SubmittedAt = context.Now
        };

        var timeoutIntent = ProcessEffect.CreateTimeout(
            delay: TimeSpan.FromHours(48),
            trigger: new InvoiceExpiredEvent(eventMessage.InvoiceId));

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Suspend(
            updated,
            effects: [timeoutIntent]));
    }

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        InvoiceCertifiedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"  [Step 2/2] InvoiceCertified: Tax authority approved code '{eventMessage.CertificationCode}'. Completing process!");

        var updated = state with
        {
            CertificationCode = eventMessage.CertificationCode,
            IsCertified = true
        };

        var outboundEvent = ProcessEffect.CreateEvent(new
        {
            eventMessage.InvoiceId,
            eventMessage.CertificationCode,
            CertifiedAt = context.Now
        });

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Complete(
            updated,
            effects: [outboundEvent]));
    }

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        InvoiceExpiredEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"  [Timeout] InvoiceExpired: 48h elapsed without approval. Failing process.");

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Fail(
            state,
            "Timeout waiting for tax authority certification callback"));
    }
}

public sealed class SubmitInvoiceCorrelation : IProcessCorrelation<SubmitInvoiceEvent>
{
    public ProcessId ExtractProcessId(SubmitInvoiceEvent @event) => ProcessId.From(@event.InvoiceId);
    public CorrelationId ExtractCorrelationId(SubmitInvoiceEvent @event) => CorrelationId.From(@event.InvoiceId.ToString());
}

public sealed class InvoiceCertifiedCorrelation : IProcessCorrelation<InvoiceCertifiedEvent>
{
    public ProcessId ExtractProcessId(InvoiceCertifiedEvent @event) => ProcessId.From(@event.InvoiceId);
    public CorrelationId ExtractCorrelationId(InvoiceCertifiedEvent @event) => CorrelationId.From(@event.InvoiceId.ToString());
}

/// <summary>
/// Level 3B: Long-Running Process Manager with Timeout Intent &amp; External Callback
/// </summary>
public static class Level03InvoiceCertificationProcessDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 03-B: PROCESS MANAGER WITH TIMEOUT SUSPENSION & ASYNC CALLBACK");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<InvoiceState>();
        var coordinator = new ProcessCoordinator<InvoiceState>(store);
        var process = new InvoiceCertificationProcess();
        var invoiceId = Guid.NewGuid();

        // 1. Submit invoice and suspend with timeout intent
        var step1Result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new SubmitInvoiceCorrelation(),
            eventMessage: new SubmitInvoiceEvent(invoiceId, "TAX-US-991122", 15400.00m),
            initialStateFactory: e => InvoiceState.Initial(e.InvoiceId.ToString(), e.IssuerTaxId, e.TotalAmount, DateTimeOffset.UtcNow),
            canInitiate: true);

        Console.WriteLine($"Status after step 1: {step1Result.Instance.Status}");
        Console.WriteLine($"Emitted Effect:      {step1Result.Effects[0].GetType().Name}");

        // 2. Simulate external asynchronous callback received hours/days later
        var step2Result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new InvoiceCertifiedCorrelation(),
            eventMessage: new InvoiceCertifiedEvent(invoiceId, "GOV-CERT-2026-98765"),
            canInitiate: false);

        Console.WriteLine();
        Console.WriteLine($"Status after callback: {step2Result.Instance.Status}");
        Console.WriteLine($"Is Certified:          {step2Result.Instance.State.IsCertified}");
        Console.WriteLine($"Certification Code:    {step2Result.Instance.State.CertificationCode}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 03-B Invoice Certification Process completed successfully.");
        Console.ResetColor();
    }
}
