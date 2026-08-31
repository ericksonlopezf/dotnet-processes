# Technical Roadmap: EricksonLopez.Processes

Consolidated technical roadmap and architectural milestone plan for the `EricksonLopez.Processes` ecosystem.

---

## 1. Vision & Architectural Invariants

`EricksonLopez.Processes` serves as the foundational reference library for Process Managers and Sagas in .NET 10+, delivering:
- **100% Native AOT & Trimming Compliance**: Zero runtime reflection, zero `Activator.CreateInstance`, zero `Assembly.GetTypes()`.
- **Zero / Low Allocation Hotpaths**: Struct value objects, `ISpanParsable<T>`/`ISpanFormattable`, stack-friendly transitions.
- **Decoupled Persistence with Optimistic Concurrency (CAS)**: Atomic updates with monotonic `Revision` tokens across 6 database dialects.
- **Pure Output Intents**: Transitions yield immutable `ProcessEffect` records without performing direct network I/O.

---

## 2. Evolution Milestones

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
- **Focus**: Pure domain abstractions, Roslyn Source Generator, OCC CAS retry loop, 100% test coverage, and benchmark baselines.
- **Deliverables**:
  - `EricksonLopez.Processes.Abstractions` and `EricksonLopez.Processes` runtime.
  - Zero compilation warnings (`TreatWarningsAsErrors=true`).
  - Compile-time Roslyn Incremental Generator (`ProcessSourceGenerator`).
  - Native OpenTelemetry telemetry instrumentation with `ActivitySource` and `Meter`.

### Phase 1 — Package & API Polish (v1.0.0-rc1)
- **Status**: `COMPLETED`
- **Focus**: Public API stability, testing doubles, and explicit intent hierarchy.
- **Deliverables**:
  - `ProcessCoordinatorOptions` for decoupled retry and backoff configuration (ADR-030).
  - Dedicated `EricksonLopez.Processes.Testing` package with thread-safe `InMemoryProcessStore<TState>` (ADR-031).
  - Purified `ProcessEffect` hierarchy (Command, Event, ScheduleTimeout, Compensation).
  - Centralized `ProcessCoordinator.CompensateAsync<TSaga>` for orchestrated saga rollback (ADR-035).

### Phase 2 — Performance & Allocation Hardening (v1.0.0-rc2)
- **Status**: `COMPLETED`
- **Focus**: Micro-allocation elimination in hotpaths and span formatting.
- **Deliverables**:
  - Zero-allocation telemetry using `System.Diagnostics.TagList` and `ActivitySource.HasListeners()` checks.
  - Implemented `ISpanParsable<TSelf>` and `ISpanFormattable` across all identifier structs (ADR-032).
  - BenchmarkDotNet baseline report published in `docs/benchmarks/results.md`.

### Phase 3 — Ecosystem Integration & Reference Samples (v1.0.0-final)
- **Status**: `COMPLETED`
- **Focus**: Integration bridges and comprehensive developer documentation.
- **Deliverables**:
  - PostgreSQL + Dapper technical persistence guide (`docs/guides/postgresql-dapper-store.md`).
  - Outbox and EventBus integration guide (`docs/guides/outbox-eventbus-integration.md`).
  - Runnable reference samples in `samples/`.

### Phase 4 — Schema Evolution & Versioning Extensions (v1.1.0)
- **Status**: `COMPLETED`
- **Focus**: Sequential state migration during instance hydration.
- **Deliverables**:
  - `ProcessStateMigrationPipeline` with fluent pipeline builder for V1 -> V2 -> V3 transformations (ADR-033).
  - Stepwise migration test coverage.

### Phase 5 — Competitive Parity & Storage Dialects Expansion
- **Status**: `COMPLETED`
- **Focus**: Expanding persistence options, compile-time DI extensions, and resolving audit gaps.
- **Deliverables**:
  - Extended `ProcessSourceGenerator` emitting `GeneratedProcessRegistryExtensions.g.cs` with `AddGeneratedProcesses(IServiceCollection)` (ADR-038).
  - Production-ready ADO.NET storage providers for SQLite, MySQL, MariaDB, and Oracle with atomic CAS semantics (ADR-040).
  - Enhanced `MediatorProcessDispatcher` with unrecognized payload callbacks and type-safe helpers (ADR-039).

### Phase 6 — AOT Hardening & Type Safety (v2.0.0 Target)
- **Status**: `PLANNED`
- **SemVer**: Major Breaking Release
- **Focus**: Resolving AOT safety and type erasure in effect and compensation payloads.
- **Planned Changes**:
  - Replace `object` payload in `CompensationStep` with structured `System.Text.Json.JsonElement` or generic payload contracts.
  - Introduce typed effect variants `ProcessEffect.TypedCommand<T>` and `ProcessEffect.TypedEvent<T>`.
  - Promote `IProcessStore<TState>.GetByCorrelationIdAsync` from a default interface method to a required abstract interface method.
