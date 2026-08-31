# ADR-018: Source Generator Architecture and Dispatch Generation

## Status
**Accepted**

> **Revision History:**
> - Initial Decision: Roslyn Incremental Generator for static metadata discovery.
> - Scope Update: Added `AddGeneratedProcesses(IServiceCollection)` DI extension (ADR-038).

---

## Context
Developers need an ergonomic way to discover and register processes and sagas without writing boilerplate dispatch tables or resorting to runtime reflection scanning.

---

## Problem
What responsibilities should `EricksonLopez.Processes.Generator` fulfill, and how should it be architected using Roslyn Incremental Generators?

---

## Options
1. **Dynamic Expression / Runtime IL Generator**: Build a complex runtime reflection or expression generator.
2. **Roslyn Incremental Generator**: Build an `IIncrementalGenerator` that inspects `[ProcessDefinition]` and `[SagaDefinition]` attributes, emitting compile-time static registries and DI registration extensions (`AddGeneratedProcesses(...)`).

---

## Decision
We adopt **Option 2: Roslyn Incremental Generator for static registration**.

`EricksonLopez.Processes.Generator` analyzes syntax trees incrementally and emits two source files:

### File 1: `GeneratedProcessRegistry.g.cs`
Contains:
- `GeneratedProcessRegistry.RegisterDiscoveredProcesses(ProcessRegistry registry)`: Statically registers all discovered process and saga metadata without runtime reflection.
- `GeneratedProcessRegistry.CreateRegistry()`: Factory creating and populating a `ProcessRegistry`.

### File 2: `GeneratedProcessRegistryExtensions.g.cs`
Contains:
- `GeneratedProcessRegistryExtensions.AddGeneratedProcesses(this IServiceCollection services)`: DI extension method registering the pre-populated `ProcessRegistry` as an AOT-safe singleton.

---

## Current Scope & Capabilities

| Capability | Status |
| :--- | :--- |
| Metadata Registration (`ProcessType` + `ProcessVersion`) | ✅ Implemented |
| `RegisterDiscoveredProcesses(registry)` Method | ✅ Implemented |
| `CreateRegistry()` Factory | ✅ Implemented |
| `AddGeneratedProcesses(IServiceCollection)` Extension | ✅ Implemented |
| Event-to-Handler Routing Dispatch Tables | ❌ Out of Scope (Requires generic state types) |
| Dynamic Correlation Extractors | ❌ Out of Scope (Managed via `IProcessCorrelation<TEvent>`) |

---

## Consequences
- **Positive**: Sub-millisecond startup, zero memory overhead, 100% Native AOT compliance, and single-line DI configuration.
- **Negative**: Requires consumer projects to reference `Microsoft.Extensions.DependencyInjection.Abstractions` when calling `AddGeneratedProcesses()`.

---

## Rejected Alternatives
- Runtime assembly scanning (`Assembly.GetTypes()` / `Scrutor`).
- Runtime IL generation or dynamic proxy interception.
