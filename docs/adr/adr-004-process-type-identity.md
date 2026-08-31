# ADR-004: Process Type Identity Strategy

## Context
Process instances stored in durable databases must be associated with their logical process definition independently of internal .NET CLR namespace, type names, or assembly versions.

## Problem
How should `ProcessType` be represented and persisted?

## Options
1. `Type.AssemblyQualifiedName` or `Type.FullName` at runtime.
2. Strongly typed `readonly record struct ProcessType` wrapping an explicit logical string identifier (e.g. `"order.fulfillment"`).

## Decision
We adopt **Option 2: Explicit logical `ProcessType` string identifier**.

Each process definition declares its unique `ProcessType` via an explicit attribute or property (e.g., `[ProcessType("order.fulfillment")]`).

## Rationale
- Decouples persistent data from .NET codebase refactorings (renaming classes, moving folders, upgrading assemblies).
- Native AOT friendly: eliminates `Type.GetType(string)` lookups at runtime.

## Consequences
- Requires developers to supply a unique string token per process definition.
- Generator/Source generators can validate uniqueness and generate static dispatch tables.

## Rejected Alternatives
- CLR Type serialization (fragile, breaks across refactorings, unsafe with AOT).
