# API Reference — EricksonLopez.Processes

Complete technical reference for all public types, interfaces, records, and exceptions in the `EricksonLopez.Processes` ecosystem. All types are derived from the actual source code and verified against `net10.0`.

---

## Namespace `EricksonLopez.Processes.Abstractions`

Package: `EricksonLopez.Processes.Abstractions` (`netstandard2.0`, `net10.0`)

### Strongly Typed Identifiers

All identifiers are `readonly record struct` — value semantics, zero-allocation, span-parsable, AOT-safe.

#### `ProcessId`

```csharp
public readonly record struct ProcessId : ISpanParsable<ProcessId>, ISpanFormattable
```

The primary key for a process instance. Wraps a `Guid`.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From(Guid)` | `static ProcessId From(Guid value)` | Factory: creates a `ProcessId` from an existing `Guid`. |
| `FromGuid(Guid)` | `static ProcessId FromGuid(Guid value)` | Alias for `From(Guid)`. |
| `NewId()` | `static ProcessId NewId()` | Generates a new random `ProcessId` (UUIDv7). |
| `Value` | `Guid Value { get; }` | The underlying `Guid`. |

#### `CorrelationId`

```csharp
public readonly record struct CorrelationId : ISpanParsable<CorrelationId>, ISpanFormattable
```

Business-level correlation identifier linking events to a process instance.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From(Guid)` | `static CorrelationId From(Guid value)` | Factory from `Guid` (stored as its string representation). |
| `FromGuid(Guid)` | `static CorrelationId FromGuid(Guid value)` | Alias for `From(Guid)`. |
| `From(string)` | `static CorrelationId From(string value)` | Factory from an existing string identifier (no hashing applied). |
| `FromString(string)` | `static CorrelationId FromString(string value)` | Alias for `From(string)`. |
| `NewId()` | `static CorrelationId NewId()` | Generates a new time-ordered `CorrelationId` (UUIDv7). |
| `Value` | `string Value { get; }` | The underlying string identifier. |

> **Note**: SHA-256 hashing is performed by `CompositeCorrelationKey.ToCorrelationId()`, not by `CorrelationId.From(string)`. `From(string)` wraps the string as-is.

#### `CausationId`

```csharp
public readonly record struct CausationId : ISpanParsable<CausationId>, ISpanFormattable
```

Optional causal chain identifier. Tracks which message caused this process action.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From(Guid)` | `static CausationId From(Guid value)` | Factory from `Guid`. |
| `Value` | `Guid Value { get; }` | The underlying `Guid`. |

#### `MessageId`

```csharp
public readonly record struct MessageId : ISpanParsable<MessageId>, ISpanFormattable
```

Unique message identifier for idempotency tracking.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From(Guid)` | `static MessageId From(Guid value)` | Factory from `Guid`. |
| `NewId()` | `static MessageId NewId()` | Generates a new random `MessageId`. |
| `Value` | `Guid Value { get; }` | The underlying `Guid`. |

#### `ProcessType`

```csharp
public readonly record struct ProcessType : ISpanParsable<ProcessType>, ISpanFormattable
```

