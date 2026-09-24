# 5-Minute Quick Start — EricksonLopez.Processes

Rapid onboarding guide to define, execute, and persist your first Process Manager with **EricksonLopez.Processes** in .NET 10 in under 5 minutes.

---

## 1. Package Installation

In your .NET 10 project, install the core packages:

```bash
dotnet add package EricksonLopez.Processes.Abstractions
dotnet add package EricksonLopez.Processes
dotnet add package EricksonLopez.Processes.DependencyInjection
dotnet add package EricksonLopez.Processes.SystemTextJson
```

For unit testing and local development, add the testing package:

```bash
dotnet add package EricksonLopez.Processes.Testing
```

---

## 2. Define State and Domain Events

Process state is modeled as an immutable `record` implementing `IProcessState`:

```csharp
using System;
using EricksonLopez.Processes.Abstractions;

// 1. Immutable process state
public sealed record OrderState(
    string OrderId,
    decimal TotalAmount,
    bool Confirmed) : IProcessState;

// 2. Domain event triggers
public sealed record OrderPlacedEvent(Guid OrderId, decimal Amount);
public sealed record PaymentReceivedEvent(Guid OrderId);
public sealed record RequestPaymentCommand(Guid OrderId, decimal Amount);
public sealed record OrderConfirmedEvent(string OrderId);
```

---

## 3. Implement the Process Manager

Implement `IProcess<TState>` and `IProcessHandler<TState, TEvent>` for each incoming domain event:

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

[ProcessDefinition("order.flow", 1)]
public sealed class OrderProcessManager :
    IProcess<OrderState>,
    IProcessHandler<OrderState, OrderPlacedEvent>,
    IProcessHandler<OrderState, PaymentReceivedEvent>
{
    public ProcessType Type => ProcessType.From("order.flow");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
        OrderState state,
        OrderPlacedEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with
        {
            OrderId = eventMessage.OrderId.ToString(),
            TotalAmount = eventMessage.Amount
        };

        // Emit a pure command intent without performing network I/O
        var command = new ProcessEffect.Command(
            new RequestPaymentCommand(eventMessage.OrderId, eventMessage.Amount));

        return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Advance(
            newState,
            ProcessStatus.Running,
            effects: [command]));
    }

    public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
        OrderState state,
        PaymentReceivedEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with { Confirmed = true };

        var @event = new ProcessEffect.Event(
            new OrderConfirmedEvent(state.OrderId));

        return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Complete(
            newState,
            effects: [@event]));
    }
}
```

---

## 4. Configure the Correlation Extractor

Map incoming events to `ProcessId` and `CorrelationId`:

```csharp
using System;
using EricksonLopez.Processes.Abstractions;

public sealed class OrderPlacedCorrelation : IProcessCorrelation<OrderPlacedEvent>
{
    public ProcessId ExtractProcessId(OrderPlacedEvent e) => ProcessId.FromGuid(e.OrderId);
    public CorrelationId ExtractCorrelationId(OrderPlacedEvent e) => CorrelationId.From(e.OrderId.ToString());
    public CausationId? ExtractCausationId(OrderPlacedEvent e) => null;
}
```

---

## 5. Execute via ProcessCoordinator

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Testing;

var store = new InMemoryProcessStore<OrderState>();
var coordinator = new ProcessCoordinator<OrderState>(store);
var process = new OrderProcessManager();

var orderId = Guid.NewGuid();
var @event = new OrderPlacedEvent(orderId, 199.99m);

// Execute initial transition (creates the instance)
var result = await coordinator.ExecuteAsync(
    handler: process,
    correlation: new OrderPlacedCorrelation(),
    eventMessage: @event,
    initialStateFactory: e => new OrderState(e.OrderId.ToString(), e.Amount, false),
    canInitiate: true,
    cancellationToken: CancellationToken.None);

Console.WriteLine($"Process started! Status: {result.Instance.Status}, Revision: {result.Instance.Revision}");
Console.WriteLine($"Emitted effects: {result.Effects.Count}");
```

---

## Next Steps

- Explore production-ready recipes in the [Official Cookbook](cookbook.md).
- Study distributed sagas and reverse-order rollback in [Building Sagas](building-sagas.md).
- Review all 11 progressive showcase modules in the [Showcase Reference](../showcase/index.md).
