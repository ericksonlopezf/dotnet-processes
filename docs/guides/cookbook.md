# Official Cookbook & Recipes — EricksonLopez.Processes

Comprehensive collection of practical, fully compilable recipes verified against the official public API of `EricksonLopez.Processes` in .NET 10. Each recipe addresses a concrete architectural requirement using verified types from the public API inventory.

---

## Recipe Index

1. [Recipe 1: Minimal Process Manager (Forward-Only Flow Without Compensation)](#recipe-1-minimal-process-manager-forward-only-flow-without-compensation)
2. [Recipe 2: Distributed Saga with Reverse LIFO Compensation](#recipe-2-distributed-saga-with-reverse-lifo-compensation)
3. [Recipe 3: Multi-Key Correlation with `CompositeCorrelationKey`](#recipe-3-multi-key-correlation-with-compositecorrelationkey)
4. [Recipe 4: Complete Dependency Injection & Storage Configuration](#recipe-4-complete-dependency-injection--storage-configuration)
5. [Recipe 5: Reliable Transactional Dispatch with Outbox](#recipe-5-reliable-transactional-dispatch-with-outbox)
6. [Recipe 6: Synchronous State Version Migration (`ProcessStateMigrationPipeline`)](#recipe-6-synchronous-state-version-migration-processstatemigrationpipeline)
7. [Recipe 7: Unit Testing & OCC Resilience with Test Doubles](#recipe-7-unit-testing--occ-resilience-with-test-doubles)
8. [Recipe 8: Native AOT Publishing Setup with `JsonSerializerContext`](#recipe-8-native-aot-publishing-setup-with-jsonserializercontext)
9. [Recipe 9: Identifier Bridging Across Libraries (`ProcessEventsIdentifierExtensions`)](#recipe-9-identifier-bridging-across-libraries-processeventsidentifierextensions)
10. [Recipe 10: Instrumentation & Observability with OpenTelemetry (`ProcessDiagnostics`)](#recipe-10-instrumentation--observability-with-opentelemetry-processdiagnostics)

---

## Recipe 1: Minimal Process Manager (Forward-Only Flow Without Compensation)

### Problem
You need to orchestrate a sequential enterprise workflow (such as invoice certification) where each step advances the process status and no automated rollback is required upon failure.

### Solution
Implement `IProcess<TState>` and `IProcessHandler<TState, TEvent>` for each relevant domain event.

### Complete Code

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace MyEnterpriseApp.Processes;

// 1. Immutable process state
public sealed record InvoiceCertificationState(
    string InvoiceId,
    decimal TotalAmount,
    bool TaxCalculated,
    bool Certified) : IProcessState;

// 2. Domain events
public sealed record InvoiceRegisteredEvent(Guid InvoiceId, decimal Amount);
public sealed record TaxCalculatedEvent(Guid InvoiceId, decimal TaxAmount);
public sealed record CertificationApprovedEvent(Guid InvoiceId);

// 3. Process Manager
[ProcessDefinition("invoice.certification", 1)]
public sealed class InvoiceCertificationProcess :
    IProcess<InvoiceCertificationState>,
    IProcessHandler<InvoiceCertificationState, InvoiceRegisteredEvent>,
    IProcessHandler<InvoiceCertificationState, TaxCalculatedEvent>,
    IProcessHandler<InvoiceCertificationState, CertificationApprovedEvent>
{
    public ProcessType Type => ProcessType.From("invoice.certification");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<InvoiceCertificationState>> HandleAsync(
        InvoiceCertificationState state,
        InvoiceRegisteredEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with
        {
            InvoiceId = eventMessage.InvoiceId.ToString(),
            TotalAmount = eventMessage.Amount
        };

        // Emit command intent to calculate tax
        var commandEffect = ProcessEffect.CreateCommand(
            new { InvoiceId = eventMessage.InvoiceId, Amount = eventMessage.Amount },
            "CalculateTaxCommand");

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceCertificationState>.Advance(
            newState,
            ProcessStatus.Running,
            effects: [commandEffect]));
    }

    public ValueTask<ProcessTransitionResult<InvoiceCertificationState>> HandleAsync(
        InvoiceCertificationState state,
        TaxCalculatedEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with { TaxCalculated = true };
        return ValueTask.FromResult(ProcessTransitionResult<InvoiceCertificationState>.Advance(
            newState,
            ProcessStatus.Running));
    }

    public ValueTask<ProcessTransitionResult<InvoiceCertificationState>> HandleAsync(
        InvoiceCertificationState state,
        CertificationApprovedEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with { Certified = true };

        var completionEvent = ProcessEffect.CreateEvent(
            new { state.InvoiceId, CertifiedAt = context.Now },
            "InvoiceCertifiedEvent");

        return ValueTask.FromResult(ProcessTransitionResult<InvoiceCertificationState>.Complete(
            newState,
            effects: [completionEvent]));
    }
}
```

### Explanation
- Handlers are pure functions: they receive the current state, the incoming event, and the context, returning an immutable `ProcessTransitionResult<TState>`.
- `ProcessEffect.CreateCommand` and `ProcessEffect.CreateEvent` declare outbound side-effect intents without performing direct network I/O.

### Best Practices
- Use immutable records (`record`) for state models with value semantics.
- Always read `context.Now` instead of `DateTime.UtcNow` to guarantee deterministic unit testing with `TimeProvider`.

### Common Pitfalls
- ❌ Mutating properties on the input `state` instance.
- ❌ Throwing exceptions for expected business errors instead of returning `ProcessTransitionResult.Fail(state, reason)`.

---

## Recipe 2: Distributed Saga with Reverse LIFO Compensation

### Problem
You need to coordinate a distributed transaction (such as a travel booking with flight and hotel) where a failure at a later stage requires rolling back previous successful steps in reverse order (LIFO).

### Solution
Implement `ISaga<TState>` and `ICompensationHandler<TState>`, recording forward milestones using `CompensationStep` and returning `CompensationAction` directives upon failure.

### Complete Code

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace MyEnterpriseApp.Sagas;

public sealed record TravelBookingState(
    string BookingId,
    bool HotelBooked,
    bool FlightBooked) : IProcessState;

public sealed record StartBookingEvent(Guid BookingId);
public sealed record HotelBookedEvent(Guid BookingId, string HotelReservationCode);
public sealed record FlightFailedEvent(Guid BookingId, string FailureReason);

[SagaDefinition("travel.booking", 1)]
public sealed class TravelBookingSaga :
    ISaga<TravelBookingState>,
    ICompensationHandler<TravelBookingState>,
    IProcessHandler<TravelBookingState, StartBookingEvent>,
    IProcessHandler<TravelBookingState, HotelBookedEvent>,
    IProcessHandler<TravelBookingState, FlightFailedEvent>
{
    public ProcessType Type => ProcessType.From("travel.booking");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<TravelBookingState>> HandleAsync(
        TravelBookingState state,
        StartBookingEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with { BookingId = eventMessage.BookingId.ToString() };
        var bookHotelCmd = ProcessEffect.CreateCommand(new { eventMessage.BookingId }, "BookHotelCommand");

        return ValueTask.FromResult(ProcessTransitionResult<TravelBookingState>.Advance(
            newState,
            ProcessStatus.Running,
            effects: [bookHotelCmd]));
    }

    public ValueTask<ProcessTransitionResult<TravelBookingState>> HandleAsync(
        TravelBookingState state,
        HotelBookedEvent eventMessage,
        ProcessContext context)
    {
        var newState = state with { HotelBooked = true };

        // Record compensable milestone on the LIFO stack
        var compensationStep = CompensationStep.Create(
            "CancelHotelReservation",
            new { eventMessage.HotelReservationCode },
            context.Now);

        var bookFlightCmd = ProcessEffect.CreateCommand(new { state.BookingId }, "BookFlightCommand");

        return ValueTask.FromResult(ProcessTransitionResult<TravelBookingState>.Advance(
            newState,
            ProcessStatus.Running,
            effects: [bookFlightCmd],
            recordedCompensations: [compensationStep]));
    }

    public ValueTask<ProcessTransitionResult<TravelBookingState>> HandleAsync(
        TravelBookingState state,
        FlightFailedEvent eventMessage,
        ProcessContext context)
    {
        // Trigger compensation by supplying actions to execute
        var cancelAction = CompensationAction.Create(
            "CancelHotelReservation",
            new { state.BookingId });

        return ValueTask.FromResult(ProcessTransitionResult<TravelBookingState>.Compensate(
            state,
            compensationActions: [cancelAction]));
    }

    public ValueTask<ProcessTransitionResult<TravelBookingState>> CompensateAsync(
        TravelBookingState state,
        CompensationAction action,
        ProcessContext context)
    {
        if (action.StepName == "CancelHotelReservation")
        {
            var newState = state with { HotelBooked = false };
            var cancelCmd = ProcessEffect.CreateCommand(action.Payload, "SendHotelCancellationCommand");

            return ValueTask.FromResult(ProcessTransitionResult<TravelBookingState>.Compensated(
                newState,
                effects: [cancelCmd]));
        }

        return ValueTask.FromResult(ProcessTransitionResult<TravelBookingState>.Fail(
            state,
            $"Unrecognized compensation action: {action.StepName}"));
    }
}
```

### Explanation
- When `FlightFailedEvent` arrives, the handler returns `ProcessTransitionResult.Compensate(state, actions)`.
- The coordinator invokes the internal `SagaCompensationEngine`, reversing the recorded steps in LIFO order and invoking `CompensateAsync`.
- Once all reversions complete successfully, the instance transitions to `ProcessStatus.Compensated`.

### Best Practices
- Store all data required to undo an operation inside `CompensationStep.Payload` without relying on external state queries.
- Ensure every compensation step is idempotent.

---

## Recipe 3: Multi-Key Correlation with `CompositeCorrelationKey`

### Problem
A purchase order workflow is identified by a compound key consisting of `TenantId`, `CustomerId`, and `OrderNumber`. You need to produce a unique, deterministic `CorrelationId` without key collisions.

### Solution
Use `CompositeCorrelationKey.From(...)`, which computes a deterministic SHA-256 (UUIDv5) hash to yield a canonical `CorrelationId`.

### Complete Code

```csharp
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyEnterpriseApp.Correlation;

public sealed record OrderPlacedEvent(string TenantId, string CustomerId, string OrderNumber);

public sealed class OrderPlacedCorrelation : IProcessCorrelation<OrderPlacedEvent>
{
    public ProcessId ExtractProcessId(OrderPlacedEvent eventMessage)
    {
        // Generates a new sequential UUIDv7 for the instance
        return ProcessId.NewId();
    }

    public CorrelationId ExtractCorrelationId(OrderPlacedEvent eventMessage) =>
        CompositeCorrelationKey.From(
            eventMessage.TenantId,
            eventMessage.CustomerId,
            eventMessage.OrderNumber).ToCorrelationId();

    public CausationId? ExtractCausationId(OrderPlacedEvent eventMessage) =>
        CausationId.From($"EVENT-ORDER-PLACED-{eventMessage.OrderNumber}");
}
```

---

## Recipe 4: Complete Dependency Injection & Storage Configuration

### Problem
Configure a .NET 10 host with compile-time Roslyn discovery (`AddProcesses()`), OCC retry options, System.Text.Json serialization, and PostgreSQL persistence.

### Solution
Register services in `Program.cs` using the official dependency injection extension methods.

### Complete Code

```csharp
using System;
using EricksonLopez.Processes.DependencyInjection;
using EricksonLopez.Processes.Storage.PostgreSql;
using EricksonLopez.Processes.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register core engine and compile-time Roslyn source-generated discoveries
builder.Services.AddProcesses();

// 2. Register coordinator with customized OCC retry options
builder.Services.AddProcessCoordinator<MyEnterpriseApp.Sagas.TravelBookingState>(options =>
{
    options.MaxConcurrencyRetries = 5;                           // Up to 5 retries on CAS collision
    options.InitialBackoffDelay = TimeSpan.FromMilliseconds(50); // Linear backoff: 50ms * attempt
    options.MaxCompensations = 100;                              // Safety guard on rollback steps
});

// 3. Register System.Text.Json serializer (AOT source-generated)
builder.Services.AddSingleton<EricksonLopez.Processes.Abstractions.IProcessStateSerializer<MyEnterpriseApp.Sagas.TravelBookingState>>(
    new SystemTextJsonProcessStateSerializer<MyEnterpriseApp.Sagas.TravelBookingState>(TravelBookingJsonContext.Default.TravelBookingState));

// 4. Register PostgreSQL persistent storage provider
builder.Services.AddPostgreSqlProcessStore<MyEnterpriseApp.Sagas.TravelBookingState>(
    connectionString: builder.Configuration["ConnectionStrings:Postgres"] ?? "Host=localhost;Database=processes;Username=postgres;Password=secret",
    tableName: "process_instances");

var app = builder.Build();
app.Run();
```

---

## Recipe 5: Reliable Transactional Dispatch with Outbox

### Problem
Prevent dual-write inconsistencies (updating the database state and publishing to a message broker in separate uncoordinated steps) by persisting process effects within the same transaction using an Outbox.

### Solution
Use `IProcessOutboxDispatcher` to reliably dispatch emitted `ProcessEffect` records.

### Complete Code

```csharp
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Outbox;

namespace MyEnterpriseApp.Execution;

public sealed class ProcessExecutionWorker
{
    private readonly ProcessCoordinator<MyEnterpriseApp.Processes.InvoiceCertificationState> _coordinator;
    private readonly IProcessOutboxDispatcher _outboxDispatcher;

    public ProcessExecutionWorker(
        ProcessCoordinator<MyEnterpriseApp.Processes.InvoiceCertificationState> coordinator,
        IProcessOutboxDispatcher outboxDispatcher)
    {
        _coordinator = coordinator;
        _outboxDispatcher = outboxDispatcher;
    }

    public async Task ProcessEventAsync(
        MyEnterpriseApp.Processes.InvoiceRegisteredEvent @event,
        IProcessCorrelation<MyEnterpriseApp.Processes.InvoiceRegisteredEvent> correlation,
        IOutboxTransactionContext transactionContext,
        CancellationToken cancellationToken)
    {
        var result = await _coordinator.ExecuteAsync(
            handler: new MyEnterpriseApp.Processes.InvoiceCertificationProcess(),
            correlation: correlation,
            eventMessage: @event,
            initialStateFactory: e => new MyEnterpriseApp.Processes.InvoiceCertificationState(e.InvoiceId.ToString(), e.Amount, false, false),
            canInitiate: true,
            cancellationToken: cancellationToken);

        if (result.IsSuccess && result.Effects.Count > 0)
        {
            // Atomically dispatches all effects via the transactional outbox
            await _outboxDispatcher.DispatchEffectsAsync(
                result.Effects,
                result.Instance.Id,
                transactionContext,
                cancellationToken);
        }
    }
}
```

---

## Recipe 6: Synchronous State Version Migration (`ProcessStateMigrationPipeline`)

### Problem
State schema has evolved from Version 1 to Version 3 (`V1 -> V2 -> V3`). In-flight instances stored in the database must migrate in-memory seamlessly without downtime.

### Solution
Implement `IProcessStateMigrator<TFrom, TTo>` and chain them using `ProcessStateMigrationPipeline`.

### Complete Code

```csharp
using System;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

namespace MyEnterpriseApp.Migrations;

public sealed record OrderStateV1(string OrderId, decimal Amount) : IProcessState;
public sealed record OrderStateV2(string OrderId, decimal Amount, string Currency) : IProcessState;
public sealed record OrderStateV3(string OrderId, decimal Amount, string Currency, decimal TaxAmount) : IProcessState;

public sealed class OrderV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public ProcessVersion FromVersion => ProcessVersion.From(1);
    public ProcessVersion ToVersion => ProcessVersion.From(2);

    public OrderStateV2 Migrate(OrderStateV1 sourceState) =>
        new OrderStateV2(sourceState.OrderId, sourceState.Amount, Currency: "USD");
}

public static class OrderMigrationPipelineFactory
{
    public static IProcessStateMigrator<OrderStateV1, OrderStateV3> CreatePipeline()
    {
        return ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(new OrderV1ToV2Migrator())
            .AddStep(ProcessVersion.From(3), (OrderStateV2 v2) =>
                new OrderStateV3(v2.OrderId, v2.Amount, v2.Currency, TaxAmount: v2.Amount * 0.18m))
            .Build();
    }
}
```

---

## Recipe 7: Unit Testing & OCC Resilience with Test Doubles

### Problem
Verify that your process tolerates optimistic concurrency conflicts and retries correctly without requiring an active external database.

### Solution
Use `FaultInjectingProcessStore<TState>` decorating an `InMemoryProcessStore<TState>`.

### Complete Code

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using Xunit;

public sealed class ConcurrencyResilienceTests
{
    [Fact]
    public async Task Coordinator_ShouldRetryAndSucceed_WhenConcurrencyConflictsOccur()
    {
        // Arrange
        var innerStore = new InMemoryProcessStore<MyEnterpriseApp.Processes.InvoiceCertificationState>();
        var faultStore = new FaultInjectingProcessStore<MyEnterpriseApp.Processes.InvoiceCertificationState>(innerStore)
        {
            // Simulate 2 OCC collisions before succeeding
            ConcurrencyConflictsToSimulate = 2
        };

        var options = new ProcessCoordinatorOptions
        {
            MaxConcurrencyRetries = 3,
            InitialBackoffDelay = TimeSpan.FromMilliseconds(5)
        };

        var coordinator = new ProcessCoordinator<MyEnterpriseApp.Processes.InvoiceCertificationState>(faultStore, options);
        var handler = new MyEnterpriseApp.Processes.InvoiceCertificationProcess();
        var invoiceId = Guid.NewGuid();
        var @event = new MyEnterpriseApp.Processes.InvoiceRegisteredEvent(invoiceId, 500m);

        // Act
        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: new SimpleCorrelation(invoiceId),
            eventMessage: @event,
            initialStateFactory: e => new MyEnterpriseApp.Processes.InvoiceCertificationState(e.InvoiceId.ToString(), e.Amount, false, false),
            canInitiate: true,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessSaveResult.Success, result.SaveResult);
        Assert.Equal(ProcessStatus.Running, result.Instance.Status);
    }

    private sealed class SimpleCorrelation : IProcessCorrelation<MyEnterpriseApp.Processes.InvoiceRegisteredEvent>
    {
        private readonly Guid _id;
        public SimpleCorrelation(Guid id) => _id = id;
        public ProcessId ExtractProcessId(MyEnterpriseApp.Processes.InvoiceRegisteredEvent e) => ProcessId.FromGuid(_id);
        public CorrelationId ExtractCorrelationId(MyEnterpriseApp.Processes.InvoiceRegisteredEvent e) => CorrelationId.From(_id.ToString());
        public CausationId? ExtractCausationId(MyEnterpriseApp.Processes.InvoiceRegisteredEvent e) => null;
    }
}
```

---

## Recipe 8: Native AOT Publishing Setup with `JsonSerializerContext`

### Problem
Publish your application as a self-contained Native AOT executable without trimming warnings or runtime reflection during state serialization.

### Solution
Declare a `JsonSerializerContext` with `[JsonSerializable]` attributes and pass it to `ProcessJsonSerializerOptions`.

### Complete Code

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.SystemTextJson;

namespace MyEnterpriseApp.Serialization;

[JsonSerializable(typeof(MyEnterpriseApp.Processes.InvoiceCertificationState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
public partial class AppJsonContext : JsonSerializerContext
{
}

public static class AotConfigurationHelper
{
    public static JsonSerializerOptions CreateAotOptions()
    {
        // Pre-registers official identifier converters with the AOT context
        return ProcessJsonSerializerOptions.Create(AppJsonContext.Default);
    }
}
```

---

## Recipe 9: Identifier Bridging Across Libraries (`ProcessEventsIdentifierExtensions`)

### Problem
Your application uses both `EricksonLopez.Processes` and `EricksonLopez.Events`. You need to map identifiers across packages without string allocations or manual conversions.

### Solution
Use the allocation-free extension methods provided by `ProcessEventsIdentifierExtensions`.

### Complete Code

```csharp
using System;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Events;

namespace MyEnterpriseApp.Integration;

public static class IdentifierBridgingExample
{
    public static void BridgeIdentifiers()
    {
        // 1. From Processes to Events
        var procCorrelation = CorrelationId.From("TX-ORD-9988");
        EricksonLopez.Events.Identifiers.CorrelationId eventsCorrelation = procCorrelation.ToEventsCorrelationId();

        // 2. From Events back to Processes
        CorrelationId backToProcesses = eventsCorrelation.ToProcessesCorrelationId();

        // 3. CausationId
        var procCausation = CausationId.From("CMD-START-01");
        EricksonLopez.Events.Identifiers.CausationId eventsCausation = procCausation.ToEventsCausationId();
        CausationId causationBack = eventsCausation.ToProcessesCausationId();

        Console.WriteLine($"Correlation matches: {procCorrelation.Value == backToProcesses.Value}");
        Console.WriteLine($"Causation matches: {procCausation.Value == causationBack.Value}");
    }
}
```

---

## Recipe 10: Instrumentation & Observability with OpenTelemetry (`ProcessDiagnostics`)

### Problem
Monitor OCC collision rates, transition latency, and process completion rates in telemetry tools such as Prometheus, Grafana, or Datadog.

### Solution
Configure OpenTelemetry listeners for the official `"EricksonLopez.Processes"` activity source and meter.

### Complete Code

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;

public static class TelemetrySetup
{
    public static void ConfigureOpenTelemetry(IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("EnterpriseOrderService"))
            .WithTracing(tracing => tracing
                .AddSource("EricksonLopez.Processes") // Official ActivitySource name
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddMeter("EricksonLopez.Processes")  // Official Meter name
                .AddConsoleExporter());
    }
}
```
