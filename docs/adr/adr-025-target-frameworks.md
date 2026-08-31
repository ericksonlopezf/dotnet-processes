# ADR-025: Target Frameworks Policy

## Context
.NET evolves rapidly, introducing superior high-performance primitives (`TimeProvider`, `FrozenDictionary`, C# 13/14 features, enhanced AOT compiler diagnostics).

## Problem
Which target frameworks should be supported by `EricksonLopez.Processes`?

## Options
1. Legacy `netstandard2.0` / `netstandard2.1` (restricts modern BCL features, impairs Native AOT).
2. Modern LTS/Current target `net10.0` (with optional `net9.0`).

## Decision
We adopt **Option 2: Modern `net10.0` target**.

## Rationale
- Leverages built-in `TimeProvider`, `ReadOnlySpan<T>`, `FrozenSet<T>`, and latest Native AOT compiler enhancements.
- Avoids polyfill packages and obsolete API shims.

## Consequences
- High-performance, clean codebase using modern C# features.

## Rejected Alternatives
- Supporting legacy .NET Framework or .NET Standard 2.0.
