# Exhaustive Public API Inventory — EricksonLopez.Processes

This document constitutes the **official inventory and single source of truth** for the public API surface of `EricksonLopez.Processes`, extracted directly from compiled assemblies across the **Core Library** and **Infrastructure** layers. No documentation guide, sample, or showcase example may use elements outside this inventory.

---

## 1. Strongly Typed Identifiers and Value Objects

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `ProcessId` | `EricksonLopez.Processes.Abstractions` | Immutable primary key for a process instance wrapping a `Guid` (UUIDv7). Implements `ISpanParsable` and `ISpanFormattable`. | BCL (`Guid`, `ISpanParsable`) | Unique identification of process and saga instances. | Basic | Yes (Level 01, 02) |
| `CorrelationId` | `EricksonLopez.Processes.Abstractions` | Business correlation identifier based on text (`string`). Correlates inbound messages with active process instances. | BCL (`string`, `ISpanParsable`) | Routing inbound events to the target process instance. | Basic | Yes (Level 01, 02, 05) |
| `CausationId` | `EricksonLopez.Processes.Abstractions` | Causal identifier (`string`) for provenance tracking in distributed event chains. | BCL (`string`, `ISpanParsable`) | Traceability and auditing of which command/event caused a state transition. | Intermediate | Yes (Level 02, 09) |
| `MessageId` | `EricksonLopez.Processes.Abstractions` | Unique message identifier for idempotency control and deduplication. | BCL (`string`, `ISpanParsable`) | Idempotent processing of duplicate messages. | Intermediate | Yes (Level 02) |
| `ProcessType` | `EricksonLopez.Processes.Abstractions` | Logical process type discriminator (e.g. `"order.fulfillment"`). | BCL (`string`, `ISpanParsable`) | Registration, polymorphic discrimination, and storage partitioning. | Basic | Yes (Level 01, 02, 10) |
| `ProcessVersion` | `EricksonLopez.Processes.Abstractions` | Monotonic integer representing process state schema version. | BCL (`int`, `ISpanParsable`) | Schema migrations and version coexistence management. | Intermediate | Yes (Level 02, 10) |
| `Revision` | `EricksonLopez.Processes.Abstractions` | Monotonic 64-bit integer (`long`) Optimistic Concurrency Control (OCC CAS) token. | BCL (`long`, `ISpanParsable`) | Atomic revision validation against concurrent mutations. | Intermediate | Yes (Level 02, 05, 06) |
| `CompositeCorrelationKey` | `EricksonLopez.Processes.Abstractions` | Deterministic `CorrelationId` builder using SHA-256 hashing (UUIDv5) from 2, 3, or 4 business key components. | BCL (`SHA256`) | Processes correlated by compound keys (e.g. Tenant + OrderId). | Intermediate | Yes (Level 05) |

---

