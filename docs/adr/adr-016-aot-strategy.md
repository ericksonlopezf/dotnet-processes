# ADR-016: Native AOT Strategy

## Context
.NET 10 provides first-class support for Native AOT (Ahead-of-Time compilation) and single-file executables, delivering near-instant startup, reduced memory footprint, and smaller binary sizes. However, Native AOT prohibits runtime dynamic code generation (`IL emit`, `System.Reflection.Emit`, `Expression.Compile`) and heavily restricts runtime reflection.

## Problem
How should `EricksonLopez.Processes` achieve 100% Native AOT compliance without runtime reflection?

## Options
1. Use runtime reflection with `[RequiresUnreferencedCode]` annotations (breaks AOT compatibility and triggers warnings).
2. Design static generic dispatch, explicit registration, and compile-time Roslyn Source Generators.

## Decision
We adopt **Option 2: Pure static generic dispatch and Roslyn Source Generation**.

All dispatch paths, correlation lookups, and state transitions are bound at compile time using generic interfaces and source-generated dispatch tables. No `Activator.CreateInstance`, `Assembly.GetTypes()`, or runtime dynamic expressions are used anywhere in the codebase.

Furthermore, side-effect intents (`ProcessEffect`) and compensation milestones (`CompensationStep`, `CompensationAction`) provide strongly typed generic factories (`CreateCommand<T>`, `CreateEvent<T>`, `CreateTimeout<T>`, `Create<TPayload>`) and typed extraction helpers (`ExtractPayload<T>`, `TryExtractPayload<T>`) to guarantee 100% Native AOT type safety without type erasure or reflection dependencies.

## Rationale
- Native AOT is a core design requirement, not an afterthought.
- Delivers maximum execution speed and zero trimming regressions.

## Consequences
- Requires using explicit registration methods or the `EricksonLopez.Processes.Generator` source generator.

## Rejected Alternatives
- Runtime reflection scanning for `IProcess` implementations.
