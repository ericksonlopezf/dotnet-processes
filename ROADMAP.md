# Technical Roadmap

This roadmap documents the architectural evolution, completed milestone deliveries, and planned future enhancements for the **`EricksonLopez.Processes`** ecosystem.

---

## 1. Vision & Core Principles

`EricksonLopez.Processes` is the premier, zero-reflection Process Manager and Saga library for modern .NET 10+, built around four non-negotiable principles:

1. **100% Native AOT & Trimming-First**: Zero dynamic code generation (`IL Emit`), zero unannotated reflection, and zero runtime assembly scanning.
2. **Zero/Low Allocation Hotpath**: Value object identifiers (`readonly record struct`), `ISpanParsable<T>`/`ISpanFormattable` implementations, and allocation-free telemetry guards.
3. **Decoupled Persistence with Optimistic Concurrency Control (CAS)**: Atomic Compare-And-Swap updates using monotonic `Revision` tokens across 6 relational storage engines.
4. **Pure Output Intents**: Transitions yield pure immutable `ProcessEffect` records without performing external network I/O during state mutation.

---

## 2. Milestone Overview

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                       CONSOLIDATED TECHNICAL ROADMAP                        │
│                                                                             │
│  Phase 0: Architectural Alignment & Baseline [COMPLETED]                    │
│    └─ Pure Contracts, Source Gen, OCC Loop, 100% Tests, Benchmarks Baseline │
│                                                                             │
│  Phase 1: Package & API Polish (v1.0.0-rc1) [COMPLETED]                     │
│    └─ ProcessCoordinatorOptions, Testing Package, Intent Clarification     │
│                                                                             │
│  Phase 2: Performance & Allocation Hardening (v1.0.0-rc2) [COMPLETED]       │
│    └─ BenchmarkDotNet Baselines, Zero-alloc Hotpath, ISpanParsable IDs      │
│                                                                             │
│  Phase 3: Ecosystem Samples & Integration Guides (v1.0.0-final) [COMPLETED] │
│    └─ Dapper/PostgreSQL Store, Outbox Integration, Clean Documentation      │
│                                                                             │
│  Phase 4: Schema Evolution & Versioning Extensions (v1.1.0) [COMPLETED]     │
│    └─ Automated Migrator Pipelines, Multi-version Coexistence Samples       │
│                                                                             │
│  Phase 5: Competitive Parity & Storage Dialects Expansion [COMPLETED]       │
│    └─ Source Generator DI Extension, SQLite/MySQL/MariaDB/Oracle Adapters   │
│                                                                             │
│  Phase 6: AOT Hardening & Type Safety (v2.0.0) [PLANNED]                   │
│    └─ Typed ProcessEffect payloads, CompensationStep<TPayload>, SemVer Major│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Detailed Milestone Progress

### Phase 0 — Architectural Baseline & Invariants
- **Status**: `COMPLETED`
- **Deliverables**:
  - Zero-dependency abstractions package (`EricksonLopez.Processes.Abstractions`).
  - Core coordinator runtime with optimistic concurrency control (`ProcessCoordinator<TState>`).
  - Roslyn Incremental Source Generator (`ProcessSourceGenerator`) for zero-reflection discovery.
  - Native telemetry instrumentation with `ActivitySource` and `Meter`.
  - 100% unit, architecture, and trim test suite pass rate.

### Phase 1 — Package Structure & Testing SDK
- **Status**: `COMPLETED`
- **Deliverables**:
  - `ProcessCoordinatorOptions` for configurable retry loops and exponential backoff policies (ADR-030).
  - Dedicated testing package (`EricksonLopez.Processes.Testing`) featuring `InMemoryProcessStore<TState>` with atomic CAS semantics (ADR-031).
  - Purified `ProcessEffect` hierarchy as immutable data records (Command, Event, ScheduleTimeout, Compensation).
  - Centralized `ProcessCoordinator.CompensateAsync<TSaga>` for orchestrated rollback (ADR-035).

### Phase 2 — Performance Hardening & Span Parsing
- **Status**: `COMPLETED`
- **Deliverables**:
  - Implemented `ISpanParsable<TSelf>` and `ISpanFormattable` across all identifier value objects (ADR-032).
  - Zero-allocation telemetry via `System.Diagnostics.TagList` and `ActivitySource.HasListeners()` checks.
  - Formal BenchmarkDotNet baseline reports (>800k ops/sec in-memory throughput).

### Phase 3 — Ecosystem Integration & Reference Samples
- **Status**: `COMPLETED`
- **Deliverables**:
  - Integration bridges for `EricksonLopez.Outbox`, `EricksonLopez.Mediator`, and `EricksonLopez.Events.Contracts`.
  - Comprehensive reference showcase (`samples/EricksonLopez.Processes.Showcase`) spanning 11 progressive learning levels.

### Phase 4 — Schema Migration Pipelines
- **Status**: `COMPLETED`
- **Deliverables**:
  - `ProcessStateMigrationPipeline` with fluent pipeline builder for sequential multi-version upgrades (V1 -> V2 -> V3) during instance hydration (ADR-033).
  - Stepwise state migration unit test coverage.

### Phase 5 — Storage Expansion & Source Generator DI
- **Status**: `COMPLETED`
- **Deliverables**:
  - Extended `ProcessSourceGenerator` to generate `AddGeneratedProcesses(IServiceCollection)` DI extensions at compile time (ADR-038).
  - Production-ready ADO.NET storage providers for PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, and Oracle with atomic CAS (ADR-040).
  - Enhanced `MediatorProcessDispatcher` with unrecognized payload callbacks and type-safe helpers (ADR-039).

### Phase 6 — AOT Payload Type-Safety (v2.0.0 Target)
- **Status**: `PLANNED`
- **Target SemVer**: Major Release
- **Planned Enhancements**:
  - Eliminate `object` type erasure in `CompensationStep.Payload` by introducing structured `JsonElement` / typed payload contracts.
  - Provide generic typed variants `ProcessEffect.TypedCommand<T>` and `ProcessEffect.TypedEvent<T>` for compile-time AOT serialization guarantees.
  - Promote `IProcessStore<TState>.GetByCorrelationIdAsync` from a default interface method to a mandatory abstract interface method.
