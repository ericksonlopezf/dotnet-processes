// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace OrderFulfillmentSample;

public sealed record OrderSagaState(
    string OrderId,
    string CustomerId,
    decimal Amount,
    bool PaymentCharged,
    bool InventoryReserved,
    bool ShipmentCreated,
    List<string> CompensationAuditLog) : IProcessState
{
    public static OrderSagaState Initial(string orderId, string customerId, decimal amount) =>
        new(orderId, customerId, amount, false, false, false, new List<string>());
}

public sealed record OrderCreatedEvent(Guid OrderId, string CustomerId, decimal Amount);
public sealed record PaymentCompletedEvent(Guid OrderId);
public sealed record InventoryReservedEvent(Guid OrderId);
public sealed record ShipmentFailedEvent(Guid OrderId, string Reason);

public sealed record ChargePaymentCommand(Guid OrderId, decimal Amount);
public sealed record ReserveInventoryCommand(Guid OrderId);
public sealed record CreateShipmentCommand(Guid OrderId);
public sealed record RefundPaymentCommand(Guid OrderId, decimal Amount);
public sealed record ReleaseInventoryCommand(Guid OrderId);

public sealed class OrderFulfillmentSaga :
    ISaga<OrderSagaState>,
    ICompensationHandler<OrderSagaState>,
    IProcessHandler<OrderSagaState, OrderCreatedEvent>,
    IProcessHandler<OrderSagaState, PaymentCompletedEvent>,
    IProcessHandler<OrderSagaState, InventoryReservedEvent>,
    IProcessHandler<OrderSagaState, ShipmentFailedEvent>
{
    public ProcessType Type => ProcessType.From("order.fulfillment");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<OrderSagaState>> HandleAsync(
        OrderSagaState state,
        OrderCreatedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[1/4] OrderCreated: Order '{eventMessage.OrderId}' created for amount ${eventMessage.Amount}. Requesting payment...");

        var updated = state with { OrderId = eventMessage.OrderId.ToString(), CustomerId = eventMessage.CustomerId, Amount = eventMessage.Amount };
        var effect = new ProcessEffect.Command(new ChargePaymentCommand(eventMessage.OrderId, eventMessage.Amount));

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [effect]));
    }

    public ValueTask<ProcessTransitionResult<OrderSagaState>> HandleAsync(
        OrderSagaState state,
        PaymentCompletedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[2/4] PaymentCompleted: Payment received. Requesting warehouse inventory reservation...");

        var updated = state with { PaymentCharged = true };
        var effect = new ProcessEffect.Command(new ReserveInventoryCommand(eventMessage.OrderId));
        var compensation = new CompensationStep("ChargePayment", new { Amount = state.Amount }, context.Now);

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [effect],
            recordedCompensations: [compensation]));
    }

    public ValueTask<ProcessTransitionResult<OrderSagaState>> HandleAsync(
        OrderSagaState state,
        InventoryReservedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[3/4] InventoryReserved: Stock reserved. Requesting courier shipment creation...");

        var updated = state with { InventoryReserved = true };
        var effect = new ProcessEffect.Command(new CreateShipmentCommand(eventMessage.OrderId));
        var compensation = new CompensationStep("ReserveInventory", new { OrderId = state.OrderId }, context.Now);

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [effect],
            recordedCompensations: [compensation]));
    }

    public ValueTask<ProcessTransitionResult<OrderSagaState>> HandleAsync(
        OrderSagaState state,
        ShipmentFailedEvent eventMessage,
        ProcessContext context)
    {
        Console.WriteLine($"[4/4] ShipmentFailed: Courier failed: '{eventMessage.Reason}'. Triggering reverse-order Saga Compensation!");

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Compensate(
            state,
            compensationActions: [
                new CompensationAction("ReserveInventory", new { OrderId = state.OrderId }),
                new CompensationAction("ChargePayment", new { Amount = state.Amount })
            ]));
    }

    public ValueTask<ProcessTransitionResult<OrderSagaState>> CompensateAsync(
        OrderSagaState state,
        CompensationAction action,
        ProcessContext context)
    {
        Console.WriteLine($"  <- Executing compensation step: '{action.StepName}'");
        state.CompensationAuditLog.Add(action.StepName);

        var updated = action.StepName switch
        {
            "ReserveInventory" => state with { InventoryReserved = false },
            "ChargePayment" => state with { PaymentCharged = false },
            _ => state
        };

        var effect = action.StepName switch
        {
            "ReserveInventory" => (ProcessEffect)new ProcessEffect.Command(new ReleaseInventoryCommand(Guid.Parse(state.OrderId))),
            "ChargePayment" => new ProcessEffect.Command(new RefundPaymentCommand(Guid.Parse(state.OrderId), state.Amount)),
            _ => new ProcessEffect.Event(new { Action = "UnknownCompensation" })
        };

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Advance(
            updated,
            ProcessStatus.Compensating,
            effects: [effect]));
    }
}



