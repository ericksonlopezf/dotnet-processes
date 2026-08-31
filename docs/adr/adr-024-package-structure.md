# ADR-024: Package Decomposition Strategy

## Context
A well-factored library ecosystem minimizes package bloat while strictly isolating concerns and optional adapters.

## Problem
What is the optimal set of NuGet packages for `EricksonLopez.Processes`?

## Options
1. Single monolithic NuGet package containing everything (core, DI, generator, JSON serializer).
2. Clean decomposition:
   - `EricksonLopez.Processes.Abstractions`: Pure contracts, IDs, states, intents, store interfaces.
   - `EricksonLopez.Processes`: Core execution coordinator, saga engine, transition handlers, correlation lookup.
   - `EricksonLopez.Processes.Generator`: Roslyn source generator (development dependency, no runtime footprint).
   - `EricksonLopez.Processes.SystemTextJson`: AOT-safe JSON serialization helpers.
   - `EricksonLopez.Processes.DependencyInjection`: Optional `IServiceCollection` extension methods.

## Decision
We adopt **Option 2: Clean decomposition into 5 targeted packages**.

## Rationale
- Allows domain layers to reference only `Abstractions`.
- Allows infrastructure/application layers to add DI and serialization as needed.
- Generator acts as a build-time analyzer with zero runtime dll overhead.

## Consequences
- Clear separation of concerns with minimal friction.

## Rejected Alternatives
- Fragmenting into dozens of micro-packages or dumping everything into one monolithic dll.