Named discriminator identifying the kind of process (e.g., `"order.fulfillment"`).

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From(string)` | `static ProcessType From(string name)` | Factory from string name. |
| `Value` | `string Value { get; }` | The process type name. |

#### `ProcessVersion`

```csharp
public readonly record struct ProcessVersion : ISpanParsable<ProcessVersion>, ISpanFormattable
```

Schema version for state migration. Used in `IProcessStateMigrator<TFrom, TTo>`.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `Initial` | `static readonly ProcessVersion Initial` | Version `1`. |
| `From(int)` | `static ProcessVersion From(int version)` | Factory from integer. |
| `Value` | `int Value { get; }` | The version integer. |

#### `Revision`

```csharp
public readonly record struct Revision : ISpanParsable<Revision>, ISpanFormattable
```

Optimistic concurrency token (OCC). Monotonically incremented on each successful save.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `None` | `static Revision None` | Revision `0` — uncommitted/pre-save state. Not yet persisted. |
| `Initial` | `static Revision Initial` | Revision `1` — first committed revision after the first successful save. |
| `From(long)` | `static Revision From(long value)` | Factory from `long` (must be ≥ 0). |
| `FromInt64(long)` | `static Revision FromInt64(long value)` | Alias for `From(long)`. |
| `Next()` | `Revision Next()` | Returns `Revision.From(Value + 1)`. |
| `Value` | `long Value { get; }` | The current revision number. |

#### `CompositeCorrelationKey`

```csharp
public readonly record struct CompositeCorrelationKey
```

Constructs a deterministic `CorrelationId` from multiple business key parts via SHA-256 hashing.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `From<T1,T2>(T1,T2)` | `static CompositeCorrelationKey From<T1,T2>(T1 part1, T2 part2)` | 2-part composite key. |
| `From<T1,T2,T3>(...)` | `static CompositeCorrelationKey From<T1,T2,T3>(...)` | 3-part composite key. |
| `From<T1,T2,T3,T4>(...)` | `static CompositeCorrelationKey From<T1,T2,T3,T4>(...)` | 4-part composite key. |
| `ToCorrelationId()` | `CorrelationId ToCorrelationId()` | Returns a deterministic `CorrelationId` from the composite key. |
| `Value` | `string Value { get; }` | The combined string representation. |

---

### State & Instance Records

#### `IProcessState`

```csharp
public interface IProcessState
```

Marker interface. Must be implemented by all process state records. Should be an immutable `record`.

#### `ProcessInstance<TState>`

```csharp
public sealed record ProcessInstance<TState> where TState : notnull
```

The persistent envelope wrapping process state plus all metadata.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `ProcessId` | Primary key (unique process instance identifier). |
| `CorrelationId` | `CorrelationId` | Business correlation key. |
| `Type` | `ProcessType` | Discriminator (process kind). |
| `Version` | `ProcessVersion` | Schema version for migrations. |
| `Status` | `ProcessStatus` | Current lifecycle status. |
| `State` | `TState` | The domain state payload. |
| `Revision` | `Revision` | Optimistic concurrency token. |
| `CreatedAt` | `DateTimeOffset` | UTC creation timestamp. |
| `UpdatedAt` | `DateTimeOffset` | UTC last-update timestamp. |
| `CompletedAt` | `DateTimeOffset?` | UTC timestamp when the process reached a terminal status, if any. |

#### `ProcessStatus`

```csharp
public enum ProcessStatus
```

| Value | Description |
| :--- | :--- |
| `Initialized` (0) | Initial state — instance created but no events processed yet. |
| `Running` (1) | Active — executing forward workflow steps. |
| `Suspended` (2) | Suspended — awaiting external input, approval, or a timer trigger. |
| `Completed` (3) | Terminal success — process finished normally. |
| `Compensating` (4) | Compensation in progress — executing steps in LIFO order. |
| `Compensated` (5) | Terminal — compensation completed successfully. |
| `Failed` (6) | Terminal — compensation exhausted or unrecoverable error. |

#### `ProcessStateRecord`

```csharp
public sealed record ProcessStateRecord
```

Raw serialized state record stored in the persistence layer (storage adapter input/output).

> **Note**: `ProcessStateRecord` is the raw flat database record used internally by storage adapters. Application code works with `ProcessInstance<TState>` from `IProcessStore<TState>`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ProcessId` | `string` | Process primary key (string form of `ProcessId`). |
| `ProcessType` | `string` | Logical process type name. |
| `Version` | `string` | Schema version string. |
| `Status` | `int` | Integer representation of `ProcessStatus`. |
| `Revision` | `long` | OCC revision token. |
| `CorrelationId` | `string` | Business correlation identifier string. |
| `StatePayload` | `string` | Serialized state payload (format depends on `IProcessStateSerializer<TState>`). |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp (UTC). |
| `UpdatedAt` | `DateTimeOffset` | Last update timestamp (UTC). |
| `CompletedAt` | `DateTimeOffset?` | Terminal status timestamp (UTC), if applicable. |

---

### Effects & Compensation

#### `ProcessEffect`

```csharp
public abstract record ProcessEffect
```

Discriminated union of side-effect intents emitted from a transition. The library never dispatches effects — the host application is responsible for consuming them.

**Variants:**

