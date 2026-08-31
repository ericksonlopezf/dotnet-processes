// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level03_RealWorldUseCases;

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

[SagaDefinition("order.fulfillment", 1)]
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
        Console.WriteLine($"  [Step 1/4] OrderCreated: Order '{eventMessage.OrderId}' for ${eventMessage.Amount}. Requesting payment...");

        var updated = state with { OrderId = eventMessage.OrderId.ToString(), CustomerId = eventMessage.CustomerId, Amount = eventMessage.Amount };
        var effect = ProcessEffect.CreateCommand(new ChargePaymentCommand(eventMessage.OrderId, eventMessage.Amount));

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
        Console.WriteLine($"  [Step 2/4] PaymentCompleted: Payment received. Requesting inventory reservation...");

        var updated = state with { PaymentCharged = true };
        var effect = ProcessEffect.CreateCommand(new ReserveInventoryCommand(eventMessage.OrderId));
        var compensation = CompensationStep.Create("ChargePayment", new { Amount = state.Amount }, context.Now);

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
        Console.WriteLine($"  [Step 3/4] InventoryReserved: Inventory reserved. Requesting courier shipment creation...");

        var updated = state with { InventoryReserved = true };
        var effect = ProcessEffect.CreateCommand(new CreateShipmentCommand(eventMessage.OrderId));
        var compensation = CompensationStep.Create("ReserveInventory", new { OrderId = state.OrderId }, context.Now);

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
        Console.WriteLine($"  [Step 4/4] ShipmentFailed: Courier dispatch failed ('{eventMessage.Reason}'). Triggering Saga rollback!");

        return ValueTask.FromResult(ProcessTransitionResult<OrderSagaState>.Compensate(
            state,
            compensationActions: [
                CompensationAction.Create("ReserveInventory", new { OrderId = state.OrderId }),
                CompensationAction.Create("ChargePayment", new { Amount = state.Amount })
            ]));
    }

    public ValueTask<ProcessTransitionResult<OrderSagaState>> CompensateAsync(
        OrderSagaState state,
        CompensationAction action,
        ProcessContext context)
    {
        Console.WriteLine($"    <- [Compensate] Undoing step '{action.StepName}'...");
        state.CompensationAuditLog.Add(action.StepName);

        var updated = action.StepName switch
        {
            "ReserveInventory" => state with { InventoryReserved = false },
            "ChargePayment" => state with { PaymentCharged = false },
            _ => state
        };

        var effect = action.StepName switch
        {
            "ReserveInventory" => (ProcessEffect)ProcessEffect.CreateCommand(new ReleaseInventoryCommand(Guid.Parse(state.OrderId))),
            "ChargePayment" => ProcessEffect.CreateCommand(new RefundPaymentCommand(Guid.Parse(state.OrderId), state.Amount)),
            _ => ProcessEffect.CreateEvent(new { Action = "UnknownCompensation" })
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

/// <summary>
/// Level 3A: Order Fulfillment Saga with Reverse LIFO Compensation
/// </summary>
public static class Level03OrderFulfillmentSagaDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 03-A: SAGA ORCHESTRATION & REVERSE-ORDER (LIFO) COMPENSATION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<OrderSagaState>();
        var coordinator = new ProcessCoordinator<OrderSagaState>(store);
        var saga = new OrderFulfillmentSaga();
        var orderId = Guid.NewGuid();

        // 1. Order Created
        await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: new OrderCreatedEvent(orderId, "CUSTOMER-884", 259.99m),
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

        // 4. Shipment Failed
        await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new ShipmentFailedCorrelation(),
            eventMessage: new ShipmentFailedEvent(orderId, "Courier dispatch capacity exceeded"),
            canInitiate: false);

        // 5. Execute recorded compensation steps in reverse LIFO order
        var compensationResult = await coordinator.CompensateAsync(
            processId: ProcessId.From(orderId),
            recordedSteps: [
                CompensationStep.Create("ChargePayment", new { Amount = 259.99m }, DateTimeOffset.UtcNow.AddMinutes(-3)),
                CompensationStep.Create("ReserveInventory", new { OrderId = orderId.ToString() }, DateTimeOffset.UtcNow.AddMinutes(-1))
            ],
            saga: saga);

        var finalInstance = compensationResult.Instance;
        Console.WriteLine();
        Console.WriteLine($"Saga Final Status:        {finalInstance.Status}");
        Console.WriteLine($"Payment Still Charged:    {finalInstance.State.PaymentCharged}");
        Console.WriteLine($"Inventory Still Reserved: {finalInstance.State.InventoryReserved}");
        Console.WriteLine($"Compensations Executed:   [{string.Join(" -> ", finalInstance.State.CompensationAuditLog)}]");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 03-A Order Fulfillment Saga completed successfully.");
        Console.ResetColor();
    }
}
