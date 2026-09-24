# API Technical Reference — EricksonLopez.Processes

Official detailed Microsoft Learn-style technical documentation for all public types and methods of `EricksonLopez.Processes` in .NET 10. All types and signatures have been validated against compiled library assemblies and Native AOT / trimming compatibility.

---

## Namespace Index

- [EricksonLopez.Processes.Abstractions](#namespace-ericksonlopezprocessesabstractions)
  - [ProcessId](#processid)
  - [CorrelationId](#correlationid)
  - [CausationId](#causationid)
  - [MessageId](#messageid)
  - [ProcessType](#processtype)
  - [ProcessVersion](#processversion)
  - [Revision](#revision)
  - [CompositeCorrelationKey](#compositecorrelationkey)
  - [ProcessInstance&lt;TState&gt;](#processinstancetstate)
  - [ProcessEffect](#processeffect)
  - [CompensationStep](#compensationstep)
  - [CompensationAction](#compensationaction)
  - [IProcessStore&lt;TState&gt;](#iprocessstoretstate)
- [EricksonLopez.Processes](#namespace-ericksonlopezprocesses)
  - [ProcessCoordinator&lt;TState&gt;](#processcoordinatortstate)
  - [ProcessTransitionResult&lt;TState&gt;](#processtransitionresulttstate)
  - [ProcessContext](#processcontext)
  - [ProcessStateMigrationPipeline](#processstatemigrationpipeline)
  - [ProcessDiagnostics](#processdiagnostics)
- [EricksonLopez.Processes.DependencyInjection](#namespace-ericksonlopezprocessesdependencyinjection)
- [Integrations and Dispatchers](#namespace-integrations-and-dispatchers)
  - [EventProcessDispatcher](#eventprocessdispatcher)
  - [MediatorProcessDispatcher](#mediatorprocessdispatcher)
  - [OutboxProcessDispatcher](#outboxprocessdispatcher)
  - [ProcessEventsIdentifierExtensions](#processeventsidentifierextensions)

---

# Namespace `EricksonLopez.Processes.Abstractions`

## `ProcessId`

```csharp
public readonly record struct ProcessId : 
    IComparable<ProcessId>, IComparable, ISpanFormattable, IFormattable, 
    ISpanParsable<ProcessId>, IParsable<ProcessId>, IEquatable<ProcessId>
```

Immutable primary key for a process instance wrapping a `Guid` value (UUIDv7 in .NET 10).

### Methods and Factories

#### `ProcessId.NewId()`
- **Signature**: `public static ProcessId NewId()`
- **Returns**: A time-ordered `ProcessId` (UUIDv7).
- **When to use**: When initiating a new process instance that is not derived from an external `Guid`.

#### `ProcessId.From(Guid value)` / `ProcessId.FromGuid(Guid value)`
- **Signature**: `public static ProcessId From(Guid value)`
- **Parameters**: `value` (`Guid`) — Underlying identifier.
- **Returns**: `ProcessId` instance with the specified value.
- **Exceptions**: None (stack-allocated, zero-allocation).

#### `ProcessId.Parse(string s)` / `ProcessId.TryParse(string s, IFormatProvider? provider, out ProcessId result)`
- **Signature**: `public static bool TryParse(string? s, IFormatProvider? provider, out ProcessId result)`
- **Parameters**: `s` (`string?`) — String representing a valid Guid; `provider` — Optional format provider; `result` — Output parameter.
- **Returns**: `true` if parsing succeeded; otherwise `false`.

---

## `CorrelationId`

```csharp
public readonly record struct CorrelationId : 
    IComparable<CorrelationId>, IComparable, ISpanFormattable, IFormattable, 
    ISpanParsable<CorrelationId>, IParsable<CorrelationId>, IEquatable<CorrelationId>
```

Text-based (`string`) business correlation identifier. Used to correlate incoming events with a process instance.

### Main Methods

#### `CorrelationId.FromString(string value)` / `CorrelationId.From(string value)`
- **Signature**: `public static CorrelationId FromString(string value)`
- **Parameters**: `value` (`string`) — Business identifier string (e.g. `"ORD-12345"`). Does not apply hashing; preserves raw textual value.
- **Returns**: `CorrelationId` value object.

#### `CorrelationId.FromGuid(Guid value)` / `CorrelationId.From(Guid value)`
- **Signature**: `public static CorrelationId FromGuid(Guid value)`
- **Parameters**: `value` (`Guid`) — Converts Guid to its canonical string representation.

---

## `Revision`

```csharp
public readonly record struct Revision : 
    IComparable<Revision>, IComparable, ISpanFormattable, IFormattable, 
    ISpanParsable<Revision>, IParsable<Revision>, IEquatable<Revision>
```

Monotonic Optimistic Concurrency Control (OCC CAS) token represented by a 64-bit integer (`long`).

### Properties and Methods

- `Revision.None`: Represents revision `0` (in-memory pre-save instance, not yet committed to storage).
- `Revision.Initial`: Represents revision `1` (first committed version).
- `Revision Next()`: Returns a new `Revision` with value `Value + 1`.
- `long ToInt64()`: Returns the primitive `long` value.

---

## `CompositeCorrelationKey`

```csharp
public readonly record struct CompositeCorrelationKey : IEquatable<CompositeCorrelationKey>
```

Deterministic builder of `CorrelationId` from multiple business key components using SHA-256 hashing (UUIDv5).

### Main Methods

#### `CompositeCorrelationKey.From<T1, T2>(T1 part1, T2 part2)`
- **Signature**: `public static CompositeCorrelationKey From<T1, T2>(T1 part1, T2 part2)`
- **Overloads**: Supports 2, 3, and 4 generic parts (`From<T1, T2, T3>`, `From<T1, T2, T3, T4>`).
- **Returns**: `CompositeCorrelationKey` struct.

#### `CompositeCorrelationKey.ToCorrelationId()`
- **Signature**: `public CorrelationId ToCorrelationId()`
- **Returns**: Deterministic `CorrelationId` generated from the SHA-256 hash of joined parts.
- **Remarks**: Ensures events sharing the same compound business keys route to the exact same process in partitioned stores.

---

## `ProcessInstance<TState>`

```csharp
public sealed record ProcessInstance<TState> where TState : notnull
```

Immutable container wrapping domain state along with all persistence and lifecycle metadata.

### Properties
- `Id` (`ProcessId`): Primary key.
- `CorrelationId` (`CorrelationId`): Correlation key.
- `Type` (`ProcessType`): Logical process type name.
- `Version` (`ProcessVersion`): Schema version.
- `Status` (`ProcessStatus`): Current lifecycle status (`Initialized`, `Running`, `Suspended`, `Completed`, `Compensating`, `Compensated`, `Failed`).
- `Revision` (`Revision`): OCC token.
- `CreatedAt`, `UpdatedAt` (`DateTimeOffset`): UTC timestamps.
- `CompletedAt` (`DateTimeOffset?`): Completion timestamp, non-null only in terminal statuses.
- `State` (`TState`): Domain state payload.
- `RecordedCompensations` (`IReadOnlyList<CompensationStep>`): Compensatable step stack.

### Methods

#### `ProcessInstance<TState>.Create(...)`
- **Signature**: `public static ProcessInstance<TState> Create(ProcessId id, ProcessType type, ProcessVersion version, CorrelationId correlationId, TState initialState, DateTimeOffset now)`
- **Purpose**: Canonical factory to initialize a new instance in `ProcessStatus.Initialized` status with `Revision.None`.

#### `ProcessInstance<TState>.Advance(...)`
- **Signature**: `public ProcessInstance<TState> Advance(TState newState, ProcessStatus newStatus, DateTimeOffset now, IReadOnlyList<CompensationStep>? newCompensations = null, int maxCompensations = 100)`
- **Purpose**: Produces a new immutable instance incrementing `Revision.Next()`, updating `UpdatedAt`, and appending new compensatable steps.

#### `ProcessInstance<TState>.AdvanceCompensation(...)`
- **Signature**: `public ProcessInstance<TState> AdvanceCompensation(TState newState, ProcessStatus newStatus, DateTimeOffset now)`
- **Purpose**: Advances state during the rollback lifecycle (`Compensating` or `Compensated`) preserving revision auditability.

---

## `ProcessEffect`

```csharp
public abstract record ProcessEffect : IEquatable<ProcessEffect>
```

Discriminated union of pure side-effect intentions emitted by state transitions.

### Variants
1. `ProcessEffect.Command`: Intent to execute a command (`CommandPayload`, `CommandType`).
2. `ProcessEffect.Event`: Intent to publish an event (`EventPayload`, `EventType`).
3. `ProcessEffect.ScheduleTimeout`: Intent to schedule a timer (`Delay`, `TimeoutTrigger`, `TriggerType`).
4. `ProcessEffect.Compensation`: Intent to execute a rollback action (`CompensationAction`).

### Typed Factories
- `ProcessEffect.CreateCommand<T>(T payload, string? commandType = null)`
- `ProcessEffect.CreateEvent<T>(T payload, string? eventType = null)`
- `ProcessEffect.CreateTimeout<T>(TimeSpan delay, T trigger, string? triggerType = null)`
- `ProcessEffect.CreateCompensation(CompensationAction action)`
- `ProcessEffect.CreateCompensation<T>(string stepName, T payload)`

---

## `IProcessStore<TState>`

```csharp
public interface IProcessStore<TState> where TState : notnull
```

Storage port decoupled from the underlying storage infrastructure.

### Methods
- `ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken ct = default)`: Load by primary key. Returns `null` if not found.
- `ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken ct = default)`: Atomically saves verifying OCC CAS revision.
- `ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken ct = default)`: Verifies existence without deserializing the state payload.
- `ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken ct = default)`: Default Interface Method (DIM) for secondary correlation lookup.

---

# Namespace `EricksonLopez.Processes`

## `ProcessCoordinator<TState>`

```csharp
public sealed class ProcessCoordinator<TState> where TState : notnull
```

Core orchestration and transition execution engine of the library.

### Constructor
```csharp
public ProcessCoordinator(
    IProcessStore<TState> store,
    ProcessCoordinatorOptions? options = null,
    TimeProvider? timeProvider = null,
    Func<int, TimeSpan>? backoffStrategy = null)
```

### Main Methods

#### `ExecuteAsync<TEvent>(...)`
- **Signature**:
  ```csharp
  public ValueTask<ProcessExecutionResult<TState>> ExecuteAsync<TEvent>(
      IProcessHandler<TState, TEvent> handler,
      IProcessCorrelation<TEvent> correlation,
      TEvent eventMessage,
      Func<TEvent, TState>? initialStateFactory = null,
      bool canInitiate = false,
      CancellationToken cancellationToken = default)
  ```
- **Parameters**:
  - `handler`: Process handler implementing `IProcessHandler<TState, TEvent>`.
  - `correlation`: Identifier extractor for the event.
  - `eventMessage`: The incoming event message.
  - `initialStateFactory`: Optional factory to initialize domain state when the instance does not exist and `canInitiate == true`.
  - `canInitiate`: `true` if this event can start a new process instance.
  - `cancellationToken`: Cooperative cancellation token.
- **Returns**: `ValueTask<ProcessExecutionResult<TState>>` containing the updated instance and emitted side effects.
- **Exceptions**:
  - `ProcessNotFoundException`: Thrown when instance does not exist and `canInitiate == false`.
  - `ConcurrencyConflictException`: Thrown when OCC retries defined in `options.MaxConcurrencyRetries` are exhausted.
  - `InvalidProcessTransitionException`: Thrown upon illegal state transitions.
- **Remarks**: Executes the linear OCC retry loop with hydration and atomic persistence.

#### `CompensateAsync<TSaga>(...)`
- **Signature**:
  ```csharp
  public ValueTask<ProcessExecutionResult<TState>> CompensateAsync<TSaga>(
      ProcessId processId,
      IReadOnlyList<CompensationStep> recordedSteps,
      TSaga saga,
      CancellationToken cancellationToken = default)
      where TSaga : IProcess<TState>, ICompensationHandler<TState>
  ```
- **Purpose**: Executes rollback of compensatable steps in strict reverse order (LIFO).

#### `DefaultBackoffStrategy(int attempt)`
- **Signature**: `public static TimeSpan DefaultBackoffStrategy(int attempt)`
- **Returns**: Linearly calculated `TimeSpan` (`10ms * attempt`).

---

## `ProcessTransitionResult<TState>`

```csharp
public sealed record ProcessTransitionResult<TState> where TState : notnull
```

Mandatory immutable output of every `HandleAsync` and `CompensateAsync` transition method.

### Factory Methods
- `Advance(TState state, ProcessStatus status = ProcessStatus.Running, IEnumerable<ProcessEffect>? effects = null, IEnumerable<CompensationStep>? recordedCompensations = null)`: Advances the process with new state and optional effects.
- `Complete(TState state, IEnumerable<ProcessEffect>? effects = null, IEnumerable<CompensationStep>? recordedCompensations = null)`: Marks successful completion in `Completed` status.
- `Fail(TState state, string? reason = null, IEnumerable<ProcessEffect>? effects = null)`: Transitions to `Failed` with an optional error reason.
- `Suspend(TState state, IEnumerable<ProcessEffect>? effects = null)`: Temporarily suspends the process (`ProcessStatus.Suspended`).
- `Compensate(TState state, IEnumerable<CompensationAction> compensationActions)`: Triggers Saga compensation passing actions to rollback.
- `Compensated(TState state, IEnumerable<ProcessEffect>? effects = null)`: Marks compensation completed successfully (`ProcessStatus.Compensated`).
- `Unchanged(TState state, ProcessStatus currentStatus)`: Returns the current state without modifications.

---

## `ProcessContext`

```csharp
public sealed class ProcessContext
```

Execution context container passed to process handlers.

### Properties
- `ProcessId` (`ProcessId`): Running instance ID.
- `CorrelationId` (`CorrelationId`): Business correlation ID.
- `CausationId` (`CausationId`): Causation message ID.
- `MessageId` (`MessageId`): Message ID for deduplication.
- `Now` (`DateTimeOffset`): Current UTC timestamp provided by the configured `TimeProvider`.
- `TimeProvider` (`TimeProvider`): Injected time abstraction.
- `Items` (`IReadOnlyDictionary<string, object?>`): Ambient metadata dictionary (e.g. TenantId).
- `CancellationToken` (`CancellationToken`): Operation cancellation token.

---

## `ProcessStateMigrationPipeline`

```csharp
public static class ProcessStateMigrationPipeline
```

Fluent builder to construct linear schema migration pipelines.

### Methods
- `ProcessStateMigrationPipeline.Create<TInitialState>(ProcessVersion initialVersion)`: Starts the builder for initial state.
- `AddStep<TNextState>(IProcessStateMigrator<TCurrent, TNextState> migrator)`: Appends a typed migration step validating version compatibility.
- `AddStep<TNextState>(ProcessVersion targetVersion, Func<TCurrent, TNextState> transformer)`: Appends an inline transformer function.
- `Build<TFinalState>()`: Compiles the pipeline into an `IProcessStateMigrator<object, TFinalState>`.

---

# Namespace `EricksonLopez.Processes.DependencyInjection`

## `ServiceCollectionExtensions`

```csharp
public static class ServiceCollectionExtensions
```

- `AddProcesses(this IServiceCollection services)`: Registers base process services and the `ProcessRegistry` catalog.
- `AddProcessCoordinator<TState>(this IServiceCollection services, Action<ProcessCoordinatorOptions>? configureOptions = null)`: Registers `ProcessCoordinator<TState>` with specified options.

---

# Namespace Integrations and Dispatchers

## `EventProcessDispatcher` (`EricksonLopez.Processes.Events`)
- `DispatchEffectAsync(ProcessEffect effect, ProcessId processId, CancellationToken ct)`: Dispatches a `ProcessEffect.Event` to the event bus.
- `DispatchEffectsAsync(IEnumerable<ProcessEffect> effects, ProcessId processId, CancellationToken ct)`: Dispatches a batch of effects.

## `MediatorProcessDispatcher` (`EricksonLopez.Processes.Mediator`)
- `DispatchEffectAsync(ProcessEffect effect, ProcessId processId, CancellationToken ct)`: Dispatches a `ProcessEffect.Command` to `IMediator`.
- Property `Action<object, ProcessId, string?>? OnUnrecognizedPayload`: Callback to log or redirect payloads not matching expected command contracts.

## `OutboxProcessDispatcher` (`EricksonLopez.Processes.Outbox`)
- `DispatchEffectAsync(ProcessEffect effect, ProcessId processId, IOutboxTransactionContext transaction, CancellationToken ct)`: Durably enqueues any `ProcessEffect` (`Command`, `Event`, `Timeout`, `Compensation`) into the Outbox transaction.
- `DispatchEffectsAsync(...)`: Atomically saves multiple effects to the Outbox.

## `ProcessEventsIdentifierExtensions` (`EricksonLopez.Processes.Events`)
- `CorrelationId.ToEventsCorrelationId()`: Converts `EricksonLopez.Processes.Abstractions.CorrelationId` to `EricksonLopez.Events.Identifiers.CorrelationId`.
- `CorrelationId.ToProcessesCorrelationId()`: Converts `EricksonLopez.Events.Identifiers.CorrelationId` to `EricksonLopez.Processes.Abstractions.CorrelationId`.
- `CausationId.ToEventsCausationId()`: Converts `EricksonLopez.Processes.Abstractions.CausationId` to `EricksonLopez.Events.Identifiers.CausationId`.
- `CausationId.ToProcessesCausationId()`: Converts `EricksonLopez.Events.Identifiers.CausationId` to `EricksonLopez.Processes.Abstractions.CausationId`.

---

# Namespace `EricksonLopez.Processes.SystemTextJson`

## `SystemTextJsonProcessStateSerializer<TState>`
Provides Native AOT, reflection-free state serialization implementing `IProcessStateSerializer<TState>`:
- `SystemTextJsonProcessStateSerializer(JsonTypeInfo<TState> jsonTypeInfo)`: Initializes serializer with source-generated JSON metadata.
- `SystemTextJsonProcessStateSerializer(JsonTypeInfo<TState> jsonTypeInfo, int? maxPayloadSizeBytes)`: Initializes serializer with an optional maximum byte payload guard.
- `byte[] Serialize(TState state)`: Serializes domain state to UTF-8 JSON bytes.
- `TState Deserialize(ReadOnlySpan<byte> bytes)`: Deserializes UTF-8 JSON bytes to domain state.

## `ProcessJsonSerializerOptions`
Static helper methods configuring standard `JsonSerializerOptions` with all strongly typed identifier converters:
- `ProcessJsonSerializerOptions.Default`: Read-only pre-configured options instance.
- `ProcessJsonSerializerOptions.Create()`: Creates a new `JsonSerializerOptions` instance with all 7 strongly typed converters registered.
- `ProcessJsonSerializerOptions.Configure(JsonSerializerOptions options)`: Registers all process converters onto an existing `JsonSerializerOptions` instance.

## Specialized JSON Converters
Available for custom `JsonSerializerOptions` pipelines:
- `ProcessIdJsonConverter`: Custom `JsonConverter<ProcessId>` for Guid-backed process identifiers.
- `ProcessTypeJsonConverter`: Custom `JsonConverter<ProcessType>` for string-backed process types.
- `ProcessVersionJsonConverter`: Custom `JsonConverter<ProcessVersion>` for integer version tokens.
- `RevisionJsonConverter`: Custom `JsonConverter<Revision>` for monotonic CAS 64-bit revision numbers.
- `CorrelationIdJsonConverter`: Custom `JsonConverter<CorrelationId>` for string correlation tokens.
- `CausationIdJsonConverter`: Custom `JsonConverter<CausationId>` for string causation tokens.
- `MessageIdJsonConverter`: Custom `JsonConverter<MessageId>` for message identity tokens.
