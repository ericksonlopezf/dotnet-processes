# Architectural Boundary Specification: EricksonLopez.Processes.Abstractions

## 1. Purpose

`EricksonLopez.Processes.Abstractions` defines the zero-dependency pure abstractions, state machine contracts, strongly typed value object identifiers, effect intent models, and storage SPI interfaces for long-running business process managers and sagas in .NET.

---

## 2. Package Ownership

- **State & Lifecycle**: `IProcessState`, `IProcessStateMigrator`, `ProcessInstance<TState>`, `ProcessStatus`.
- **Side-Effect Intent Models**: `ProcessEffect` abstract records (`Command`, `Event`, `ScheduleTimeout`, `Compensation`).
- **Storage Port (SPI)**: `IProcessStore<TState>`, `IProcessStateSerializer<TState>`, `ProcessSaveResult`.
- **Strongly Typed Value Object Identifiers**: `ProcessId`, `CorrelationId`, `CausationId`, `MessageId`, `ProcessType`, `ProcessVersion`, `Revision`, `CompositeCorrelationKey`.
- **Compensation Primitives**: `CompensationAction`, `CompensationStep`.
- **Handler & Correlation Interfaces**: `IProcess<TState>`, `IProcessHandler<TState, in TEvent>`, `ISaga<TState>`, `ICompensationHandler<TState>`, `IProcessCorrelation<in TEvent>`.

---

## 3. Explicitly Excluded Responsibilities

- Process execution coordinator or state transition engine (`EricksonLopez.Processes`).
- Event publisher integration (`EricksonLopez.Processes.Events`).
- Mediator dispatch integration (`EricksonLopez.Processes.Mediator`).
- Outbox transactional dispatch (`EricksonLopez.Processes.Outbox`).
- Relational database persistence providers (`EricksonLopez.Processes.Storage.*`).
- Roslyn compile-time source generation (`EricksonLopez.Processes.Generator`).

---

## 4. Allowed Dependencies

- **.NET Base Class Library (BCL) only**.
- **Zero** external or third-party dependencies (see ADR-001, ADR-009).

---

## 5. Forbidden Dependencies

- `EricksonLopez.Events.Contracts` (decoupled per ADR-009).
- Database drivers (`Npgsql`, `Microsoft.Data.SqlClient`, `Microsoft.Data.Sqlite`, `MySqlConnector`, `Oracle.ManagedDataAccess.Core`).
- `Microsoft.Extensions.DependencyInjection` (confined strictly to DI package).
- `System.Text.Json` (pure abstraction boundary).

---

## 6. Consumers (Who Can Depend On It)

- `EricksonLopez.Processes` (core engine).
- `EricksonLopez.Processes.Events` (event bridge).
- `EricksonLopez.Processes.Mediator` (mediator bridge).
- `EricksonLopez.Processes.Outbox` (outbox bridge).
- `EricksonLopez.Processes.Storage.*` (storage provider adapters).
- `EricksonLopez.Processes.Testing` (in-memory testing doubles).

---

## 7. Public API Constraints

- All state representations must be immutable records.
- Identifier structs must implement `ISpanParsable<TSelf>` and `ISpanFormattable` (ADR-032).
- Pure effect intents must be decoupled from transport execution.

---

## 8. Native AOT & Trimming Expectations

- `IsAotCompatible=true`
- `IsTrimmable=true`
- 100% reflection-free architecture.