## 2. State, Lifecycle, and Persistence

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `IProcessState` | `EricksonLopez.Processes.Abstractions` | Marker interface for immutable domain state contracts. | BCL | Defining immutable state records (`record`). | Basic | Yes (Level 01, 03, 10) |
| `ProcessStatus` | `EricksonLopez.Processes.Abstractions` | Lifecycle status enum (`Initialized`, `Running`, `Suspended`, `Completed`, `Compensating`, `Compensated`, `Failed`). | BCL | Evaluating transitions and controlling state-machine flow. | Basic | Yes (Level 01, 03, 06) |
| `ProcessInstance<TState>` | `EricksonLopez.Processes.Abstractions` | Persistent wrapper encapsulating state, identifiers, metadata, and revisions. Exposes `Create`, `Advance`, and `AdvanceCompensation`. | `ProcessId`, `CorrelationId`, `ProcessType`, `ProcessVersion`, `Revision`, `ProcessStatus` | Canonical in-memory and persistence wrapper for processes. | Intermediate | Yes (Level 01, 02, 08) |
| `ProcessStateRecord` | `EricksonLopez.Processes.Abstractions` | Flat relational record for direct database column mapping. | BCL (`DateTimeOffset`) | Relational persistence adapters (ADO.NET). | Advanced | Yes (Level 08) |
| `ProcessSaveResult` | `EricksonLopez.Processes.Abstractions` | Persistence result enum (`Success`, `ConcurrencyConflict`, `NotFound`, `PersistenceError`). | BCL | Communicating atomic store save outcomes back to the coordinator. | Intermediate | Yes (Level 06) |
| `IProcessStore<TState>` | `EricksonLopez.Processes.Abstractions` | Storage port (`GetByIdAsync`, `SaveAsync`, `ExistsAsync`, `GetByCorrelationIdAsync`). | `ProcessInstance<TState>`, `ProcessSaveResult` | Decoupled persistence abstraction over relational database engines. | Intermediate | Yes (Level 01, 08) |
| `IProcessStateSerializer<TState>` | `EricksonLopez.Processes.Abstractions` | Transport-agnostic binary serialization port (`byte[]`). | BCL (`ReadOnlySpan<byte>`) | Trimming-safe, Native AOT-ready state serialization. | Intermediate | Yes (Level 02, 08) |
| `ISagaSnapshotRepository<TState>` | `EricksonLopez.Processes.Abstractions` | Optional port for persisting and retrieving intermediate saga snapshots. | `ProcessId`, `Revision` | Performance optimization for long-running sagas with high step counts. | Advanced | Yes (Level 08) |
| `IProcessStateMigrator<TFrom, TTo>` | `EricksonLopez.Processes.Abstractions` | Contract for a synchronous schema migration step between consecutive versions. | `ProcessVersion` | Zero-downtime hot state contract evolution without data loss. | Advanced | Yes (Level 10) |

---

## 3. Side Effects and Compensations

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `ProcessEffect` | `EricksonLopez.Processes.Abstractions` | Discriminated union of side-effect intentions (`Command`, `Event`, `ScheduleTimeout`, `Compensation`). Exposes typed factories (`CreateCommand<T>`, `CreateEvent<T>`, `CreateTimeout<T>`, `CreateCompensation`). | BCL (`TimeSpan`, `object`) | Returning pure intentions from process transition handlers. | Intermediate | Yes (Level 03, 04, 05) |
| `ProcessEffect.Command` | `EricksonLopez.Processes.Abstractions` | Intent to dispatch a command (e.g. Mediator). Exposes `GetPayload<T>()` and `TryGetPayload<T>()`. | `ProcessEffect` | In-process or outbox command dispatching. | Intermediate | Yes (Level 04, 05) |
| `ProcessEffect.Event` | `EricksonLopez.Processes.Abstractions` | Intent to publish an integration event. Exposes `GetPayload<T>()` and `TryGetPayload<T>()`. | `ProcessEffect` | Publishing notifications to event buses or external message brokers. | Intermediate | Yes (Level 04, 05) |
| `ProcessEffect.ScheduleTimeout` | `EricksonLopez.Processes.Abstractions` | Intent to schedule a wake-up timer. Exposes `GetTrigger<T>()` and `TryGetTrigger<T>()`. | `ProcessEffect`, `TimeSpan` | Sagas awaiting external confirmation or expiration timeouts. | Advanced | Yes (Level 03, 05) |
| `ProcessEffect.Compensation` | `EricksonLopez.Processes.Abstractions` | Intent encapsulating a rollback action (`CompensationAction`). | `CompensationAction` | Explicit rollback triggers dispatched to downstream services. | Advanced | Yes (Level 03, 05) |
| `CompensationStep` | `EricksonLopez.Processes.Abstractions` | Immutable record of a completed compensatable step appended to the LIFO stack. Exposes `ExtractPayload<T>()` and `TryExtractPayload<T>()`. | BCL (`DateTimeOffset`) | Recording prior successful steps to enable rollback upon failure. | Advanced | Yes (Level 03, 05) |
| `CompensationAction` | `EricksonLopez.Processes.Abstractions` | Directive emitted to execute a specific compensation step. Exposes `ExtractPayload<T>()` and `TryExtractPayload<T>()`. | BCL | Instructing `ICompensationHandler` which step to revert. | Advanced | Yes (Level 03, 06) |

---

## 4. Correlation and Routing

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `IProcessCorrelation<TEvent>` | `EricksonLopez.Processes.Abstractions` | Contract for extracting `ProcessId`, `CorrelationId`, and `CausationId` from an incoming message. | `ProcessId`, `CorrelationId`, `CausationId` | Decoupling message structure from process identifiers. | Basic | Yes (Level 01, 03, 05) |

