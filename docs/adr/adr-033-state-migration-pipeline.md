# ADR-033: Sequential State Schema Migration Pipeline

## Status
**Accepted**

---

## Context
In long-running business processes, instances may persist across database snapshots for days, months, or years while application code evolves through successive schema versions (V1 -> V2 -> V3). The framework requires a deterministic, strongly typed, and reflection-free mechanism to chain stepwise migrators (`IProcessStateMigrator<TFrom, TTo>`) during instance hydration.

---

## Decision
1. Introduce `ProcessStateMigrationPipeline` and `ProcessStateMigrationPipelineBuilder` in `EricksonLopez.Processes`.
2. Provide a fluent API allowing developers to chain `IProcessStateMigrator` instances or mapping delegates (`AddStep`).
3. Enforce validation during pipeline construction ensuring that the `ToVersion` of each step matches the `FromVersion` of the subsequent step.
4. Produce a compiled composite `IProcessStateMigrator<TFrom, TTo>` executing the migration chain deterministically.

---

## Consequences
- **Positive**:
  - Clean encapsulation of complex multi-version schema evolution (e.g. V1 -> V2 -> V3).
  - 100% Native AOT compliance with zero dynamic invocation or reflection.
  - Stepwise validation preventing version mismatch during pipeline composition.
- **Negative**:
  - Requires explicit registration of stepwise migrators for each schema version increment.
