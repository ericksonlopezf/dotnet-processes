# Level 01: State Machine & Saga Orchestration

## 1. Defining a Durable State Machine
`EricksonLopez.Processes` models state machine states, events, and transitions through strongly typed domain records:

```csharp
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

public enum OrderState { Created, PaymentPending, InventoryReserved, Completed, Failed }

public sealed record OrderProcessState : ProcessState<OrderState>
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed class OrderFulfillmentProcess : ProcessDefinition<OrderProcessState, OrderState>
{
    public OrderFulfillmentProcess()
    {
        Configure(OrderState.Created)
            .Permit<PaymentReceivedEvent>(OrderState.PaymentPending, OnPaymentReceived)
            .Permit<CancelOrderCommand>(OrderState.Failed, OnOrderCancelled);

        Configure(OrderState.PaymentPending)
            .Permit<InventoryAllocatedEvent>(OrderState.Completed, OnCompleted)
            .Permit<InventoryExhaustedEvent>(OrderState.Failed, OnInventoryFailed);
    }

    private ValueTask OnPaymentReceived(OrderProcessState state, PaymentReceivedEvent evt, CancellationToken ct)
    {
        // Emit domain command to inventory service
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnInventoryFailed(OrderProcessState state, InventoryExhaustedEvent evt, CancellationToken ct)
    {
        // Execute saga compensation: refund payment
        await RefundPaymentAsync(state.OrderId, state.TotalAmount, ct);
    }
}
```

---

## 2. Deterministic State Transitions
Transitions are transactional and atomic. In case of unexpected crashes or transient database disconnects during transition execution, the state machine restarts from the last committed snapshot without duplicate side-effects.
