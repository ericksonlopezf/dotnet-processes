# CHANGELOG

All notable changes to the `EricksonLopez.Processes` project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [2.0.0] - 2026-09-24

### Breaking Changes
- **Core / ABI (`ProcessInstance<TState>.Advance`):** The 3-parameter overload `Advance(newState, newStatus, now)` has been updated to include optional parameters `newCompensations` and `maxCompensations`. In compiled .NET metadata, the 3-parameter method symbol was removed. Precompiled assemblies calling the 3-parameter method must be recompiled to avoid runtime `System.MissingMethodException`.
- **Core / Lifecycle Invariant (`ProcessInstance<TState>.Advance`):** Invoking `Advance` on an instance that has reached a terminal status (`Completed`, `Compensated`, or `Failed`) now throws `InvalidProcessTransitionException`. Terminal states are immutable and irreversible. Consumers must not advance terminal instances; initiate a brand new process instance with a new `ProcessId` if a new workflow cycle is required.
- **Core / Safety Guardrail (`ProcessInstance<TState>.Advance`):** `Advance` now enforces a safety threshold of maximum allowed compensation steps (default `1000`) to prevent memory leaks in infinite processes, throwing `InvalidOperationException` when exceeded. Consumers requiring larger compensation stacks must supply an explicit `maxCompensations` parameter or configure `ProcessCoordinatorOptions.MaxCompensations`.
- **Core / ABI (`ProcessInstance<TState>..ctor`):** The public 10-parameter constructor has been replaced with an 11-parameter constructor accepting `IReadOnlyList<CompensationStep>? recordedCompensations = null`. Custom storage implementations, serializers, and external assemblies constructing `ProcessInstance` directly must be recompiled.
- **Core / ABI (`ProcessExecutionResult<TState>..ctor`):** The public 3-parameter constructor has been replaced with a 4-parameter constructor accepting `IReadOnlyList<CompensationStep>? recordedCompensations = null`. Mock implementations, test doubles, and external callers must be recompiled.
- **Engine / Configuration (`ProcessCoordinatorOptions.InitialBackoffDelay`):** Setting `InitialBackoffDelay` to a duration less than or equal to `TimeSpan.Zero` now throws `ArgumentOutOfRangeException`. Unit tests that previously used `TimeSpan.Zero` to eliminate latency must set `TimeSpan.FromMilliseconds(1)` or supply a custom `backoffStrategy` delegate (e.g., `_ => TimeSpan.Zero`) to `ProcessCoordinator`.
- **Storage / Configuration (`IProcessStore` Implementations):** All relational storage providers (`PostgreSql`, `SqlServer`, `Sqlite`, `MySql`, `MariaDb`, `Oracle`) now validate `tableName` against the regex `^[a-zA-Z_][a-zA-Z0-9_]*$` to eliminate SQL injection vectors. Schema-qualified names (e.g., `dbo.process_instances` or `public.processes`), dashes, brackets, and quotes are rejected with `ArgumentException`. Pass bare table names and configure default schemas at the connection string level or database user search path.
- **Storage / Oracle Database (`OracleProcessStore<TState>`):** All SQL queries now enclose table names in double quotes (`"{_tableName}"`), enforcing case-sensitive identifier resolution in Oracle Database. Consumers must ensure that the `tableName` string passed to `OracleProcessStore` matches the exact casing of the physical database table (standard Oracle tables are typically uppercase `PROCESS_INSTANCES`).
- **Engine / Validation (`ProcessCoordinator<TState>.ExecuteAsync`):** Passing `canInitiate: true` alongside `initialStateFactory: null` now immediately throws `ArgumentNullException(nameof(initialStateFactory))` at method entry, regardless of whether the instance already exists in storage. Callers must pass `canInitiate: false` when targeting existing instances without an initial state factory.
- **Engine / Saga Compensation (`ProcessCoordinator<TState>.CompensateAsync`):** The `recordedSteps` parameter passed to `CompensateAsync` is now bypassed; compensation steps are loaded directly from `instance.RecordedCompensations` in the database and executed iteratively step-by-step in reverse order with intermediate `Compensating` status writes. Consumers must migrate to the parameterless overload `CompensateAsync(processId, saga)` and record steps during forward transitions via `ProcessTransitionResult<TState>.Advance(..., recordedCompensations)`.
- **Engine / Persistence Lifecycle (`ProcessCoordinator<TState>.ExecuteAsync`):** Initiating a new process instance now performs an initial database `SaveAsync` of the initial state followed by a second `SaveAsync` of the transitioned state, resulting in newly initiated instances starting at `Revision = 2` rather than `Revision = 1`. Downstream database triggers and test assertions expecting `Revision = 1` must be updated.
- **Integration / Dispatchers (`EventProcessDispatcher` & `MediatorProcessDispatcher`):** Handling `ProcessEffect.ScheduleTimeout` now executes an asynchronous wait (`await Task.Delay(timeout.Delay)`) prior to publishing. For long timeout durations, consumers must not use in-process dispatchers and should instead use transactional outbox persistence (`EricksonLopez.Processes.Outbox`) paired with an external scheduler.
- **Analyzers / Diagnostics (`ProcessTransitionAnalyzer`):** Diagnostic rule `ELPROC002` has been retitled to *"Saga definition missing compensation logic"* and now reports on any class annotated with `[SagaDefinition]` that does not implement `ICompensationHandler<TState>`. In strict build environments (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`), sagas must implement compensation handlers or suppress the rule.
- **Storage / Data Schema (`IProcessStore` Implementations):** Relational storage stores now persist the `Version` / `version` column during `UpdateExistingAsync`. Previously, version increments during schema migration were not persisted to the database. Downstream auditing and change data capture (CDC) pipelines observing version column mutations must account for this behavior.

### Added
- Defense-in-depth payload sizing guard (`MaxPayloadSizeBytes`) in `SystemTextJsonProcessStateSerializer<TState>` to prevent LOH exhaustion and DoS attacks.
- Strong-naming key configuration (`SignAssembly`) with embedded public key infrastructure (`EricksonLopez.snk`).
- Sigstore Provenance Attestations in publishing pipeline (`actions/attest-build-provenance@v2`).

### Changed
- Standardized all repository technical documentation, architectural diagrams, and showcase guides into English.
- Updated `CONTRIBUTING.md` with exact pinned .NET SDK version (`10.0.100`), benchmark gate policy, and active branch strategy (`main`, `develop`).
- Updated `SUPPORT.md` with validated documentation paths, corrected repository URLs, and enterprise support policies.
- Bumped `Microsoft.Extensions.*` central package dependencies from `10.0.10` to `10.0.11` in `Directory.Packages.props`.
- Renamed internal test project `EricksonLopez.Processes.AotTests` to `EricksonLopez.Processes.AotSmokeTest`.

### Fixed
- Fixed non-existent `./test-unit.ps1` script references in `CONTRIBUTING.md` and `.github/PULL_REQUEST_TEMPLATE.md` to standard `dotnet test` commands.
- Fixed broken documentation hyperlinks in `SUPPORT.md` and `README.md`.
- Updated placeholder contact email in `CODE_OF_CONDUCT.md` to maintainer's verified email.

### Security
- Enabled central NuGet vulnerability audit enforcement (`<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditMode>all</NuGetAuditMode>`, `<NuGetAuditLevel>low</NuGetAuditLevel>`).
- Enforced short-lived OIDC trusted publishing for NuGet.org package releases (`NuGet/login@v1`), eliminating static API keys.

---

## [1.0.0] - 2026-08-25

### Added
- **Core Process Engine (`EricksonLopez.Processes.Abstractions` & `EricksonLopez.Processes`)**:
  - Immutable contracts: `IProcess<TState>`, `IProcessHandler<TState, in TEvent>`, `ISaga<TState>`, `ICompensationHandler<TState>`, `IProcessCorrelation<in TEvent>`.
  - Zero-allocation strongly typed value object identifiers: `ProcessId` (RFC 9562 UUIDv7), `CorrelationId`, `CausationId`, `MessageId`, `ProcessType`, `ProcessVersion`, `Revision`, and `CompositeCorrelationKey`.
  - Pure output intents via `ProcessEffect` (`Command`, `Event`, `ScheduleTimeout`, `Compensation`).
  - Runtime execution coordinator (`ProcessCoordinator<TState>`) featuring Optimistic Concurrency Control (OCC CAS) with configurable linear/exponential backoff loops.
  - Native telemetry instrumentation with `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`.
- **Compile-Time Roslyn Generator & Analyzers**:
  - `EricksonLopez.Processes.Generator`: Incremental Source Generator discovering `[ProcessDefinition]` and `[SagaDefinition]` with static `AddGeneratedProcesses()` DI registrations.
  - `EricksonLopez.Processes.Analyzers`: Compile-time diagnostics validating state transitions and compensation completeness.
- **Relational Persistence Providers (`EricksonLopez.Processes.Storage.*`)**:
  - Native ADO.NET storage providers with atomic CAS semantics for PostgreSQL (`Npgsql`), SQL Server (`Microsoft.Data.SqlClient`), SQLite (`Microsoft.Data.Sqlite`), MySQL (`MySqlConnector`), MariaDB (`MySqlConnector`), and Oracle (`Oracle.ManagedDataAccess.Core`).
- **Integration Dispatchers**:
  - `EricksonLopez.Processes.Outbox`: Transactional outbox dispatching via `EricksonLopez.Outbox`.
  - `EricksonLopez.Processes.Mediator`: In-process CQRS command/notification dispatching via `EricksonLopez.Mediator`.
  - `EricksonLopez.Processes.Events`: Domain event dispatching via `EricksonLopez.Events.Contracts`.
- **Schema Evolution & Versioning**:
  - `ProcessStateMigrationPipeline` enabling zero-downtime sequential state migrations (V1 -> V2 -> V3).
- **Testing SDK**:
  - `EricksonLopez.Processes.Testing`: Fast, thread-safe `InMemoryProcessStore<TState>` with atomic CAS simulation for unit and property testing.
- **Comprehensive Reference Showcase (`samples/EricksonLopez.Processes.Showcase`)**:
  - 11 progressive levels (Level 00 to Level 10) demonstrating the entire public API surface, Native AOT compilation, and enterprise multi-database patterns.

---

[Unreleased]: https://github.com/ericksonlopezf/dotnet-processes/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ericksonlopezf/dotnet-processes/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-processes/releases/tag/v1.0.0
