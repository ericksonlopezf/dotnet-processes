# ADR-029: Performance, Memory Allocation, and Data Structures Policy

## Context
High-volume distributed systems process thousands of domain events per second. The process manager library must not introduce heap churn, garbage collection pauses, or CPU overhead.

## Problem
What performance design rules govern `EricksonLopez.Processes`?

## Options
1. Use standard heap-allocated objects, LINQ queries, reflection delegates, and `Task<T>`.
2. Apply strict performance engineering: `ValueTask`, `readonly struct` identifiers, zero unnecessary allocations on hot paths, static generic dispatch, and benchmark-driven verification with BenchmarkDotNet.

## Decision
We adopt **Option 2: High performance and low allocation engineering rules**.

- **Return types**: Use `ValueTask` and `ValueTask<T>` for asynchronous operations to eliminate `Task` heap allocation on synchronously completing operations.
- **Identifiers**: `ProcessId`, `ProcessType`, `ProcessVersion`, `Revision`, `CorrelationId`, `CausationId`, `MessageId` are `readonly record struct` value types.
- **Collections**: Use `ReadOnlyMemory<T>`, `ReadOnlySpan<T>`, or immutable structures instead of allocating temporary arrays or LINQ enumerators.
- **Dispatch**: 100% static compile-time binding via generic handlers and generated dispatch tables.
- **Benchmarking**: BenchmarkDotNet test suite validates memory allocation and execution speed for state transitions and correlation lookups.

## Rationale
- Minimizes Gen0/Gen1 GC pressure and enables predictable high-throughput execution.
- Native AOT produces optimal native assembly without JIT de-optimization.

## Consequences
- Requires discipline in API design to avoid hidden boxing or delegate allocations.

## Rejected Alternatives
- Unbounded LINQ queries and reflection-based dynamic invocation in hot execution paths.
