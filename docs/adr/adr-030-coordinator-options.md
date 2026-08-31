# ADR-030: Decoupled Coordinator Configuration Options

## Status
**Accepted**

---

## Context
`ProcessCoordinator<TState>` requires configurable parameters such as the maximum retry attempts for Optimistic Concurrency Control conflicts (`MaxConcurrencyRetries`) and the base delay for exponential backoff (`InitialBackoffDelay`). Previously, these parameters were either hardcoded or required bloated constructor overloads with primitive parameters.

---

## Decision
1. Introduce the `ProcessCoordinatorOptions` class with sensible defaults (`MaxConcurrencyRetries = 3`, `InitialBackoffDelay = 50ms`).
2. Support configuring the coordinator via the explicit constructor overload `ProcessCoordinator(store, options, timeProvider, backoffStrategy)`.
3. Expose fluent configuration in `Microsoft.Extensions.DependencyInjection` via `AddProcessCoordinator<TState>(Action<ProcessCoordinatorOptions>)`.

---

## Consequences
- **Positive**:
  - Clean, extensible, and strongly typed configuration API.
  - Flexibility to tune retry policies to match specific database contention profiles.
  - 100% Native AOT and trimming compliant.
- **Negative**:
  - Introduces a single configuration object allocation during application startup (0 allocations in runtime execution hotpaths).
