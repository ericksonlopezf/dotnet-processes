# ADR-031: Dedicated Testing Package (`EricksonLopez.Processes.Testing`)

## Status
**Accepted**

---

## Context
Consumers of `EricksonLopez.Processes` need to write isolated unit and integration tests for their process handlers and sagas without requiring a live relational database instance (PostgreSQL, SQL Server, etc.). Keeping `InMemoryProcessStore` inside the abstractions or core package would add test double utility code to production assemblies.

---

## Decision
1. Introduce the dedicated package `EricksonLopez.Processes.Testing`.
2. Provide `InMemoryProcessStore<TState>` as a thread-safe, high-performance implementation of `IProcessStore<TState>` with atomic Compare-And-Swap (CAS) simulation.
3. Update all samples, showcases, and test harnesses to consume this package.

---

## Consequences
- **Positive**:
  - `Abstractions` and `Core` remain pure, minimal, and free of testing doubles.
  - Consumers can reference `<PackageReference Include="EricksonLopez.Processes.Testing" />` in test projects for instant, isolated in-memory test setups.
- **Negative**:
  - Adds one additional NuGet package to publish and maintain in the CI/CD pipeline.
