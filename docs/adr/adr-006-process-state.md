# ADR-006: Process State Immutability and Schema Design

## Context
Process state stores the intermediate business data and execution milestones of a long-running workflow.

## Problem
Should process state be modeled as mutable objects or immutable data records?

## Options
1. Mutable classes with property setters (`state.IsPaid = true`).
2. Immutable data records (`record` / `readonly struct`) where state transitions produce new instances (`return state with { IsPaid = true };`).

## Decision
We adopt **Option 2: Immutable data records**.

Process state types implement marker interface `IProcessState` and are recommended to be C# `record` types. State transition handlers accept current state and event, returning a `ProcessTransitionResult<TState>` containing the updated state.

## Rationale
- Thread-safe and free from race conditions during concurrent handler evaluations.
- Pure state reducers enable deterministic testing and snapshot auditing.
- Eliminates subtle side-effects where failed transitions leave in-memory state dirty.

## Consequences
- Requires using `with` expressions or factory methods for state updates.

## Rejected Alternatives
- Mutable entity classes with change tracking (adds heavy runtime overhead and concurrency hazards).
