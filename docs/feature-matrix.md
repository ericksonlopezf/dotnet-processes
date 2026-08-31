# Consolidated Feature Matrix: EricksonLopez.Processes

Definitive categorization, architectural evaluation, and design decision matrix for the `EricksonLopez.Processes` ecosystem.

---

## 1. Decision & Status Legend

### Implementation Statuses
- **Implemented**: Capability fully implemented, tested, and verified in the repository.
- **Partial**: Implementation requiring refinement or responsibility segregation.
- **Missing**: Capability not present in current codebase.
- **Broken**: Defective implementation or architectural violation.
- **Duplicate**: Redundant capability within the repository or ecosystem.
- **Deprecated**: Obsolete capability marked for removal.
- **Out of Scope**: Capability outside the library's domain boundaries.

### Architectural Decisions
- **KEEP**: Preserve functionality without conceptual changes.
- **IMPLEMENT**: Develop capability as a roadmap priority.
- **REDESIGN**: Substantially restructure contract or behavior.
- **REFACTOR**: Improve internal implementation while maintaining public API stability.
- **MERGE**: Consolidate multiple types into a single abstraction.
- **MOVE**: Move responsibility to another ecosystem library (`Outbox`, `EventBus`, `Mediator`).
- **REMOVE**: Delete existing code due to scope creep or anti-pattern.
- **DEFER**: Postpone to future milestone without affecting current stability.
- **REJECT**: Explicitly rejected non-goal documented via Architecture Decision Record (ADR).

---

## 2. Exhaustive Capabilities Matrix (55 Evaluated Features)

