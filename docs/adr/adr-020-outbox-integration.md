# ADR-020: Outbox Integration Contract

## Context
When a process transitions, it typically updates its durable state AND emits commands/events (e.g. `PaymentRequested`, `OrderCompleted`). If the database updates but the outgoing message fails to send, the system enters an inconsistent state (dual-write hazard).

## Problem
How should `EricksonLopez.Processes` integrate with `EricksonLopez.Outbox` without creating tight circular package dependencies?

## Options
1. Have `Processes` depend directly on `EricksonLopez.Outbox` and manage database transactions.
2. Have `Processes` return explicit `ProcessEffect` output intents in `ProcessTransitionResult<TState>`, enabling application/persistence adapters to commit both state and outbox records in the same transactional unit of work.

## Decision
We adopt **Option 2: Explicit `ProcessEffect` intents committed atomically by storage adapters**.

The `ProcessCoordinator` executes the transition, gathers emitted `ProcessEffect.Command` and `ProcessEffect.Event` intents, and passes them to the storage/outbox pipeline within the user's database transaction boundary.

## Rationale
- Completely avoids coupling `Processes` to specific transaction managers.
- Guarantees transactional consistency and zero dual-write bugs.

## Consequences
- Clean, composable integration with `EricksonLopez.Outbox`.

## Rejected Alternatives
- Direct database transaction management in `Processes.Abstractions`.
