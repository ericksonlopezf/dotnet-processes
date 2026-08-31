# Cookbook — EricksonLopez.Processes

Verified, copy-paste-ready recipes for common `EricksonLopez.Processes` scenarios. All code is validated against the current public API.

---

## Recipe 1: Minimal Process Manager (No Compensation)

Use a Process Manager when steps are non-reversible or you only need forward-progression tracking.

```csharp
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

// 1. Define state
public sealed record InvoiceState(
    string InvoiceId,
    bool Approved,
    bool Paid) : IProcessState;

// 2. Define events
public sealed record InvoiceCreatedEvent(Guid InvoiceId);
public sealed record InvoiceApprovedEvent(Guid InvoiceId);
public sealed record InvoicePaidEvent(Guid InvoiceId);

// 3. Implement process (no compensation)
[ProcessDefinition("invoice.approval", 1)]
public sealed class InvoiceApprovalProcess :
    IProcess<InvoiceState>,
    IProcessHandler<InvoiceState, InvoiceCreatedEvent>,
    IProcessHandler<InvoiceState, InvoiceApprovedEvent>,
    IProcessHandler<InvoiceState, InvoicePaidEvent>
{
    // Required by IProcess<TState>: identifies this process in the registry
    public ProcessType Type { get; } = ProcessType.From("invoice.approval");
    public ProcessVersion Version { get; } = ProcessVersion.From(1);

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state, InvoiceCreatedEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Advance(
            state with { InvoiceId = e.InvoiceId.ToString() },
            ProcessStatus.Running));

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state, InvoiceApprovedEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Advance(
            state with { Approved = true },
            ProcessStatus.Running));

    public ValueTask<ProcessTransitionResult<InvoiceState>> HandleAsync(
        InvoiceState state, InvoicePaidEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<InvoiceState>.Complete(
            state with { Paid = true }));
}
```

---

## Recipe 2: Saga with Reverse-Order Compensation

```csharp
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

public sealed record BookingState(
    string BookingId,
    bool HotelReserved,
    bool FlightBooked) : IProcessState;

public sealed record BookingStartedEvent(Guid BookingId);
public sealed record HotelReservedEvent(Guid BookingId, string HotelId);
public sealed record FlightFailedEvent(Guid BookingId, string Reason);

[SagaDefinition("travel.booking", 1)]
public sealed class TravelBookingSaga :
    ISaga<BookingState>,
    ICompensationHandler<BookingState>,
    IProcessHandler<BookingState, BookingStartedEvent>,
    IProcessHandler<BookingState, HotelReservedEvent>,
    IProcessHandler<BookingState, FlightFailedEvent>
{
    // Forward: initiate
    public ValueTask<ProcessTransitionResult<BookingState>> HandleAsync(
        BookingState state, BookingStartedEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<BookingState>.Advance(
            state with { BookingId = e.BookingId.ToString() },
            ProcessStatus.Running,
            effects: [new ProcessEffect.Command(new ReserveHotelCommand(e.BookingId))]));

    // Forward: record compensation step on success
    public ValueTask<ProcessTransitionResult<BookingState>> HandleAsync(
        BookingState state, HotelReservedEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<BookingState>.Advance(
            state with { HotelReserved = true },
            ProcessStatus.Running,
            effects: [new ProcessEffect.Command(new BookFlightCommand(e.BookingId))],
            recordedCompensations: [new CompensationStep("ReserveHotel", new { e.HotelId }, ctx.Now)]));

    // Forward: trigger compensation
    public ValueTask<ProcessTransitionResult<BookingState>> HandleAsync(
        BookingState state, FlightFailedEvent e, ProcessContext ctx) =>
        ValueTask.FromResult(ProcessTransitionResult<BookingState>.Compensate(
            state,
            compensationActions: [new CompensationAction("ReserveHotel", new { state.BookingId })]));

    // Compensation handler (LIFO)
    public ValueTask<ProcessTransitionResult<BookingState>> CompensateAsync(
        BookingState state, CompensationAction action, ProcessContext ctx) =>
        action.StepName switch
        {
            "ReserveHotel" => ValueTask.FromResult(
                ProcessTransitionResult<BookingState>.Advance(
                    state with { HotelReserved = false },
                    ProcessStatus.Compensating,
                    effects: [new ProcessEffect.Command(new CancelHotelCommand(state.BookingId))])),
            _ => ValueTask.FromResult(
                ProcessTransitionResult<BookingState>.Fail(state, $"Unknown step: {action.StepName}"))
        };
}
```

---

## Recipe 3: Composite Correlation Keys

Use `CompositeCorrelationKey` when a process instance is identified by multiple business keys.

