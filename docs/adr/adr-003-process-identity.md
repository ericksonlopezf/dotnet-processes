# ADR-003: Process Identity Representation

## Context
Process instances require unique, persistent, and indexable identifiers across databases and distributed logs.

## Problem
How should `ProcessId` be modeled in the core library?

## Options
1. `System.Guid` directly.
2. `string` directly.
3. Strongly typed `readonly record struct ProcessId` wrapping `Guid` with support for Guid v7 (time-ordered) and string serialization.

## Decision
We adopt **Option 3: `readonly record struct ProcessId`**.

`ProcessId` is a lightweight, zero-allocation value type wrapping `Guid`, providing factory methods `ProcessId.NewId()` (Guid v7 or random v4), parsing, formatting, and type-safe equality.

## Rationale
- Zero heap allocation.
- Prevents primitive obsession and accidental parameter transposition.
- Guid v7 ensures B-tree index friendly locality in SQL/NoSQL databases.

## Consequences
- Highly ergonomic, AOT-friendly, zero boxing in comparisons and hashing.

## Rejected Alternatives
- Pure `string` (causes excessive heap allocations and non-standard indexing).
- Generic `ProcessId<T>` (adds generic complexity without business value).
