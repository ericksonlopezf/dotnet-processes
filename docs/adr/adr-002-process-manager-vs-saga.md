# ADR-002: Process Manager vs. Saga Terminology and Model

## Context
In enterprise literature (Enterprise Integration Patterns vs. Distributed Transactions), "Process Manager" and "Saga" are often used interchangeably, yet represent nuanced architectural patterns:
- **Process Manager**: A stateful coordinator that reacts to domain events, tracks long-term business progress across bounded contexts, and issues commands.
- **Saga**: A pattern for managing distributed data consistency across microservices via a sequence of local transactions, executing compensating transactions when a step fails.

## Problem
Should `EricksonLopez.Processes` treat Process Managers and Sagas as two completely separate frameworks, or unify them under a single extensible model?

## Options
1. **Completely Disjoint Frameworks**: Distinct abstractions (`IProcessManager` vs `ISaga`) with duplicate lifecycle and persistence code.
2. **Unified Core Model with Specialized Saga Primitives**: A single stateful workflow core (`IProcess<TState>`) where a Saga (`ISaga<TState>`) is a specialized process manager equipped with explicit compensation step definitions and rollback execution mechanics.

## Decision
We adopt **Option 2: Unified Core Model with Specialized Saga Primitives**.

`ISaga<TState>` inherits from or composes `IProcess<TState>`, sharing state lifecycle, correlation, versioning, and persistence contracts, while introducing structured compensation handlers (`ICompensationHandler<TState, TStep>`).

## Rationale
- Maximizes code reuse and conceptual consistency.
- Avoids false dichotomies in real-world workflows that require both long-running state coordination and multi-step compensations.

## Consequences
- Common persistence and concurrency infrastructure applies uniformly to both Process Managers and Sagas.

## Rejected Alternatives
- Reinventing separate storage, correlation, and dispatch engines for Sagas vs Process Managers.
