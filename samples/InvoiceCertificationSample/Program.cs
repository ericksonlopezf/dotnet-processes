// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace InvoiceCertificationSample;

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

public sealed record CreateInvoiceEvent(Guid InvoiceId, string IssuerTaxId, decimal TotalAmount);
public sealed record InvoiceCertifiedEvent(Guid InvoiceId, string CertificationCode);
public sealed record InvoiceExpiredEvent(Guid InvoiceId);

public sealed class InvoiceCertificationProcess :
    IProcess<InvoiceState>,
    IProcessHandler<InvoiceState, CreateInvoiceEvent>,
    IProcessHandler<InvoiceState, InvoiceCertifiedEvent>,
    IProcessHandler<InvoiceState, InvoiceExpiredEvent>
{
    public ProcessType Type => ProcessType.From("invoice.certification");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        CreateInvoiceEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[1/3] CreateInvoice: Invoice '{eventMessage.InvoiceId}' submitted. Emitting 48h timeout intent and suspending process...");

        var updated = state with
        {
            InvoiceId = eventMessage.InvoiceId.ToString(),
            IssuerTaxId = eventMessage.IssuerTaxId,
            TotalAmount = eventMessage.TotalAmount,
            SubmittedAt = context.Now
        };

        var timeoutIntent = new ProcessEffect.ScheduleTimeout(
            Delay: TimeSpan.FromHours(48),
            TimeoutTrigger: new InvoiceExpiredEvent(eventMessage.InvoiceId));

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Suspend(
            updated,
            effects: [timeoutIntent]));
    }

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        InvoiceCertifiedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[2/3] InvoiceCertified: Tax authority approved certification code '{eventMessage.CertificationCode}'. Completing process!");

        var updated = state with
        {
            CertificationCode = eventMessage.CertificationCode,
            IsCertified = true
        };

        var outboundEvent = new ProcessEffect.Event(new { eventMessage.InvoiceId, eventMessage.CertificationCode, CertifiedAt = context.Now });

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Complete(
            updated,
            effects: [outboundEvent]));
    }

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state,
        InvoiceExpiredEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[!] InvoiceExpired: 48-hour timeout reached without certification callback. Transitioning to Failed.");

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Fail(
            state,
            "Timeout waiting for tax authority certification callback"));
    }
}



public sealed class CreateInvoiceCorrelation : IProcessCorrelation<CreateInvoiceEvent>
{
    public ProcessId ExtractProcessId(CreateInvoiceEvent @event) => ProcessId.From(@event.InvoiceId);
    public CorrelationId ExtractCorrelationId(CreateInvoiceEvent @event) => CorrelationId.From(@event.InvoiceId.ToString());
}

public sealed class InvoiceCertifiedCorrelation : IProcessCorrelation<InvoiceCertifiedEvent>
{
    public ProcessId ExtractProcessId(InvoiceCertifiedEvent @event) => ProcessId.From(@event.InvoiceId);
    public CorrelationId ExtractCorrelationId(InvoiceCertifiedEvent @event) => CorrelationId.From(@event.InvoiceId.ToString());
}

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" EricksonLopez.Processes — Invoice Certification Sample  ");
        Console.WriteLine("=========================================================");

        var store = new EricksonLopez.Processes.Testing.InMemoryProcessStore<InvoiceState>();
        var coordinator = new ProcessCoordinator<InvoiceState>(store);
        var process = new InvoiceCertificationProcess();
        var invoiceId = Guid.NewGuid();

        // 1. Submit invoice and suspend with timeout intent
        var creationResult = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new CreateInvoiceCorrelation(),
            eventMessage: new CreateInvoiceEvent(invoiceId, "TAX-998877", 12500.00m),
            initialStateFactory: e => InvoiceState.Initial(e.InvoiceId.ToString(), e.IssuerTaxId, e.TotalAmount, DateTimeOffset.UtcNow),
            canInitiate: true);

        Console.WriteLine($"Process Status after creation: {creationResult.Instance.Status}");
        Console.WriteLine($"Emitted Effects: {creationResult.Effects.Count} (Type: {creationResult.Effects[0].GetType().Name})");

        // 2. External callback received from Tax Authority
        var certificationResult = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new InvoiceCertifiedCorrelation(),
            eventMessage: new InvoiceCertifiedEvent(invoiceId, "CERT-2026-XYZ-999"),
            canInitiate: false);

        Console.WriteLine();
        Console.WriteLine($"Process Status after callback: {certificationResult.Instance.Status}");
        Console.WriteLine($"Is Certified: {certificationResult.Instance.State.IsCertified}");
        Console.WriteLine($"Certification Code: {certificationResult.Instance.State.CertificationCode}");
        Console.WriteLine("=========================================================");
    }
}