---

## 5. Domain Exceptions

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `ProcessException` | `EricksonLopez.Processes.Abstractions` | Base exception for all process ecosystem exceptions. Exposes `ProcessId?`. | `ProcessId` | Catching library-specific errors uniformly. | Basic | Yes (Level 06) |
| `ProcessNotFoundException` | `EricksonLopez.Processes.Abstractions` | Thrown when an instance is not found in the store and `canInitiate == false`. | `ProcessException` | Handling orphan or out-of-order events from message brokers. | Basic | Yes (Level 06) |
| `ConcurrencyConflictException` | `EricksonLopez.Processes.Abstractions` | Thrown when Optimistic Concurrency Control (OCC CAS) retries are exhausted. Exposes `ExpectedRevision`. | `ProcessException`, `Revision` | Signaling high contention to trigger external backoff or dead-lettering. | Intermediate | Yes (Level 05, 06) |
| `InvalidProcessTransitionException` | `EricksonLopez.Processes.Abstractions` | Thrown when an illegal state-machine transition is attempted. Exposes `CurrentStatus` and `AttemptedStatus`. | `ProcessException`, `ProcessStatus` | Protecting lifecycle invariants (e.g. transitioning out of a terminal state). | Intermediate | Yes (Level 06) |
| `CompensationFailedException` | `EricksonLopez.Processes.Abstractions` | Thrown when a saga rollback step fails. Exposes `StepName`. | `ProcessException` | Signaling critical rollback failures requiring human intervention or dead-letter queues. | Advanced | Yes (Level 06) |

---

## 6. Declarative Attributes

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `[ProcessDefinition]` | `EricksonLopez.Processes.Abstractions` | Annotates a class as a Process Manager declaring its `ProcessType` and version. | BCL (`Attribute`) | Compile-time discovery via Roslyn Source Generator. | Basic | Yes (Level 01, 10) |
| `[SagaDefinition]` | `EricksonLopez.Processes.Abstractions` | Annotates a class as a Saga declaring its `ProcessType` and version. | BCL (`Attribute`) | Compile-time discovery of compensatable sagas by Source Generator. | Basic | Yes (Level 03, 10) |
| `[ProcessHandler]` | `EricksonLopez.Processes.Abstractions` | Annotates an event handler method and defines whether it can initiate the instance (`CanInitiate`). | BCL (`Attribute`) | Declarative configuration of process initiation events. | Basic | Yes (Level 01, 10) |
| `[ProcessType]` | `EricksonLopez.Processes.Abstractions` | Defines the logical process type discriminator at class level. | BCL (`Attribute`) | Associating alternate type metadata. | Intermediate | Yes (Level 02) |

---

