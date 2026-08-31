# Architecture Decision Records: Discarded Features (Non-Goals)

**Document**: ADR-100 to ADR-127 | **Version**: 2.0.0 | **Taxonomy**: Rejection ADRs (non-goals and anti-features) are formally indexed in the **ADR-100 to ADR-127** series to maintain clear separation from active positive architectural decisions (ADR-001 through ADR-040 in `docs/adr/`).

---

## ADR-100: No Parent/Child Process Hierarchy

- **Status**: Accepted
- **Context**: Systems like Temporal, NServiceBus, and Windows Workflow Foundation permit parent/child workflow hierarchies with cascading cancellations.
- **Decision**: `EricksonLopez.Processes` **does not** implement parent/child tree hierarchies within the core engine.
- **Rationale**: Inter-process coordination is cleanly achieved via message choreography and event correlation. Hierarchical trees introduce foreign key complexity and distributed lock contention in storage adapters.
- **Consequences**: `ProcessInstance<TState>` remains flat and isolated. Processes correlate via shared `CorrelationId`.

---

## ADR-101: No Synchronous Execution API

- **Status**: Accepted
- **Context**: Legacy systems sometimes request synchronous blocking methods (`coordinator.Execute(...)`).
- **Decision**: `EricksonLopez.Processes` provides exclusively asynchronous, `ValueTask`-based execution APIs (`ExecuteAsync`, `CompensateAsync`).
- **Rationale**: State persistence and effect dispatching involve asynchronous I/O; synchronous wrappers cause thread-pool starvation under high concurrency.

---

## ADR-102: No Coordinator Internal Concurrency / Parallelism

- **Status**: Accepted
- **Context**: Requests for parallel branch execution (fan-out / fan-in) inside a single coordinator call.
- **Decision**: Single process instance transitions are strictly sequential. Parallelism is achieved across independent process instances or through external workers.
- **Rationale**: Preserves deterministic state mutation and prevents in-memory race conditions against the optimistic concurrency `Revision` token.

---

## ADR-103: No Dynamic Rule Engine or Visual DSL

- **Status**: Accepted
- **Context**: Visual flow designers and dynamic rule evaluation (e.g., Camunda, Elsa).
- **Decision**: Workflows are modeled purely as strongly typed C# code compiled at build time.
- **Rationale**: Dynamic expression interpreters require runtime reflection, violating 100% Native AOT and IL trimming constraints.

---

## ADR-104: No Middleware Pipeline in Coordinator

- **Status**: Accepted
- **Context**: Interceptor pipelines similar to ASP.NET Core or MediatR pipeline behaviors.
- **Decision**: Coordinator execution is direct. Cross-cutting concerns are handled via composition, decorators, or `ProcessContext`.
- **Rationale**: Eliminates delegate allocations and keeps the state transition hotpath under 120 ns.

---

## ADR-105: No Standalone Public SagaCompensationEngine with I/O

- **Status**: Accepted (see ADR-035)
- **Decision**: `SagaCompensationEngine` is internal; consumers trigger rollback via `ProcessCoordinator.CompensateAsync`.

---

## ADR-106: No Automatic Compensation Network Retries

- **Status**: Accepted
- **Decision**: Compensating network calls are not looped infinitely in the core. Sagas transition to `Failed` if a compensating action encounters an unrecoverable failure.

---

## ADR-107: No Process-Level DeadLettered State

- **Status**: Accepted
- **Decision**: Poison message dead-lettering is the responsibility of the transport/message broker, not the process aggregate lifecycle.

---

## ADR-108: No Built-in Cron / Quartz Background Worker

- **Status**: Accepted (see ADR-015)
- **Decision**: The coordinator emits `ProcessEffect.ScheduleTimeout`; the host application wires delays to its scheduler of choice.

---

## ADR-109: No Mandatory ILogger Dependency in Core Engine

- **Status**: Accepted (see ADR-028)
- **Decision**: Telemetry in core uses `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`. Generic logging dependencies are excluded from core.

---

## ADR-110: No BPMN 2.0 Engine / XML Schema Parsers

- **Status**: Accepted
- **Decision**: BPMN 2.0 XML parsing is out of scope. C# 13 code represents the workflow definition.

---

## ADR-111: No Event-Sourced Replay Architecture

- **Status**: Accepted (see ADR-009)
- **Decision**: State is stored as current snapshots with optimistic concurrency tokens, not event-sourced streams.

---

## ADR-113: No Dynamic Property Reflection for Event Correlation

- **Status**: Accepted (see ADR-008)
- **Decision**: Correlation mapping is explicitly defined via compile-time `IProcessCorrelation<TEvent>` implementations.

---

## ADR-114: No Direct EF Core Dependency in Core Abstractions

- **Status**: Accepted (see ADR-009)
- **Decision**: Core packages depend on zero ORMs; persistence is decoupled via `IProcessStore<TState>`.

---

## ADR-115: No Distributed Lock Manager (Redlock)

- **Status**: Accepted (see ADR-010)
- **Decision**: Concurrency safety is achieved via database Compare-And-Swap (CAS) on `Revision`, avoiding distributed lock deadlocks.
