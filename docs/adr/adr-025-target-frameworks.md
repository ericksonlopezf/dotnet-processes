# ADR-025: Target Frameworks Policy

## Status
Accepted

## Date
2026-09-04

## Context
.NET evolves rapidly, introducing superior high-performance primitives (`TimeProvider`, `FrozenDictionary`, C# 13/14 features, enhanced AOT compiler diagnostics).

## Problem
Which target frameworks should be supported by `EricksonLopez.Processes`?

## Options
1. Legacy `netstandard2.0` / `netstandard2.1` (restricts modern BCL features, impairs Native AOT).
2. Modern LTS/Current target `net10.0` (with optional `net9.0`).

## Decision
We adopt **Option 2: Modern `net10.0` target for all runtime libraries**.

- **Runtime Assemblies**: Target `net10.0` exclusively across `Abstractions`, `Processes`, `DependencyInjection`, `Events`, `Mediator`, `Outbox`, `SystemTextJson`, `Testing`, and all `Storage.*` database provider packages.
- **Roslyn Tooling Exception**: `EricksonLopez.Processes.Generator` and `EricksonLopez.Processes.Analyzers` target `netstandard2.0` solely to comply with the mandatory execution environment requirements of the Roslyn compiler host (`csc`) and IDE language servers.

## Rationale
- Leverages built-in `TimeProvider`, `ReadOnlySpan<T>`, `FrozenSet<T>`, and latest Native AOT compiler enhancements.
- Avoids polyfill packages and obsolete API shims for runtime execution.
- Ensures seamless source generation and diagnostic analysis across all IDEs and build systems via `netstandard2.0`.

## Consequences
- High-performance, clean runtime codebase using modern C# features.
- Strict isolation of `netstandard2.0` to compilation-only tooling artifacts.

## Rejected Alternatives
- Supporting legacy .NET Framework or .NET Standard 2.0 for runtime assemblies.