| Subtype | Constructor Signature | Description |
| :--- | :--- | :--- |
| `ProcessEffect.Command` | `record Command(object CommandPayload, string? CommandType = null)` | Intent to dispatch a command (e.g., via mediator). |
| `ProcessEffect.Event` | `record Event(object EventPayload, string? EventType = null)` | Intent to publish a domain or integration event. |
| `ProcessEffect.ScheduleTimeout` | `record ScheduleTimeout(TimeSpan Delay, object TimeoutTrigger, string? TriggerType = null)` | Intent to schedule a timeout/wake-up after a delay. |
| `ProcessEffect.Compensation` | `record Compensation(CompensationAction Action)` | Intent to execute a compensation action. |

**Typed factory methods (preferred):**

| Factory | Description |
| :--- | :--- |
| `ProcessEffect.CreateCommand<T>(T payload, string? commandType = null)` | Creates a `Command` effect with the inferred type name as `CommandType`. |
| `ProcessEffect.CreateEvent<T>(T payload, string? eventType = null)` | Creates an `Event` effect with the inferred type name as `EventType`. |
| `ProcessEffect.CreateTimeout<T>(TimeSpan delay, T trigger, string? triggerType = null)` | Creates a `ScheduleTimeout` effect with the inferred type name as `TriggerType`. |
| `ProcessEffect.CreateCompensation(CompensationAction action)` | Creates a `Compensation` effect from an existing action. |
| `ProcessEffect.CreateCompensation<T>(string stepName, T payload)` | Creates a `Compensation` effect building the `CompensationAction` inline. |

#### `CompensationStep`

```csharp
public sealed record CompensationStep
```

A recorded step pushed onto the compensation LIFO stack during forward execution.

| Property | Type | Description |
| :--- | :--- | :--- |
| `StepName` | `string` | Logical name of the compensatable step. |
| `Payload` | `object?` | Data needed to execute compensation (e.g., charged amount). |
| `RecordedAt` | `DateTimeOffset` | UTC timestamp when the step was recorded. |

#### `CompensationAction`

```csharp
public sealed record CompensationAction
```

A compensation directive returned from `ProcessTransitionResult.Compensate(...)`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `StepName` | `string` | The step to compensate. |
| `Payload` | `object?` | Data for the compensation handler. |

#### `ProcessSaveResult`

```csharp
public enum ProcessSaveResult
```

Result returned from `IProcessStore<TState>.SaveAsync(...)`. An enum, not a record.

| Value | Integer | Description |
| :--- | :--- | :--- |
| `Success` | `0` | Save completed successfully — state was created or updated with a matching revision. |
| `ConcurrencyConflict` | `1` | OCC conflict — stored revision did not match the expected revision. |
| `NotFound` | `2` | Target process instance was not found in storage. |
| `PersistenceError` | `3` | Storage or network infrastructure error occurred during persistence. |

---

### Interfaces — Contracts

#### `IProcessCorrelation<TEvent>`

```csharp
public interface IProcessCorrelation<in TEvent>
```

Maps an incoming event to the target process instance identifiers.

| Member | Returns | Description |
| :--- | :--- | :--- |
| `ExtractProcessId(TEvent)` | `ProcessId` | Primary key extraction. |
| `ExtractCorrelationId(TEvent)` | `CorrelationId` | Business correlation extraction. |
| `ExtractCausationId(TEvent)` | `CausationId?` | Optional causal chain (default: `null`). |

#### `IProcessStore<TState>`

```csharp
public interface IProcessStore<TState> where TState : notnull
```

Storage port. Implement to integrate any persistence backend. Methods return `ValueTask<>` to avoid allocations on synchronous fast paths.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `GetByIdAsync` | `ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken ct = default)` | Load by primary key. Returns `null` if not found. |
| `SaveAsync` | `ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken ct = default)` | Persist with OCC. Returns `ProcessSaveResult` enum value. |
| `ExistsAsync` | `ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken ct = default)` | Check existence without loading state. |
| `GetByCorrelationIdAsync` | `ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken ct = default)` | Default interface method (DIM) — optional override. Default returns `null`. |

#### `IProcessStateSerializer<TState>`

```csharp
public interface IProcessStateSerializer<TState> where TState : notnull
```

Serialization/deserialization of `TState` to/from a binary payload. Returns `byte[]` (not `string`) to remain format-agnostic and AOT-safe.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `Serialize(TState)` | `byte[] Serialize(TState state)` | Serialize state to a binary payload. |
| `Deserialize(ReadOnlySpan<byte>)` | `TState Deserialize(ReadOnlySpan<byte> data)` | Deserialize a binary payload back to state. |

