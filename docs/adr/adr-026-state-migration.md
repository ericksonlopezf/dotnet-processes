# ADR-026: Explicit State Migration Strategy

## Context
As business workflows evolve, the stored state schema for a long-running process may change (e.g. adding required fields, restructuring data from `OrderStateV1` to `OrderStateV2`).

## Problem
How should stored process instances be migrated between state schemas safely without runtime reflection or dynamic schema mutations?

## Options
1. Implicit ad-hoc reflection mapper.
2. Explicit, deterministic state migration contract `IProcessStateMigrator<TFrom, TTo>`.

## Decision
We adopt **Option 2: Explicit state migrator `IProcessStateMigrator<TFrom, TTo>`**.

When loading a process stored under an older schema version:
```csharp
public interface IProcessStateMigrator<in TFrom, out TTo>
{
    TTo Migrate(TFrom sourceState);
}
```
The application registers migrators explicitly. When a v1 record is loaded by a v2 coordinator, the migrator executes deterministically and updates the instance metadata version.

## Rationale
- 100% type-safe, deterministic, and AOT-compliant.
- Audit trails clearly document how old records were upgraded.

## Consequences
- Developers write explicit transformation functions for breaking state changes.

## Rejected Alternatives
- Unchecked dynamic JSON object mutations.