```csharp
public sealed class MultiKeyCorrelation : IProcessCorrelation<ShipmentEvent>
{
    public ProcessId ExtractProcessId(ShipmentEvent e) =>
        ProcessId.From(CompositeCorrelationKey
            .From(e.OrderId, e.WarehouseId)
            .ToCorrelationId().Value);

    public CorrelationId ExtractCorrelationId(ShipmentEvent e) =>
        CompositeCorrelationKey
            .From(e.OrderId, e.WarehouseId)
            .ToCorrelationId();
}
```

---

## Recipe 4: Full DI Registration with Source Generator

```csharp
// Program.cs
using EricksonLopez.Processes.DependencyInjection;
using EricksonLopez.Processes.Storage.PostgreSql;
using EricksonLopez.Processes.SystemTextJson;

builder.Services
    .AddGeneratedProcesses()               // Source-generated: registers all [SagaDefinition] / [ProcessDefinition]
    .AddProcessCoordinator<OrderSagaState>(options =>
    {
        options.MaxConcurrencyRetries = 3;    // Default; increase for high-concurrency scenarios
        options.InitialBackoffDelay = TimeSpan.FromMilliseconds(50); // Default; decrease for low-latency
    })
    .AddSystemTextJsonProcessStateSerializer<OrderSagaState>()
    .AddPostgreSqlProcessStore<OrderSagaState>(
        connectionString: builder.Configuration["ConnectionStrings:Postgres"]!,
        tableName: "order_saga_instances");
```

---

## Recipe 5: Outbox Effect Dispatching

Dispatch `ProcessEffect.OutboxMessage` effects reliably to avoid dual-write.

```csharp
// Register
services.AddProcessOutboxDispatcher();

// Consume after ExecuteAsync
var result = await coordinator.ExecuteAsync(...);

foreach (var effect in result.Effects)
{
    if (effect is ProcessEffect.OutboxMessage outbox)
        await outboxDispatcher.DispatchAsync(outbox.Payload, cancellationToken);
}
```

---

## Recipe 6: State Schema Migration

Evolve process state across versions without losing existing instances.

```csharp
// Old state (version 1)
public sealed record OrderStateV1(string OrderId, string Status) : IProcessState;

// New state (version 2)
public sealed record OrderStateV2(
    string OrderId,
    string Status,
    string CustomerId   // new field with default
) : IProcessState;

// Migration step
public sealed class OrderStateV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public Task<OrderStateV2> MigrateAsync(OrderStateV1 old, CancellationToken ct) =>
        Task.FromResult(new OrderStateV2(old.OrderId, old.Status, CustomerId: "UNKNOWN"));
}

// DI registration
services.AddProcessStateMigrator<OrderStateV1, OrderStateV2, OrderStateV1ToV2Migrator>();
```

The `ProcessStateMigrationPipeline<TState>` chains migrators automatically during `ExecuteAsync` when the stored `Version` is lower than the current version.

---

## Recipe 7: In-Memory Testing with `InMemoryProcessStore`

```csharp
using EricksonLopez.Processes.Testing;
using Xunit;

public sealed class OrderSagaTests
{
    [Fact]
    public async Task HandleOrderCreated_ShouldTransitionToRunning()
    {
        // Arrange
        var store = new InMemoryProcessStore<OrderSagaState>();
        var coordinator = new ProcessCoordinator<OrderSagaState>(store, new ProcessCoordinatorOptions());
        var saga = new OrderFulfillmentSaga();
        var @event = new OrderCreatedEvent(Guid.NewGuid(), "CUST-1", 100m);

        // Act
        var result = await coordinator.ExecuteAsync(
            handler: saga,
            correlation: new OrderCreatedCorrelation(),
            eventMessage: @event,
            initialStateFactory: e => new OrderSagaState(e.OrderId.ToString(), e.CustomerId, e.Amount, false, false),
            canInitiate: true);

        // Assert
        Assert.Equal(ProcessStatus.Running, result.Instance.Status);
        Assert.Single(result.Effects);
        Assert.IsType<ProcessEffect.Command>(result.Effects[0]);
    }
}
```

---

## Recipe 8: Native AOT Registration

For Native AOT publishing, ensure all state types are registered in a `JsonSerializerContext`:

```csharp
[JsonSerializable(typeof(OrderSagaState))]
[JsonSerializable(typeof(CompensationStep[]))]
internal partial class ProcessSerializerContext : JsonSerializerContext { }

// DI
services.AddSystemTextJsonProcessStateSerializer<OrderSagaState>(
    jsonTypeInfo: ProcessSerializerContext.Default.OrderSagaState);
```

The library itself uses no reflection — all dispatch is static generic. The only source of AOT incompatibility is the `IProcessStateSerializer` implementation — use `SystemTextJsonProcessStateSerializer` with a `JsonSerializerContext` for full AOT compatibility.