#### `IProcessStateMigrator<TFrom, TTo>`

```csharp
public interface IProcessStateMigrator<in TFrom, out TTo>
    where TFrom : notnull
    where TTo : notnull
```

Implements one synchronous schema version migration step. Migrate is **synchronous** and does not accept a `CancellationToken`. Chaining multiple steps is done via `ProcessStateMigrationPipeline`.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `FromVersion` | `ProcessVersion FromVersion { get; }` | The source schema version this migrator accepts. |
| `ToVersion` | `ProcessVersion ToVersion { get; }` | The target schema version this migrator produces. |
| `Migrate(TFrom)` | `TTo Migrate(TFrom sourceState)` | Transforms state from the old schema to the new schema (synchronous). |

#### `ISagaSnapshotRepository<TState>`

```csharp
public interface ISagaSnapshotRepository<TState> where TState : notnull
```

Optional interface for saga snapshot storage. Allows storing periodic state snapshots to optimize compensation replay performance.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `SaveSnapshotAsync` | `ValueTask SaveSnapshotAsync(ProcessInstance<TState> instance, CancellationToken ct = default)` | Persist a snapshot of the process instance. |
| `LoadSnapshotAsync` | `ValueTask<ProcessInstance<TState>?> LoadSnapshotAsync(ProcessId id, CancellationToken ct = default)` | Load the latest snapshot by process identifier. |

---

### Attributes

| Attribute | Target | Description |
| :--- | :--- | :--- |
| `[SagaDefinition(string type, int version)]` | Class | Marks a class as a Saga with a `ProcessType` name and version. |
| `[ProcessDefinition(string type, int version)]` | Class | Marks a class as a Process Manager. |
| `[ProcessHandler(Type stateType, Type eventType)]` | Class/Assembly | Registers a handler for Source Generator discovery. |
| `[ProcessType(string name)]` | Class | Alternative attribute for type registration. |

---

## Namespace `EricksonLopez.Processes`

Package: `EricksonLopez.Processes` (`net10.0`)

### Interfaces — Core

#### `IProcessHandler<TState, TEvent>`

```csharp
public interface IProcessHandler<TState, TEvent>
    where TState : notnull
```

Handles a specific event type for a process. Implement for each event the process reacts to.

```csharp
ValueTask<ProcessTransitionResult<TState>> HandleAsync(
    TState state,
    TEvent eventMessage,
    ProcessContext context);
```

#### `ICompensationHandler<TState>`

```csharp
public interface ICompensationHandler<TState>
    where TState : notnull
```

Executes a compensation step during saga rollback.

```csharp
ValueTask<ProcessTransitionResult<TState>> CompensateAsync(
    TState state,
    CompensationAction action,
    ProcessContext context);
```

#### `IProcess<TState>`

```csharp
public interface IProcess<TState>
    where TState : notnull
```

Marker interface for Process Managers (no compensation). Exposes `Type` (`ProcessType`) and `Version` (`ProcessVersion`) that identify the process in the registry.

#### `ISaga<TState>`

```csharp
public interface ISaga<TState>
    where TState : notnull
```

Marker interface for Sagas (compensation-capable). Combined with `ICompensationHandler<TState>`.

#### `IProcessRegistry`

```csharp
public interface IProcessRegistry
```

Internal DI registry of known process types. Populated by Source Generator via `AddGeneratedProcesses()`.

---

### Key Types

#### `ProcessTransitionResult<TState>`

```csharp
public sealed record ProcessTransitionResult<TState> where TState : notnull
```

The return value from every `HandleAsync` and `CompensateAsync` call. A `record`, not a `class`.

**Static factory methods:**

| Factory | Description |
| :--- | :--- |
| `Advance(state, status?, effects?, recordedCompensations?)` | Moves the process forward with a new state and effects. Default status: `Running`. |
| `Complete(state, effects?)` | Transitions to `Completed` terminal status. |
| `Fail(state, message?)` | Transitions to `Failed` terminal status with an optional reason. |

**Properties:**