## 7. Execution and Orchestration Engine (`EricksonLopez.Processes`)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `IProcess<TState>` | `EricksonLopez.Processes` | Contract defining a Process Manager, exposing `Type` (`ProcessType`) and `Version` (`ProcessVersion`). | `ProcessType`, `ProcessVersion` | Marking and formally identifying processes in the DI registry. | Basic | Yes (Level 01, 03) |
| `ISaga<TState>` | `EricksonLopez.Processes` | Contract defining a Saga (inherits `IProcess<TState>`), requiring compensation capabilities. | `IProcess<TState>` | Distributed workflows requiring LIFO rollback upon failure. | Intermediate | Yes (Level 03) |
| `IProcessHandler<TState, TEvent>` | `EricksonLopez.Processes` | Contract for forward event handling (`HandleAsync`). | `ProcessTransitionResult<TState>`, `ProcessContext` | Pure business logic transitions for each event type. | Basic | Yes (Level 01, 03) |
| `ICompensationHandler<TState>` | `EricksonLopez.Processes` | Contract for rollback execution (`CompensateAsync`). | `ProcessTransitionResult<TState>`, `CompensationAction`, `ProcessContext` | Executing specific rollback steps sequentially. | Advanced | Yes (Level 03, 06) |
| `ProcessContext` | `EricksonLopez.Processes` | Ambient execution context (`ProcessId`, `CorrelationId`, `CausationId`, `MessageId`, `Now`, `TimeProvider`, `Items`, `CancellationToken`). | BCL (`TimeProvider`, `CancellationToken`) | Passing deterministic time, cancellation tokens, and metadata to handlers. | Intermediate | Yes (Level 02) |
| `ProcessCoordinator<TState>` | `EricksonLopez.Processes` | Core orchestration engine. Coordinates hydration, OCC retry loop, pure execution, persistence, and effect harvesting. | `IProcessStore<TState>`, `ProcessCoordinatorOptions`, `TimeProvider` | Primary entry point for process and saga execution in host applications. | Intermediate | Yes (Level 01, 02, 03, 05) |
| `ProcessCoordinatorOptions` | `EricksonLopez.Processes` | Configurable coordinator options (`MaxConcurrencyRetries`, `InitialBackoffDelay`, `MaxCompensations`). | BCL (`TimeSpan`) | Fine-tuning OCC retry loops and safety thresholds. | Basic | Yes (Level 02, 05) |
| `ProcessTransitionResult<TState>` | `EricksonLopez.Processes` | Immutable transition outcome record. Factory methods: `Advance`, `Complete`, `Fail`, `Suspend`, `Compensate`, `Compensated`, `Unchanged`. | `ProcessStatus`, `ProcessEffect`, `CompensationStep` | Mandatory return value from `HandleAsync` and `CompensateAsync`. | Basic | Yes (Level 01, 03, 06) |
| `ProcessExecutionResult<TState>` | `EricksonLopez.Processes` | Record holding outcome of `ExecuteAsync` or `CompensateAsync` (`Instance`, `Effects`, `RecordedCompensations`, `SaveResult`, `IsSuccess`). | `ProcessInstance<TState>`, `ProcessEffect`, `ProcessSaveResult` | Consuming updated state and emitted effects in the outer application layer. | Intermediate | Yes (Level 01, 04, 05) |
| `ProcessDiagnostics` | `EricksonLopez.Processes` | OpenTelemetry metrics and tracing (`ActivitySource` and `Meter`). Methods: `RecordProcessStarted`, `RecordProcessCompleted`, `RecordProcessFailed`, `RecordProcessCompensated`, `RecordConcurrencyConflict`, `RecordTransitionDuration`. | BCL (`System.Diagnostics.DiagnosticSource`) | Zero-allocation production observability and telemetry. | Intermediate | Yes (Level 07) |
| `ProcessStateMigrationPipeline` | `EricksonLopez.Processes` | Fluent factory for assembling schema migration pipelines (`Create<TInitial>`). | `ProcessVersion` | Chaining multi-cycle state schema migrations (v1 -> v2 -> v3). | Advanced | Yes (Level 10) |
| `ProcessStateMigrationPipelineBuilder<T>` | `EricksonLopez.Processes` | Fluent builder chaining steps with `AddStep` and compiling via `Build()`. | `IProcessStateMigrator` | Validated chaining of sequential state transformations. | Advanced | Yes (Level 10) |
| `IProcessRegistry` / `ProcessRegistry` | `EricksonLopez.Processes` | Catalog of known process types in the dependency injection container. | `ProcessType`, `ProcessVersion` | Runtime verification and metadata queries for registered processes. | Intermediate | Yes (Level 02, 10) |

---

## 8. Dependency Injection (`EricksonLopez.Processes.DependencyInjection`)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `ServiceCollectionExtensions` | `EricksonLopez.Processes.DependencyInjection` | Extension methods for `IServiceCollection`: `AddProcesses()` and `AddProcessCoordinator<TState>()`. | `IServiceCollection`, `ProcessCoordinatorOptions` | Standard service registration in `Program.cs` for .NET hosts. | Basic | Yes (Level 01, 02) |

---

