# ADR-035: SagaCompensationEngine Visibility Restriction to Internal

## Status
**Accepted**

---

## Context
`SagaCompensationEngine` contains the low-level logic that executes recorded saga compensation steps in reverse LIFO order: step iteration, `ICompensationHandler<TState>` invocation, per-step fault isolation, and compensation effect accumulation.

During API surface reviews, this class was previously exposed as `public static class`, leaking an internal coordination implementation detail to consumers of `EricksonLopez.Processes`.

---

## Problem
Should `SagaCompensationEngine` remain part of the public API surface of the core package?

---

## Options Considered
1. **Retain `public`**: Allow consumers to invoke the low-level engine directly.
2. **Make `internal` + `[EditorBrowsable(Never)]`**: Restrict the engine as an internal implementation detail; expose only `ProcessCoordinator<TState>.CompensateAsync<TSaga>` as the public entry point.

---

## Decision
We adopt **Option 2: `internal static class SagaCompensationEngine`** with `[EditorBrowsable(EditorBrowsableState.Never)]`.

The sole public entry point for executing saga compensation is `ProcessCoordinator<TState>.CompensateAsync<TSaga>(...)`.

---

## Rationale
- **Minimal Public API Surface**: Consumers coordinate workflows via `ProcessCoordinator`; low-level step iteration is an engine implementation detail.
- **Encapsulation Stability**: Internalizing `SagaCompensationEngine` permits internal optimization without triggering breaking SemVer releases.
- **OCC Consistency**: `ProcessCoordinator.CompensateAsync` ensures that compensation status mutations adhere to optimistic concurrency control tokens.

---

## Consequences
- **Positive**:
  - Cleaner and more cohesive public API.
  - Freedom to refactor compensation internals without SemVer breaking changes.
  - Guaranteed OCC invariants for saga compensations.
- **Negative**:
  - Direct invocations must use `ProcessCoordinator.CompensateAsync`.

---

## Implementation Reference
- `src/EricksonLopez.Processes/Compensation/SagaCompensationEngine.cs`
- `src/EricksonLopez.Processes/Execution/ProcessCoordinator.cs`