| Property | Type | Description |
| :--- | :--- | :--- |
| `State` | `TState` | Updated state after transition. |
| `Status` | `ProcessStatus` | Target lifecycle status. |
| `Effects` | `IReadOnlyList<ProcessEffect>` | Side-effect intents emitted by this transition. |
| `RecordedCompensations` | `IReadOnlyList<CompensationStep>` | New compensation steps to push onto the LIFO stack. |
| `FailureReason` | `string?` | Explanation for `Failed` transitions; `null` otherwise. |

#### `ProcessContext`

```csharp
public sealed class ProcessContext
```

Execution context passed to every handler. Created by `ProcessContext.Create(...)` or the coordinator internally.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ProcessId` | `ProcessId` | Current process instance ID. |
| `CorrelationId` | `CorrelationId` | Business correlation ID. |
| `CausationId` | `CausationId` | Causal chain ID for the current execution. |
| `MessageId` | `MessageId` | Idempotency key of the triggering message. |
| `Now` | `DateTimeOffset` | UTC wall-clock time at execution start (from `TimeProvider`). |
| `TimeProvider` | `TimeProvider` | The time provider used by the coordinator. |
| `Items` | `IReadOnlyDictionary<string, object?>` | Ambient key/value bag for cross-cutting context (e.g., tenant ID). |
| `CancellationToken` | `CancellationToken` | Cooperative cancellation token from the calling operation. |

#### `ProcessCoordinator<TState>`

```csharp
public sealed class ProcessCoordinator<TState> where TState : notnull
```

The primary execution engine. Manages hydration, OCC retry loop, serialization, effect collection, and persistence.

```csharp
// Execute a forward step
ValueTask<ProcessExecutionResult<TState>> ExecuteAsync<TEvent>(
    IProcessHandler<TState, TEvent> handler,
    IProcessCorrelation<TEvent> correlation,
    TEvent eventMessage,
    Func<TEvent, TState>? initialStateFactory = null,
    bool canInitiate = false,
    CancellationToken cancellationToken = default);

// Execute reverse-order compensation for a saga
ValueTask<ProcessExecutionResult<TState>> CompensateAsync<TSaga>(
    ProcessId processId,
    IReadOnlyList<CompensationStep> recordedSteps,
    TSaga saga,
    CancellationToken cancellationToken = default)
    where TSaga : IProcess<TState>, ICompensationHandler<TState>;

// Reference linear backoff: 10ms * attempt (not the coordinator's default backoff strategy)
public static TimeSpan DefaultBackoffStrategy(int attempt);
```

> **Constructor**: `ProcessCoordinator<TState>(IProcessStore<TState> store, ProcessCoordinatorOptions? options = null, TimeProvider? timeProvider = null, Func<int, TimeSpan>? backoffStrategy = null)`
> The `backoffStrategy` parameter overrides the default linear `InitialBackoffDelay × attempt` formula.

#### `ProcessCoordinatorOptions`

```csharp
public sealed class ProcessCoordinatorOptions
```

Configuration for the OCC retry loop.

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `MaxConcurrencyRetries` | `int` | `3` | Maximum OCC retry attempts before throwing `ConcurrencyConflictException`. |
| `InitialBackoffDelay` | `TimeSpan` | `50ms` | Base delay for the linear backoff (`delay = InitialBackoffDelay × attempt`). |

#### `ProcessExecutionResult<TState>`

```csharp
public sealed record ProcessExecutionResult<TState> where TState : notnull
```

Returned from `ProcessCoordinator.ExecuteAsync(...)` and `ProcessCoordinator.CompensateAsync(...)`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Instance` | `ProcessInstance<TState>` | Final persisted process instance after the transition. |
| `Effects` | `IReadOnlyList<ProcessEffect>` | All effects emitted during execution. |
| `SaveResult` | `ProcessSaveResult` | The persistence outcome (enum value from `IProcessStore.SaveAsync`). |
| `IsSuccess` | `bool` | `true` if `SaveResult == ProcessSaveResult.Success`. |

#### `ProcessStateMigrationPipeline` (static class)

```csharp
public static class ProcessStateMigrationPipeline
```

Fluent factory for composing multi-step schema migration pipelines. Not generic itself — generics are on the builder.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `Create<TInitialState>` | `static ProcessStateMigrationPipelineBuilder<TInitialState> Create<TInitialState>(ProcessVersion initialVersion)` | Starts a new pipeline from the specified initial version. |