## 9. System.Text.Json Serialization (`EricksonLopez.Processes.SystemTextJson`)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `SystemTextJsonProcessStateSerializer<TState>` | `EricksonLopez.Processes.SystemTextJson` | Optimized, trimming-safe, and AOT-ready implementation of `IProcessStateSerializer<TState>` with `JsonTypeInfo<TState>`. | `System.Text.Json` | High-performance JSON serialization for relational database storage. | Basic | Yes (Level 02, 10) |
| `ProcessJsonSerializerOptions` | `EricksonLopez.Processes.SystemTextJson` | Configures or creates `JsonSerializerOptions` equipped with converters for `ProcessId`, `CorrelationId`, `CausationId`, `MessageId`, `ProcessType`, `ProcessVersion`, and `Revision`. | `System.Text.Json` | Seamless JSON serialization across Web APIs and external serializers. | Intermediate | Yes (Level 02) |
| JSON Converters (`ProcessIdJsonConverter`, etc.) | `EricksonLopez.Processes.SystemTextJson` | Individual converters for each identifier value object. | `JsonConverter<T>` | AOT-safe, reflection-free JSON serialization and deserialization. | Advanced | Yes (Level 02) |

---

## 10. Messaging and Infrastructure Integrations

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `IEventProcessDispatcher` | `EricksonLopez.Processes.Events` | Dispatch port for `ProcessEffect.Event` effects. | `ProcessEffect`, `ProcessId` | Dispatching integration events to `EricksonLopez.Events`. | Intermediate | Yes (Level 04) |
| `EventProcessDispatcher` | `EricksonLopez.Processes.Events` | Dispatcher implementation publishing events to an `IEventPublisher`. | `IEventProcessDispatcher` | Publishing domain events emitted by process transitions. | Intermediate | Yes (Level 04) |
| `ProcessEventsIdentifierExtensions` | `EricksonLopez.Processes.Events` | Extension methods for zero-copy bidirectional conversion between `Processes` and `Events` identifiers (`ToEventsCorrelationId`, `ToProcessesCorrelationId`, `ToEventsCausationId`, `ToProcessesCausationId`). | `CorrelationId`, `CausationId` | Zero-allocation identity bridges between sibling libraries. | Intermediate | Yes (Level 09) |
| `ProcessEventsServiceCollectionExtensions` | `EricksonLopez.Processes.Events` | DI extension `services.AddProcessEventsDispatcher()`. | `IServiceCollection` | DI registration for event integration. | Basic | Yes (Level 04) |
| `IMediatorProcessDispatcher` | `EricksonLopez.Processes.Mediator` | Dispatch port for `ProcessEffect.Command` effects. | `ProcessEffect`, `ProcessId` | In-process command dispatching via Mediator. | Intermediate | Yes (Level 04) |
| `MediatorProcessDispatcher` | `EricksonLopez.Processes.Mediator` | Dispatcher implementation dispatching commands to `IMediator`. Exposes `OnUnrecognizedPayload` callback. | `IMediatorProcessDispatcher` | Decoupled CQRS command orchestration. | Intermediate | Yes (Level 04) |
| `ProcessMediatorServiceCollectionExtensions` | `Microsoft.Extensions.DependencyInjection` | DI extension `services.AddProcessesMediator()`. | `IServiceCollection` | DI registration for Mediator integration. | Basic | Yes (Level 04) |
| `IProcessOutboxDispatcher` | `EricksonLopez.Processes.Outbox` | Transactional dispatch port for durable effects. | `ProcessEffect`, `IOutboxTransactionContext` | Guaranteeing exactly-once / at-least-once delivery with zero dual-write. | Advanced | Yes (Level 04) |
| `OutboxProcessDispatcher` | `EricksonLopez.Processes.Outbox` | Dispatcher persisting effects into a durable transactional Outbox. | `IProcessOutboxDispatcher` | Reliable dispatch for all effect types (`Command`, `Event`, `Timeout`, `Compensation`). | Advanced | Yes (Level 04) |
| `ProcessOutboxServiceCollectionExtensions` | `Microsoft.Extensions.DependencyInjection` | DI extension `services.AddProcessesOutbox()`. | `IServiceCollection` | DI registration for Outbox integration. | Basic | Yes (Level 04) |

---

