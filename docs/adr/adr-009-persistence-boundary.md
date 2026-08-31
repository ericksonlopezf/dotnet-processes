# ADR-009: Persistence Boundary and Storage Contracts

## Context
Process instances must survive application crashes, container restarts, and machine failovers. They must be persisted to durable storage (e.g. PostgreSQL, SQL Server, DynamoDB, MongoDB, Redis).

## Problem
How should persistence contracts be designed without leaking database details or ORM specifics into the domain core?

## Options
1. Embed Entity Framework Core `DbContext` or Dapper queries into the core library.
2. Define a minimal, persistence-agnostic abstraction `IProcessStore<TState>`.

## Decision
We adopt **Option 2: Minimal persistence abstraction `IProcessStore<TState>`**.

The contract defines:
- `ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken)`
- `ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken)`
- `ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken)`

`ProcessSaveResult` represents explicit outcomes: `Success`, `ConcurrencyConflict`, or `NotFound`.

## Rationale
- Decouples core logic completely from SQL/NoSQL drivers.
- Allows applications to implement transactional store wrappers combining state persistence with outbox writes in a single DB transaction.

## Consequences
- Clean Architecture compliance. Easy to mock and unit test with in-memory stores.

## Rejected Alternatives
- Directly referencing EF Core / Dapper in core abstractions.