#### `ProcessStateMigrationPipelineBuilder<TCurrentState>`

```csharp
public sealed class ProcessStateMigrationPipelineBuilder<TCurrentState>
    where TCurrentState : notnull
```

Chains `IProcessStateMigrator<TFrom, TTo>` steps into a single migrator function.

| Member | Signature | Description |
| :--- | :--- | :--- |
| `AddStep<TNextState>` | `ProcessStateMigrationPipelineBuilder<TNextState> AddStep<TNextState>(IProcessStateMigrator<TCurrentState, TNextState>)` | Appends a migration step. Validates that `migrator.FromVersion` matches the current pipeline version. |
| `Build<TFinalState>` | `IProcessStateMigrator<object, TFinalState> Build<TFinalState>()` | Builds the composed migrator. |

#### `ProcessDiagnostics`

```csharp
public static class ProcessDiagnostics
```

OpenTelemetry `ActivitySource` and `Meter` for tracing and metrics. Activity name: `EricksonLopez.Processes`.

---

## Namespace `EricksonLopez.Processes.DependencyInjection`

Package: `EricksonLopez.Processes.DependencyInjection` (`net10.0`)

### Extension Methods on `IServiceCollection`

```csharp
// Register the coordinator for TState
services.AddProcessCoordinator<TState>(Action<ProcessCoordinatorOptions>? configure = null);

// Register a custom serializer
services.AddProcessStateSerializer<TSerializer>();
```

---

## Namespace `EricksonLopez.Processes.Generator`

Package: `EricksonLopez.Processes.Generator` (`netstandard2.0`)

Roslyn Incremental Source Generator. Generates compile-time registration code for all types annotated with `[SagaDefinition]`, `[ProcessDefinition]`, or `[ProcessHandler]`.

**Generated extension method:**

```csharp
// Generated at compile time — zero reflection
services.AddGeneratedProcesses();
```

---

## Namespace `EricksonLopez.Processes.SystemTextJson`

Package: `EricksonLopez.Processes.SystemTextJson` (`net10.0`)

| Type | Description |
| :--- | :--- |
| `SystemTextJsonProcessStateSerializer<TState>` | `IProcessStateSerializer<TState>` implementation using `System.Text.Json`. AOT-safe via `JsonSerializerContext`. |
| `ProcessJsonSerializerOptions` | Pre-configured `JsonSerializerOptions` with all process identifier converters registered. |
| `ProcessIdJsonConverter` | Custom `JsonConverter<ProcessId>`. |
| `CorrelationIdJsonConverter` | Custom `JsonConverter<CorrelationId>`. |
| `CausationIdJsonConverter` | Custom `JsonConverter<CausationId>`. |
| `MessageIdJsonConverter` | Custom `JsonConverter<MessageId>`. |
| `ProcessTypeJsonConverter` | Custom `JsonConverter<ProcessType>`. |
| `ProcessVersionJsonConverter` | Custom `JsonConverter<ProcessVersion>`. |
| `RevisionJsonConverter` | Custom `JsonConverter<Revision>`. |

---

## Namespace `EricksonLopez.Processes.Testing`

Package: `EricksonLopez.Processes.Testing` (`net10.0`)

| Type | Description |
| :--- | :--- |
| `InMemoryProcessStore<TState>` | Thread-safe `IProcessStore<TState>` backed by `ConcurrentDictionary` with an exclusive `lock` in `SaveAsync` for OCC correctness. For unit and property-based testing. |
| `FaultInjectingProcessStore<TState>` | Wraps any `IProcessStore<TState>` and injects configurable faults (save failures, load failures) for resilience testing. |
| `TestCounterState` | Pre-built `IProcessState` with a simple counter for testing transitions. |
| `TestOrderState` | Pre-built `IProcessState` modeling a minimal order for saga testing. |
| `TestProcessEvents` | Common test event types (`TestStarted`, `TestIncrement`, `TestComplete`). |
| `TestStorageModels` | Factory helpers for constructing `ProcessStateRecord` test fixtures. |

---

## Namespace `EricksonLopez.Processes.Events`

Package: `EricksonLopez.Processes.Events` (`net10.0`)

Bridges `ProcessEffect.Event` payloads to `EricksonLopez.Events.Contracts`.

