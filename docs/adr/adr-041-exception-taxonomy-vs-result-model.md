# ADR-041: Exception Taxonomy vs. Result Model Boundary in Process Coordination

## Status
Accepted

## Date
2026-09-15

---

## Context
In distributed process managers and sagas, failures can occur during both forward execution and reverse compensation unwinding. The framework provides an explicit taxonomy of exceptions in `EricksonLopez.Processes.Abstractions.Exceptions` (including `ProcessException`, `ProcessNotFoundException`, `ConcurrencyConflictException`, `InvalidProcessTransitionException`, and `CompensationFailedException`), alongside strongly typed execution outcomes encapsulated in `ProcessExecutionResult<TState>` and `ProcessStatus` (`Failed`, `Compensated`).

During the comprehensive technical audit, an architectural divergence was reviewed: `CompensationFailedException` was defined as a public exception type with comprehensive unit tests and documentation, but was never thrown by `ProcessCoordinator` or `SagaCompensationEngine`. Instead, compensation failures transition the persistent process instance to `ProcessStatus.Failed` and return a successful `ValueTask<ProcessExecutionResult<TState>>` representing the failed state.

---

## Problem
Should critical failures during saga compensation throw an unhandled `CompensationFailedException`, terminating execution via the exception pipeline, or should the framework return a `ProcessExecutionResult<TState>` with `ProcessStatus.Failed` and retain `CompensationFailedException` for external dead-letter queue (DLQ) signaling?

---

## Options Considered
1. **Always Throw `CompensationFailedException`**:
   - `CompensateAsync` throws `CompensationFailedException` whenever a compensation step fails or retries are exhausted.
   - *Drawback*: Bypasses result-oriented workflows; prevents callers from obtaining the partially compensated state, updated revision token, and accumulated compensation side-effects without parsing exception properties.

2. **Result-Oriented Lifecycle with Semantically Available Exception (Adopted)**:
   - `ProcessCoordinator.CompensateAsync` prioritizes durable state integrity: when compensation fails or retries are exhausted, it atomically persists the instance with `ProcessStatus.Failed`, records diagnostic telemetry, and returns a `ProcessExecutionResult<TState>`.
   - `CompensationFailedException` is preserved in `EricksonLopez.Processes.Abstractions` as the canonical exception type for messaging consumers, outbox processors, and background dispatch workers to throw when routing failed sagas to dead-letter queues or alerting operators.

---

## Decision
We adopt **Option 2: Result-Oriented Lifecycle with Canonical Exception Support**.

- **Coordinator Contract**: `ProcessCoordinator<TState>.CompensateAsync` does not throw `CompensationFailedException`. It returns `ProcessExecutionResult<TState>` with `result.Instance.Status == ProcessStatus.Failed`.
- **Exception Role**: `CompensationFailedException` remains a first-class citizen in the public exception hierarchy in `EricksonLopez.Processes.Abstractions`, intended for:
  1. Downstream host applications or message bus consumers that require an explicit exception to trigger DLQ / poison message handling.
  2. Custom compensation handlers and middleware that manually signal unrecoverable rollback failures.

---

## Rationale
- **Durable State Protection**: Throwing an unhandled exception from the coordinator risks losing the in-memory context of which steps succeeded and which failed. Persisting `ProcessStatus.Failed` guarantees atomic auditability.
- **Native AOT & Performance**: Exception handling paths in .NET introduce stack trace generation overhead. Business and infrastructure failures during compensation are expected domain outcomes that should be modeled as state.
- **Backward Compatibility**: Maintains binary compatibility across the `Abstractions` package without breaking existing callers that inspect `ProcessExecutionResult<TState>`.

---

## Consequences
- **Positive**:
  - Deterministic and auditable compensation lifecycle.
  - Clear architectural demarcation between coordinator return values and host-level exception routing.
  - Eliminates the contradiction where documentation claimed `CompensateAsync` threw `CompensationFailedException`.
- **Negative**:
  - Callers must inspect `result.Instance.Status == ProcessStatus.Failed` rather than relying exclusively on `catch (CompensationFailedException)` around `CompensateAsync`.
