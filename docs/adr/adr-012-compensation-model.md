# ADR-012: Explicit Compensation Model and Lifecycle

## Context
When a distributed saga step encounters an unrecoverable failure (e.g. `ShipmentCreationFailed` after `PaymentCompleted` and `InventoryReserved`), previously completed steps must be compensated (e.g. `RefundPayment`, `ReleaseInventory`).

## Problem
How should compensating actions be modeled, ordered, and executed?

## Options
1. Magical automated rollback (impossible in distributed systems).
2. Explicit compensation steps recorded during forward execution, executed in reverse dependency order upon failure, transitioning through `Compensating` -> `Compensated` or `Failed` status.

## Decision
We adopt **Option 2: Explicit reverse-order compensation model**.

- As each forward saga step completes successfully, it records a `CompensationStep` (with serialized or typed payload) in the saga's state.
- When an unrecoverable business failure occurs:
  1. The saga status updates to `ProcessStatus.Compensating`.
  2. The saga yields compensating command intents in reverse order (LIFO: last completed step compensated first).
  3. Execution is orchestrated through `ProcessCoordinator.CompensateAsync`, which encapsulates the internal `SagaCompensationEngine` with per-step fault isolation.
  4. Upon receiving confirmation of all compensations, status updates to `ProcessStatus.Compensated`.
  5. If a compensation step fails, status updates to `ProcessStatus.Failed` with explicit compensation failure metadata, flagging for manual intervention.

## Rationale
- Completely transparent and deterministic.
- Clear error tracking when third-party compensation APIs fail.
- Internal engine encapsulation prevents leaky infrastructure abstractions.

## Consequences
- Sagas must define explicit compensation logic for every reversible step.

## Rejected Alternatives
- Hiding compensations in opaque runtime lambda closures that cannot be serialized or persisted across node restarts.