| Type | Description |
| :--- | :--- |
| `IEventProcessDispatcher` | Port interface for event dispatching. |
| `EventProcessDispatcher` | Dispatches `ProcessEffect.Event` payloads via `EricksonLopez.Events`. |
| `ProcessEventsServiceCollectionExtensions` | `services.AddProcessEventsDispatcher()`. |

---

## Namespace `EricksonLopez.Processes.Mediator`

Package: `EricksonLopez.Processes.Mediator` (`net10.0`)

Bridges `ProcessEffect.Command` payloads to `EricksonLopez.Mediator`.

| Type | Description |
| :--- | :--- |
| `IMediatorProcessDispatcher` | Port interface for in-process command dispatching. |
| `MediatorProcessDispatcher` | Dispatches `ProcessEffect.Command` payloads via `EricksonLopez.Mediator`. |
| `ProcessMediatorServiceCollectionExtensions` | `services.AddProcessMediatorDispatcher()`. |

---

## Namespace `EricksonLopez.Processes.Outbox`

Package: `EricksonLopez.Processes.Outbox` (`net10.0`)

Bridges `ProcessEffect` side-effect intents reliably to `EricksonLopez.Outbox`. Dispatches all real effect variants (`Command`, `Event`, `ScheduleTimeout`, `Compensation`) as durable outbox messages.

| Type | Description |
| :--- | :--- |
| `IProcessOutboxDispatcher` | Port interface for reliable outbox dispatching of process effects. |
| `OutboxProcessDispatcher` | Dispatches `ProcessEffect.Command`, `ProcessEffect.Event`, `ProcessEffect.ScheduleTimeout`, and `ProcessEffect.Compensation` payloads durably via `EricksonLopez.Outbox`. |
| `ProcessOutboxServiceCollectionExtensions` | `services.AddProcessOutboxDispatcher()`. |

---

## Storage Providers

All storage packages expose a single `IServiceCollection` extension method and implement `IProcessStore<TState>` using Dapper over the native ADO.NET driver for the respective database.

| Package | Extension Method | Store Class | Driver |
| :--- | :--- | :--- | :--- |
| `Storage.PostgreSql` | `AddPostgreSqlProcessStore<TState>(connectionString, tableName)` | `PostgreSqlProcessStore<TState>` | `Npgsql` |
| `Storage.SqlServer` | `AddSqlServerProcessStore<TState>(connectionString, tableName)` | `SqlServerProcessStore<TState>` | `Microsoft.Data.SqlClient` |
| `Storage.Sqlite` | `AddSqliteProcessStore<TState>(connectionString, tableName)` | `SqliteProcessStore<TState>` | `Microsoft.Data.Sqlite` |
| `Storage.MySql` | `AddMySqlProcessStore<TState>(connectionString, tableName)` | `MySqlProcessStore<TState>` | `MySqlConnector` |
| `Storage.MariaDb` | `AddMariaDbProcessStore<TState>(connectionString, tableName)` | `MariaDbProcessStore<TState>` | `MySqlConnector` |
| `Storage.Oracle` | `AddOracleProcessStore<TState>(connectionString, tableName)` | `OracleProcessStore<TState>` | `Oracle.ManagedDataAccess.Core` |

---

## Exception Taxonomy

All exceptions inherit from `ProcessException` in `EricksonLopez.Processes.Abstractions`.

```
ProcessException (base)
├── ProcessNotFoundException         — Instance not found in storage
├── ConcurrencyConflictException     — OCC revision mismatch (CAS failed)
├── InvalidProcessTransitionException — Attempted forbidden state transition
└── CompensationFailedException      — A saga compensation step failed
```

| Exception | Properties | When Thrown |
| :--- | :--- | :--- |
| `ProcessNotFoundException` | `ProcessId?` | `GetByIdAsync` returns `null` and `canInitiate = false` in `ExecuteAsync`. |
| `ConcurrencyConflictException` | `ProcessId?`, `ExpectedRevision` | `SaveAsync` returns `ConcurrencyConflict` after all OCC retries are exhausted. |
| `InvalidProcessTransitionException` | `CurrentStatus`, `AttemptedStatus` | An invalid or forbidden process state transition was attempted. |
| `CompensationFailedException` | `StepName` | A saga compensation step failed during execution. |
| `ProcessException` | `ProcessId?` | Base type; thrown for unrecoverable coordinator errors. |