| ID | Category | Feature / Capability | Status | Competitor Support | Arch Value | AOT Rating | Performance Impact | Complexity | Decision | Linked ADR |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **MOD-01** | Model & Identity | `ProcessId` UUIDv7 sequential (`readonly record struct`) | Implemented | MassTransit / NServiceBus | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-003 |
| **MOD-02** | Model & Identity | `ProcessType` strongly typed and immutable | Implemented | MassTransit / Wolverine | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-004 |
| **MOD-03** | Model & Identity | `ProcessVersion` semantic versioning (int >= 1) | Implemented | NServiceBus | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-005 |
| **MOD-04** | Model & Identity | `Revision` monotonic token for OCC CAS | Implemented | MassTransit / Marten | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-010 |
| **MOD-05** | Model & Identity | Message identifiers (`CorrelationId`, `CausationId`, `MessageId`) | Implemented | Wolverine / MassTransit | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-008 |
| **MOD-06** | Model & Identity | `ISpanParsable<T>` and `ISpanFormattable` implementations | Implemented | Modern .NET BCL | High | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-032 |
| **MOD-07** | Model & Identity | `ProcessInstance<TState>` immutable aggregate root | Implemented | NServiceBus / MassTransit | Critical | AOT Safe | 1 alloc (class) | Medium | **KEEP** | ADR-006, ADR-007 |
| **MOD-08** | Model & Identity | Parent/Child process hierarchy | Out of Scope | Temporal / NServiceBus | Low / Harmful | AOT Risk | High overhead | High | **REJECT** | ADR-030 (Reject) |
| **MOD-09** | Model & Identity | Arbitrary untyped metadata dictionary (`Dictionary<string, object>`) | Deprecated | MassTransit | Low | AOT Incompatible | Boxing / Alloc | Low | **REMOVE** | ADR-006 |
| **LIF-01** | Lifecycle | Formal `ProcessStatus` lifecycle (6 states) | Implemented | Automatonymous / Stateless | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-007 |
| **LIF-02** | Lifecycle | Deterministic pure transition `(TState, TEvent) -> TransitionResult` | Implemented | Pure Functional / Elm | Critical | AOT Safe | 1 alloc (result) | Low | **KEEP** | ADR-001 |
| **LIF-03** | Lifecycle | Idempotency on terminal states (`Completed`, `Compensated`, `Failed`) | Implemented | MassTransit / Wolverine | Critical | AOT Safe | Zero-Alloc | Low | **KEEP** | ADR-011 |
| **LIF-04** | Lifecycle | `DeadLettered` state at process level | Out of Scope | Transport / Broker | Zero | N/A | N/A | Low | **REJECT** | ADR-011 |
| **LIF-05** | Lifecycle | `Suspended` state for timeout/callback awaiting | Implemented | Temporal / Workflow | High | AOT Safe | Zero-Alloc | Low | **KEEP** | ADR-014 |
| **EXE-01** | Execution | `ProcessCoordinator<TState>` runtime coordination engine | Implemented | Core Coordinator | Critical | AOT Safe | Low-Alloc (<128 B) | Medium | **KEEP** | ADR-001, ADR-010 |
| **EXE-02** | Execution | OCC CAS loop with exponential backoff retries | Implemented | Custom / Polly | Critical | AOT Safe | Zero if no conflict | Medium | **KEEP** | ADR-010, ADR-013 |
| **EXE-03** | Execution | Strongly typed `ProcessCoordinatorOptions` | Implemented | Microsoft Extensions | High | AOT Safe | 1 config alloc | Low | **KEEP** | ADR-030 |
| **EXE-04** | Execution | Deterministic `ProcessContext` with injectable `TimeProvider` | Implemented | .NET 8+ BCL | Critical | AOT Safe | 1 alloc | Low | **KEEP** | ADR-008, ADR-028 |
| **EXE-05** | Execution | Blocking synchronous execution API (`Execute(...)`) | Out of Scope | Legacy libs | Negative | N/A | Thread blocking | Low | **REJECT** | ADR-001 |
| **EXE-06** | Execution | Coordinator internal fan-out / fan-in parallelism | Out of Scope | Temporal / TPL Dataflow | Negative | AOT Risk | Locking / Races | High | **REJECT** | ADR-001 |
| **EXE-07** | Execution | Conditional rule branching / workflow DSL engine | Out of Scope | Elsa / Camunda | Negative | AOT Risk | Dynamic IL | High | **REJECT** | ADR-001 |
| **EXE-08** | Execution | Middleware / Behavior pipeline per transition execution | Duplicate | MediatR / Wolverine | Negative | AOT Risk | Delegate overhead | Medium | **REJECT** | ADR-022 |
| **EXE-09** | Execution | Durable execution with event-sourced replay | Out of Scope | Temporal / Dapr | Negative | AOT Incompatible | Replay overhead | Extreme | **REJECT** | ADR-009 |
| **EFF-01** | Effects & Intents | `ProcessEffect` as pure immutable data records (Intents) | Implemented | Elmish / Event Store | Critical | AOT Safe | Low-Alloc | Low | **KEEP** | ADR-015, ADR-020 |
| **EFF-02** | Effects & Intents | `ProcessEffect.Command` (outgoing command intent) | Implemented | Wolverine / Outbox | Critical | AOT Safe | Low-Alloc | Low | **KEEP** | ADR-020, ADR-022 |
| **EFF-03** | Effects & Intents | `ProcessEffect.Event` (domain/integration event intent) | Implemented | EventBus / Outbox | Critical | AOT Safe | Low-Alloc | Low | **KEEP** | ADR-020, ADR-021 |
| **EFF-04** | Effects & Intents | `ProcessEffect.ScheduleTimeout` (delayed timer intent) | Implemented | Hangfire / Quartz | High | AOT Safe | Low-Alloc | Low | **KEEP** | ADR-014, ADR-015 |
| **EFF-05** | Effects & Intents | `ProcessEffect.Compensation` (rollback trigger intent) | Implemented | Saga Pattern | Critical | AOT Safe | Low-Alloc | Low | **KEEP** | ADR-012 |
| **EFF-06** | Effects & Intents | Direct transport I/O execution inside coordinator | Broken / Scope | MassTransit Courier | Negative | AOT Risk | Dual-write hazard | High | **REMOVE** | ADR-001 |
| **SAG-01** | Saga & Compensation | `ISaga<TState>` and `ICompensationHandler<TState>` contracts | Implemented | MassTransit / NServiceBus | Critical | AOT Safe | Zero-Alloc | Medium | **KEEP** | ADR-002, ADR-012 |
| **SAG-02** | Saga & Compensation | Deterministic reverse LIFO step compensation | Implemented | Saga Pattern | Critical | AOT Safe | Low-Alloc | Medium | **KEEP** | ADR-012 |
| **SAG-03** | Saga & Compensation | Centralized `coordinator.CompensateAsync<TSaga>` | Implemented | Coordinator Pattern | High | AOT Safe | Low-Alloc | Medium | **KEEP** | ADR-012, ADR-035 |
| **SAG-04** | Saga & Compensation | Public standalone `SagaCompensationEngine` with I/O | Broken | Legacy | Negative | AOT Risk | I/O coupling | Medium | **REMOVE** | ADR-035 |
| **SAG-05** | Saga & Compensation | Automatic network retry loops in compensation steps | Out of Scope | Polly / Host | Negative | N/A | Infinite loop risk | Medium | **REJECT** | ADR-012 |
| **COR-01** | Correlation | `IProcessCorrelation<TEvent>` compile-time mapping | Implemented | MassTransit / NServiceBus | Critical | AOT Safe | Zero-Alloc | Low | **KEEP** | ADR-008 |
| **COR-02** | Correlation | Dynamic property reflection correlation | Out of Scope | MassTransit Expression | Negative | AOT Incompatible | Slow reflection | Medium | **REJECT** | ADR-008 |
| **STO-01** | Persistence | Minimal `IProcessStore<TState>` SPI (4 methods) | Implemented | Store Port | Critical | AOT Safe | Zero-Alloc | Low | **KEEP** | ADR-009, ADR-010 |
| **STO-02** | Persistence | Atomic `ProcessSaveResult` (OCC ConcurrencyConflict) | Implemented | CAS Protocol | Critical | AOT Safe | Zero-Alloc (0 B) | Low | **KEEP** | ADR-009, ADR-010 |
| **STO-03** | Persistence | Thread-safe `InMemoryProcessStore<TState>` with real CAS | Implemented | Testing Harness | High | AOT Safe | Low-Alloc | Medium | **KEEP** | ADR-031 |
| **STO-04** | Persistence | Direct EF Core / Dapper coupling in Core package | Out of Scope | MassTransit Storage | Negative | AOT Risk | Dependency bloat | High | **REJECT** | ADR-009 |
| **STO-05** | Persistence | Direct Redis / NoSQL driver in Core package | Out of Scope | Redis Providers | Negative | AOT Risk | Dependency bloat | High | **REJECT** | ADR-009 |
| **STO-06** | Persistence | Distributed Locking (Redlock / ZooKeeper) | Out of Scope | Distributed Locking | Negative | N/A | Deadlock risk | High | **REJECT** | ADR-010 |
| **VER-01** | Versioning & Evolution | `IProcessStateMigrator<TFrom, TTo>` stepwise contracts | Implemented | Schema Migration | High | AOT Safe | 1 alloc | Medium | **KEEP** | ADR-026 |
| **VER-02** | Versioning & Evolution | `ProcessStateMigrationPipeline` sequential chain (V1->V2->V3) | Implemented | Pipeline Pattern | High | AOT Safe | Low-Alloc | Medium | **KEEP** | ADR-033 |
| **VER-03** | Versioning & Evolution | Parallel multi-version coexistence (`ProcessVersion`) | Implemented | Blue-Green / Zero-Downtime | High | AOT Safe | Zero-Alloc | Medium | **KEEP** | ADR-027 |
| **AOT-01** | Native AOT & Tooling | `ProcessSourceGenerator` Roslyn Incremental Generator | Implemented | Modern Roslyn | Critical | AOT Safe | 0 ms runtime | Medium | **KEEP** | ADR-016, ADR-018 |
| **AOT-02** | Native AOT & Tooling | Runtime assembly scanning (`Assembly.GetTypes`) | Out of Scope | MediatR / AutoMapper | Negative | AOT Incompatible | Trimming break | Low | **REJECT** | ADR-016 |
| **AOT-03** | Native AOT & Tooling | `System.Text.Json` source generation with `JsonTypeInfo<T>` | Implemented | STJ SourceGen | Critical | AOT Safe | Low-Alloc | Medium | **KEEP** | ADR-019 |
| **OBS-01** | Observability | OpenTelemetry Tracing via native `ActivitySource` | Implemented | OTel Semantic Conv | Critical | AOT Safe | Zero when disabled | Medium | **KEEP** | ADR-028, ADR-029 |
| **OBS-02** | Observability | OpenTelemetry Metrics via native `Meter` | Implemented | OTel Semantic Conv | Critical | AOT Safe | Zero when disabled | Medium | **KEEP** | ADR-028, ADR-029 |
| **OBS-03** | Observability | Zero-allocation telemetry with `TagList` & listener checks | Implemented | High-perf .NET | High | AOT Safe | Zero-Alloc | Low | **KEEP** | ADR-029, ADR-032 |
| **OBS-04** | Observability | Mandatory `ILogger` dependency in Core engine | Broken / Scope | Generic Logging | Negative | AOT Safe | Tag allocations | Low | **REMOVE** | ADR-028 |
| **INT-01** | Integrations | `EricksonLopez.Processes.Outbox` transactional dispatcher | Implemented | Outbox Pattern | High | AOT Safe | Low-Alloc | Medium | **KEEP (Pkg)** | ADR-020 |
| **INT-02** | Integrations | `EricksonLopez.Processes.Mediator` in-process dispatcher | Implemented | In-Process CQRS | High | AOT Safe | Low-Alloc | Medium | **KEEP (Pkg)** | ADR-022 |
| **INT-03** | Integrations | BPMN 2.0 Engine / Visual UI Workflow Designer | Out of Scope | Camunda / Elsa | Negative | AOT Incompatible | Massive bloat | Extreme | **REJECT** | ADR-001 |

---

## 3. Quantified Decision Totals

```text
┌────────────────────────────────────────────────────────┐
│               FEATURE MATRIX DECISION TOTALS           │
│                                                        │
│   TOTAL CAPABILITIES EVALUATED:  55                    │
│                                                        │
│   ✅ KEEP / COMPLETED:           33 (60.0%)            │
│   🚀 IMPLEMENT / NEW:            0  (Consolidated)     │
│   🔄 REFACTOR / REDESIGN:        0  (Completed)        │
│   📦 MOVE / INTEGRATION PKG:     2  (3.6%)             │
│   🗑️ REMOVE (Scope Creep):       3  (5.5%)             │
│   ❌ REJECT (Non-Goals with ADR): 17 (30.9%)           │
└────────────────────────────────────────────────────────┘
```