public sealed class OrderCreatedCorrelation : IProcessCorrelation<OrderCreatedEvent>
{
    public ProcessId ExtractProcessId(OrderCreatedEvent @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(OrderCreatedEvent @event) => CorrelationId.From(@event.OrderId.ToString());
}

public sealed class PaymentCompletedCorrelation : IProcessCorrelation<PaymentCompletedEvent>
{
    public ProcessId ExtractProcessId(PaymentCompletedEvent @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(PaymentCompletedEvent @event) => CorrelationId.From(@event.OrderId.ToString());
}

public sealed class InventoryReservedCorrelation : IProcessCorrelation<InventoryReservedEvent>
{
    public ProcessId ExtractProcessId(InventoryReservedEvent @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(InventoryReservedEvent @event) => CorrelationId.From(@event.OrderId.ToString());
}

public sealed class ShipmentFailedCorrelation : IProcessCorrelation<ShipmentFailedEvent>
{
    public ProcessId ExtractProcessId(ShipmentFailedEvent @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(ShipmentFailedEvent @event) => CorrelationId.From(@event.OrderId.ToString());
}

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" EricksonLopez.Processes — Order Fulfillment Saga Sample ");
        Console.WriteLine("=========================================================");

        var store = new EricksonLopez.Processes.Testing.InMemoryProcessStore<OrderSagaState>();
        var coordinator = new ProcessCoordinator<OrderSagaState>(store);
        var saga = new OrderFulfillmentSaga();
        var orderId = Guid.NewGuid();

        // 1. Order Created
        await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreatedEvent(orderId, "CUSTOMER-42", 349.99m),
            initialStateFactory: e => OrderSagaState.Initial(e.OrderId.ToString(), e.CustomerId, e.Amount),
            canInitiate: true);

        // 2. Payment Completed
        await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new PaymentCompletedCorrelation(),
            eventMessage: new PaymentCompletedEvent(orderId),
            canInitiate: false);

        // 3. Inventory Reserved
        await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new InventoryReservedCorrelation(),
            eventMessage: new InventoryReservedEvent(orderId),
            canInitiate: false);

        // 4. Shipment Failed -> Saga Compensation
        var failureResult = await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new ShipmentFailedCorrelation(),
            eventMessage: new ShipmentFailedEvent(orderId, "Local courier out of vehicles"),
            canInitiate: false);

        // Run recorded compensation steps in reverse LIFO order
        var compensationResult = await coordinator.CompensateAsync(
            processId: ProcessId.From(orderId),
            recordedSteps: [
                new CompensationStep("ChargePayment", new { Amount = 349.99m }, DateTimeOffset.UtcNow.AddMinutes(-5)),
                new CompensationStep("ReserveInventory", new { OrderId = orderId.ToString() }, DateTimeOffset.UtcNow.AddMinutes(-2))
            ],
            saga: saga);

        var finalInstance = compensationResult.Instance;

        Console.WriteLine();
        Console.WriteLine($"Saga Final Status: {finalInstance.Status}");
        Console.WriteLine($"Payment Charged: {finalInstance.State.PaymentCharged}");
        Console.WriteLine($"Inventory Reserved: {finalInstance.State.InventoryReserved}");
        Console.WriteLine($"Compensations Executed: [{string.Join(" -> ", finalInstance.State.CompensationAuditLog)}]");
        Console.WriteLine("=========================================================");
    }
}





