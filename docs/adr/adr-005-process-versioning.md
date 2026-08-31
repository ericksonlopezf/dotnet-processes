# ADR-005: Process Versioning Strategy

## Context
Long-running processes may execute across days, months, or years. During this time, business workflows evolve and new contract definitions (`v2`, `v3`) are deployed while existing instances (`v1`) remain in flight.

## Problem
How should `ProcessVersion` be represented and managed across long-running lifecycles?

## Options
1. Single mutable definition without versioning (forces in-flight migration or breaks running instances).
2. SemVer string (`"1.2.0"`).
3. Strongly typed integer versioning `readonly record struct ProcessVersion(int Value)` paired with explicit version coexistence and state migrators.

## Decision
We adopt **Option 3: Strongly typed integer `ProcessVersion` with version coexistence**.

Each process definition states its target `ProcessVersion`. Stored instances record their creation version. The runtime dispatches events to the matching version handler or invokes an `IProcessStateMigrator<TFrom, TTo>` when configured.

## Rationale
- Fast integer comparisons and index storage.
- Allows `v1` and `v2` process handlers to run simultaneously in the same host application.

## Consequences
- Clean coexistence of legacy in-flight instances alongside newly initiated processes.

## Rejected Alternatives
- Complex string SemVer matching in hot dispatch path.
