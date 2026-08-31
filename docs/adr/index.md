# Architecture Decision Records — Index

This index documents all Architecture Decision Records (ADRs) for the `EricksonLopez.Processes` ecosystem. Each record captures a significant design decision, its context, options considered, and the rationale for the chosen approach.

> **Format**: `ADR-NNN` = Accepted decision. `REJECT-NNN` = Evaluated and rejected alternative.

---

## Core Architecture

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-001](ADR-001-core-responsibility.md) | Core Responsibility Boundary | Accepted |
| [ADR-002](ADR-002-process-manager-vs-saga.md) | Process Manager vs. Saga Differentiation | Accepted |
| [ADR-003](ADR-003-process-identity.md) | Process Identity (`ProcessId`) | Accepted |
| [ADR-004](ADR-004-process-type-identity.md) | Process Type Identity (`ProcessType`) | Accepted |
| [ADR-005](ADR-005-process-versioning.md) | Process Versioning (`ProcessVersion`) | Accepted |
| [ADR-006](ADR-006-process-state.md) | Process State Design (`IProcessState`) | Accepted |
| [ADR-007](ADR-007-process-instance.md) | Process Instance Record (`ProcessInstance`) | Accepted |
| [ADR-008](ADR-008-correlation-model.md) | Correlation Model (`CorrelationId`, `CausationId`) | Accepted |

---

## Concurrency & Persistence

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-009](ADR-009-persistence-boundary.md) | Persistence Boundary (`IProcessStore`) | Accepted |
| [ADR-010](ADR-010-optimistic-concurrency.md) | Optimistic Concurrency Control (OCC/CAS via `Revision`) | Accepted |
| [ADR-011](ADR-011-idempotency-boundary.md) | Idempotency Boundary | Accepted |
| [ADR-040](ADR-040-multi-database-storage-dialects.md) | Multi-Database Storage Dialect Architecture | Accepted |

---

## Saga & Compensation

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-012](ADR-012-compensation-model.md) | Compensation Model (LIFO Reverse-Order) | Accepted |
| [ADR-013](ADR-013-retry-boundary.md) | Retry Boundary | Accepted |
| [ADR-014](ADR-014-timeout-boundary.md) | Timeout Boundary | Accepted |
| [ADR-015](ADR-015-scheduling-boundary.md) | Scheduling Boundary | Accepted |
| [ADR-035](ADR-035-saga-compensation-engine-visibility.md) | Saga Compensation Engine Visibility | Accepted |
| [REJECT-010](REJECT-010-saga-orchestration-in-mediator-pipelines.md) | Saga Orchestration in Mediator Pipelines | Rejected |

---

## Native AOT, Trimming & Performance

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-016](ADR-016-aot-strategy.md) | Native AOT Strategy | Accepted |
| [ADR-017](ADR-017-trimming-strategy.md) | Trimming Strategy | Accepted |
| [ADR-029](ADR-029-performance.md) | Performance Design | Accepted |
| [ADR-032](ADR-032-span-parsable-identifiers.md) | Span-Parsable Identifiers | Accepted |

---

## Serialization

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-019](ADR-019-serialization-boundary.md) | Serialization Boundary | Accepted |

---

## Source Generator & Analyzer

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-018](ADR-018-source-generator.md) | Roslyn Source Generator | Accepted |
| [ADR-038](ADR-038-source-generator-di-extension.md) | Source Generator DI Extension (`AddGeneratedProcesses`) | Accepted |

---

## Integrations

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-020](ADR-020-outbox-integration.md) | Outbox Integration | Accepted |
| [ADR-021](ADR-021-events-integration.md) | Events Integration | Accepted |
| [ADR-022](ADR-022-mediator-integration.md) | Mediator Integration | Accepted |
| [ADR-039](ADR-039-mediator-dispatcher-payload-contract.md) | Mediator Dispatcher Payload Contract | Accepted |

---

## Package Structure & Ecosystem

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-023](ADR-023-sharedkernel-boundary.md) | Shared Kernel Boundary | Accepted |
| [ADR-024](ADR-024-package-structure.md) | Package Structure | Accepted |
| [ADR-025](ADR-025-target-frameworks.md) | Target Frameworks | Accepted |

---

## State Migration

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-026](ADR-026-state-migration.md) | State Migration | Accepted |
| [ADR-027](ADR-027-version-coexistence.md) | Version Coexistence | Accepted |
| [ADR-033](ADR-033-state-migration-pipeline.md) | State Migration Pipeline | Accepted |
| [ADR-037](ADR-037-typed-payloads-roadmap-v3.md) | Typed Payloads Roadmap (v3) | Accepted |

---

## Observability

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-028](ADR-028-observability.md) | Observability (OpenTelemetry) | Accepted |

---

## Coordinator

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-030](ADR-030-coordinator-options.md) | Coordinator Options | Accepted |

---

## Testing

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-031](ADR-031-testing-package.md) | Testing Package (`InMemoryProcessStore`) | Accepted |
| [ADR-034](ADR-034-test-naming-osherove-ide1006.md) | Test Naming Convention (Osherove + IDE1006) | Accepted |

---

## Query Optimization

| ADR | Title | Status |
| :--- | :--- | :--- |
| [ADR-036](ADR-036-get-by-correlation-id-default-interface-method.md) | GetByCorrelationId Default Interface Method | Accepted |