## 11. Relational Persistence Storage Engines

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `PostgreSqlProcessStore<TState>` | `EricksonLopez.Processes.Storage.PostgreSql` | `IProcessStore<TState>` implementation for PostgreSQL with JSONB dialect and CAS concurrency control. | `Npgsql` (ADO.NET) | Cloud-native PostgreSQL persistence. | Intermediate | Yes (Level 09) |
| `ProcessPostgreSqlServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.PostgreSql` | DI extension `services.AddPostgreSqlProcessStore<TState>()`. | `IServiceCollection` | DI registration for PostgreSQL. | Basic | Yes (Level 09) |
| `SqlServerProcessStore<TState>` | `EricksonLopez.Processes.Storage.SqlServer` | `IProcessStore<TState>` implementation for Microsoft SQL Server with CAS concurrency control. | `Microsoft.Data.SqlClient` (ADO.NET) | Corporate Azure SQL / SQL Server enterprise deployments. | Intermediate | Yes (Level 09) |
| `ProcessSqlServerServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.SqlServer` | DI extension `services.AddSqlServerProcessStore<TState>()`. | `IServiceCollection` | DI registration for SQL Server. | Basic | Yes (Level 09) |
| `SqliteProcessStore<TState>` | `EricksonLopez.Processes.Storage.Sqlite` | Lightweight embedded `IProcessStore<TState>` implementation for SQLite. | `Microsoft.Data.Sqlite` (ADO.NET) | Embedded applications, edge computing, and local integration tests. | Intermediate | Yes (Level 09) |
| `ProcessSqliteServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.Sqlite` | DI extension `services.AddSqliteProcessStore<TState>()`. | `IServiceCollection` | DI registration for SQLite. | Basic | Yes (Level 09) |
| `MySqlProcessStore<TState>` | `EricksonLopez.Processes.Storage.MySql` | `IProcessStore<TState>` implementation for MySQL with CAS transaction support. | `MySqlConnector` (ADO.NET) | Deployments on MySQL 8.x. | Intermediate | Yes (Level 09) |
| `ProcessMySqlServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.MySql` | DI extension `services.AddMySqlProcessStore<TState>()`. | `IServiceCollection` | DI registration for MySQL. | Basic | Yes (Level 09) |
| `MariaDbProcessStore<TState>` | `EricksonLopez.Processes.Storage.MariaDb` | Dedicated `IProcessStore<TState>` implementation for MariaDB with native dialect syntax. | `MySqlConnector` (ADO.NET) | Deployments on MariaDB Enterprise and Community. | Intermediate | Yes (Level 09) |
| `ProcessMariaDbServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.MariaDb` | DI extension `services.AddMariaDbProcessStore<TState>()`. | `IServiceCollection` | DI registration for MariaDB. | Basic | Yes (Level 09) |
| `OracleProcessStore<TState>` | `EricksonLopez.Processes.Storage.Oracle` | `IProcessStore<TState>` implementation for Oracle Database with CLOB support and CAS transactions. | `Oracle.ManagedDataAccess.Core` (ADO.NET) | Enterprise and banking deployments on Oracle 19c/23c. | Advanced | Yes (Level 09) |
| `ProcessOracleServiceCollectionExtensions` | `EricksonLopez.Processes.Storage.Oracle` | DI extension `services.AddOracleProcessStore<TState>()`. | `IServiceCollection` | DI registration for Oracle. | Basic | Yes (Level 09) |

---

## 12. Testing Utilities (`EricksonLopez.Processes.Testing`)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity Level | Existing Example |
|---|---|---|---|---|---|---|
| `InMemoryProcessStore<TState>` | `EricksonLopez.Processes.Testing` | Thread-safe in-memory store backed by `ConcurrentDictionary` with exclusive locking in `SaveAsync` for rigorous OCC testing. | `IProcessStore<TState>` | Unit tests, ultra-fast integration tests, and interactive showcases. | Basic | Yes (Level 01, 05, 07) |
| `FaultInjectingProcessStore<TState>` | `EricksonLopez.Processes.Testing` | Decorator wrapper around any `IProcessStore<TState>` that injects configurable faults (`ConcurrencyConflictsToSimulate`, `ExceptionToThrowOnSave`, `ForcedSaveResult`). | `IProcessStore<TState>` | Resilience testing, OCC retry loop verification, and fault-tolerance checks. | Intermediate | Yes (Level 05, 06) |
| Test Doubles (`TestCounterState`, `TestOrderState`, etc.) | `EricksonLopez.Processes.Testing.Doubles` | Ready-to-use test states and events. | `IProcessState` | Rapid construction of test fixtures and harness setups. | Basic | Yes (Level 05, Tests) |
