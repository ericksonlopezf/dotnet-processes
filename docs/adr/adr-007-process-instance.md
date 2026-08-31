# ADR-007: ProcessInstance Composition and Metadata

## Context
A process instance combines technical execution metadata (identifiers, revision, timestamps, lifecycle status) with domain-specific state payload `TState`.

## Problem
How should metadata and user state be structured?

## Options
1. Require domain state to inherit from a base `ProcessInstance` class (intrusive domain pollution).
2. Separate technical wrapper `ProcessInstance<TState>` encapsulating metadata and `TState State`.

## Decision
We adopt **Option 2: Generic wrapper `ProcessInstance<TState>`**.

`ProcessInstance<TState>` is a strongly typed container containing:
- `ProcessId Id`
- `ProcessType Type`
- `ProcessVersion Version`
- `ProcessStatus Status`
- `Revision Revision` (for optimistic concurrency)
- `CorrelationId CorrelationId`
- `DateTimeOffset CreatedAt`
- `DateTimeOffset UpdatedAt`
- `DateTimeOffset? CompletedAt`
- `TState State`

## Rationale
- Keeps domain state pure C# records without base class coupling.
- Cleanly isolates persistence metadata needed by storage adapters.

## Consequences
- Storage adapters map `ProcessInstance<TState>` directly to database columns and JSON/binary payloads.

## Rejected Alternatives
- Base class inheritance forcing `Id`, `Version`, etc. into domain records.
