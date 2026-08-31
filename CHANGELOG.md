# CHANGELOG

All notable changes to the `EricksonLopez.Processes` project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Documentation
- Updated `docs/guides/api-reference.md`, `docs/guides/cookbook.md`, and `docs/guides/performance-guide.md` with accurate coordinator defaults, backoff formula, and serializer generic type parameters.

### Build & Infrastructure
- Added strong-naming key configuration (`SignAssembly`) and public key infrastructure.
- Enabled central NuGet audit enforcement (`NuGetAudit`, `NuGetAuditMode`, `NuGetAuditLevel`).
- Bumped `Microsoft.Extensions.*` central package dependencies from `10.0.10` to `10.0.11`.
- Renamed internal test project `EricksonLopez.Processes.AotTests` to `EricksonLopez.Processes.AotSmokeTest`.

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
