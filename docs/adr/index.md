# Architecture Decision Records — Index

This index documents all Architecture Decision Records (ADRs) for the `EricksonLopez.Processes` ecosystem. Each record captures a significant design decision, its context, options considered, and the rationale for the chosen approach.

> **Format**: `ADR-NNN` = Accepted decision. `REJECT-NNN` = Evaluated and rejected alternative.

---

## Core Architecture

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-001](adr-001-core-responsibility.md) | Core Responsibility Boundary | Accepted |
| [ADR-002](adr-002-process-manager-vs-saga.md) | Process Manager vs. Saga Differentiation | Accepted |
| [ADR-003](adr-003-process-identity.md) | Process Identity (`ProcessId`) | Accepted |
| [ADR-004](adr-004-process-type-identity.md) | Process Type Identity (`ProcessType`) | Accepted |
| [ADR-005](adr-005-process-versioning.md) | Process Versioning (`ProcessVersion`) | Accepted |
| [ADR-006](adr-006-process-state.md) | Process State Design (`IProcessState`) | Accepted |
| [ADR-007](adr-007-process-instance.md) | Process Instance Record (`ProcessInstance`) | Accepted |
| [ADR-008](adr-008-correlation-model.md) | Correlation Model (`CorrelationId`, `CausationId`) | Accepted |

---

## Concurrency & Persistence

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-009](adr-009-persistence-boundary.md) | Persistence Boundary (`IProcessStore`) | Accepted |
| [ADR-010](adr-010-optimistic-concurrency.md) | Optimistic Concurrency Control (OCC/CAS via `Revision`) | Accepted |
| [ADR-011](adr-011-idempotency-boundary.md) | Idempotency Boundary | Accepted |
| [ADR-040](adr-040-multi-database-storage-dialects.md) | Multi-Database Storage Dialect Architecture | Accepted |

---

## Saga & Compensation

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-012](adr-012-compensation-model.md) | Compensation Model (LIFO Reverse-Order) | Accepted |
| [ADR-013](adr-013-retry-boundary.md) | Retry Boundary | Accepted |
| [ADR-014](adr-014-timeout-boundary.md) | Timeout Boundary | Accepted |
| [ADR-015](adr-015-scheduling-boundary.md) | Scheduling Boundary | Accepted |
| [ADR-035](adr-035-saga-compensation-engine-visibility.md) | Saga Compensation Engine Visibility | Accepted |
| [ADR-041](adr-041-exception-taxonomy-vs-result-model.md) | Exception Taxonomy vs. Result Model Boundary | Accepted |
| [REJECT-010](reject-010-saga-orchestration-in-mediator-pipelines.md) | Saga Orchestration in Mediator Pipelines | Rejected |

---

## Native AOT, Trimming & Performance

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-016](adr-016-aot-strategy.md) | Native AOT Strategy | Accepted |
| [ADR-017](adr-017-trimming-strategy.md) | Trimming Strategy | Accepted |
| [ADR-029](adr-029-performance.md) | Performance Design | Accepted |
| [ADR-032](adr-032-span-parsable-identifiers.md) | Span-Parsable Identifiers | Accepted |

---

## Serialization

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-019](adr-019-serialization-boundary.md) | Serialization Boundary | Accepted |

---

## Source Generator & Analyzer

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-018](adr-018-source-generator.md) | Roslyn Source Generator | Accepted |
| [ADR-038](adr-038-source-generator-di-extension.md) | Source Generator DI Extension (`AddGeneratedProcesses`) | Accepted |

---

## Integrations

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-020](adr-020-outbox-integration.md) | Outbox Integration | Accepted |
| [ADR-021](adr-021-events-integration.md) | Events Integration | Accepted |
| [ADR-022](adr-022-mediator-integration.md) | Mediator Integration | Accepted |
| [ADR-039](adr-039-mediator-dispatcher-payload-contract.md) | Mediator Dispatcher Payload Contract | Accepted |

---

## Package Structure & Ecosystem

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-023](adr-023-sharedkernel-boundary.md) | Shared Kernel Boundary | Accepted |
| [ADR-024](adr-024-package-structure.md) | Package Structure | Accepted |
| [ADR-025](adr-025-target-frameworks.md) | Target Frameworks | Accepted |

---

## State Migration

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-026](adr-026-state-migration.md) | State Migration | Accepted |
| [ADR-027](adr-027-version-coexistence.md) | Version Coexistence | Accepted |
| [ADR-033](adr-033-state-migration-pipeline.md) | State Migration Pipeline | Accepted |
| [ADR-037](adr-037-typed-payloads-roadmap-v3.md) | Typed Payloads Roadmap (v3) | Accepted |

---

## Observability

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-028](adr-028-observability.md) | Observability (OpenTelemetry) | Accepted |

---

## Coordinator

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-030](adr-030-coordinator-options.md) | Coordinator Options | Accepted |

---

## Testing

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-031](adr-031-testing-package.md) | Testing Package (`InMemoryProcessStore`) | Accepted |
| [ADR-034](adr-034-test-naming-osherove-ide1006.md) | Test Naming Convention (Osherove + IDE1006) | Accepted |

---

## Query Optimization

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-036](adr-036-get-by-correlation-id-default-interface-method.md) | GetByCorrelationId Default Interface Method | Accepted |
