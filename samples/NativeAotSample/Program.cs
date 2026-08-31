// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.SystemTextJson;

namespace NativeAotSample;

public sealed record PaymentWorkflowState(
    string PaymentId,
    decimal Amount,
    string Status,
    bool IsCompleted) : IProcessState
{
    public static PaymentWorkflowState Initial(string paymentId, decimal amount) =>
        new(paymentId, amount, "Pending", false);
}

public sealed record AuthorizePaymentEvent(Guid PaymentId, decimal Amount);
public sealed record SettlePaymentEvent(Guid PaymentId);

[JsonSerializable(typeof(PaymentWorkflowState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(ProcessType))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(CausationId))]
[JsonSerializable(typeof(MessageId))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}

public sealed class PaymentProcess :
    IProcess<PaymentWorkflowState>,
    IProcessHandler<PaymentWorkflowState, AuthorizePaymentEvent>,
    IProcessHandler<PaymentWorkflowState, SettlePaymentEvent>
{
    public ProcessType Type => ProcessType.From("payment.workflow");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<PaymentWorkflowState>> HandleAsync(
        PaymentWorkflowState state,
        AuthorizePaymentEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { PaymentId = eventMessage.PaymentId.ToString(), Amount = eventMessage.Amount, Status = "Authorized" };
        var effect = new ProcessEffect.Command(new { Action = "HoldFunds", eventMessage.Amount });

        return ValueTask.FromResult(ProcessTransitionResult<PaymentWorkflowState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [effect]));
    }

    public ValueTask<ProcessTransitionResult<PaymentWorkflowState>> HandleAsync(
        PaymentWorkflowState state,
        SettlePaymentEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { Status = "Settled", IsCompleted = true };
        var effect = new ProcessEffect.Event(new { Action = "PaymentSettled", eventMessage.PaymentId });

        return ValueTask.FromResult(ProcessTransitionResult<PaymentWorkflowState>.Complete(
            updated,
            effects: [effect]));
    }
}

public sealed class AuthorizePaymentCorrelation : IProcessCorrelation<AuthorizePaymentEvent>
{
    public ProcessId ExtractProcessId(AuthorizePaymentEvent @event) => ProcessId.From(@event.PaymentId);
    public CorrelationId ExtractCorrelationId(AuthorizePaymentEvent @event) => CorrelationId.From(@event.PaymentId.ToString());
}

public sealed class SettlePaymentCorrelation : IProcessCorrelation<SettlePaymentEvent>
{
    public ProcessId ExtractProcessId(SettlePaymentEvent @event) => ProcessId.From(@event.PaymentId);
    public CorrelationId ExtractCorrelationId(SettlePaymentEvent @event) => CorrelationId.From(@event.PaymentId.ToString());
}



public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" EricksonLopez.Processes — Native AOT Standalone Sample  ");
        Console.WriteLine("=========================================================");

        var store = new EricksonLopez.Processes.Testing.InMemoryProcessStore<PaymentWorkflowState>();
        var coordinator = new ProcessCoordinator<PaymentWorkflowState>(store);
        var process = new PaymentProcess();
        var paymentId = Guid.NewGuid();

        // 1. Authorize payment
        var result1 = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new AuthorizePaymentCorrelation(),
            eventMessage: new AuthorizePaymentEvent(paymentId, 89.95m),
            initialStateFactory: e => PaymentWorkflowState.Initial(e.PaymentId.ToString(), e.Amount),
            canInitiate: true);

        Console.WriteLine($"Step 1 Status: {result1.Instance.Status}, State: {result1.Instance.State.Status}");

        // 2. Settle payment
        var result2 = await coordinator.ExecuteAsync(
            handler: process,
            correlation: new SettlePaymentCorrelation(),
            eventMessage: new SettlePaymentEvent(paymentId),
            canInitiate: false);

        Console.WriteLine($"Step 2 Status: {result2.Instance.Status}, State: {result2.Instance.State.Status}");

        // Test AOT serialization
        var serializer = new SystemTextJsonProcessStateSerializer<PaymentWorkflowState>(
            AppJsonContext.Default.PaymentWorkflowState);

        var bytes = serializer.Serialize(result2.Instance.State);
        var restored = serializer.Deserialize(bytes);

        Console.WriteLine($"AOT Serialized & Restored State PaymentId: {restored.PaymentId}, Settled: {restored.IsCompleted}");
        Console.WriteLine("Native AOT Execution Complete — 100% Zero Reflection.");
        Console.WriteLine("=========================================================");
    }
}





