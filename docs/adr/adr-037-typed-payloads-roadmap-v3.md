# ADR-037: Typed Payloads in ProcessEffect and CompensationStep — Roadmap v3.0

## Status
**Accepted (Technical Debt Formally Acknowledged)**

---

## Context
`ProcessEffect.Command`, `ProcessEffect.Event`, `ProcessEffect.ScheduleTimeout`, and `CompensationStep` currently store payload instances as `object`. This design was adopted during early phases for maximum flexibility and to permit heterogeneous lists (`IReadOnlyList<ProcessEffect>`) without requiring generic base types.

Functional parity audits identified that `object` payloads introduce potential AOT serialization friction if downstream dispatchers attempt polymorphic serialization without pre-registered `JsonSerializerContext` metadata.

---

## Problem
How to introduce fully typed payloads into `ProcessEffect` and `CompensationStep` without:
1. Triggering an immediate breaking change across all existing consumers?
2. Compromising the ergonomics of heterogeneous effect lists?
3. Introducing excessive generic type parameter bloat?

---

## Decision for Future Major Release (v3.0.0 Target)
1. **`CompensationStep.Payload`**: Migrate underlying storage to `System.Text.Json.JsonElement` in v3.0.0, enabling direct deserialization using source-generated JSON contexts without reflection.
2. **`ProcessEffect.Command` & `ProcessEffect.Event`**: Introduce typed record variants:
   ```csharp
   public sealed record TypedCommand<T>(T Payload) : ProcessEffect where T : notnull;
   ```
   while maintaining existing non-generic variants for backward compatibility.
3. **Current Mitigation in v1.x / v2.x**:
   - Provide generic factory helpers: `ProcessEffect.CreateCommand<T>(T payload)` (captures `CommandType = typeof(T).Name`).
   - Provide type-safe extractors: `GetPayload<T>()` and `TryGetPayload<T>(out T? payload)`.
   - Provide compensation extractors: `CompensationStep.ExtractPayload<T>()` and `TryExtractPayload<T>()`.

---

## Rationale for Deferral
- **SemVer Integrity**: Replacing `object` with `JsonElement` is an explicit breaking change requiring a major release.
- **Consumer Dispatcher Boundary**: In practice, downstream dispatchers (such as `OutboxProcessDispatcher` or `MediatorProcessDispatcher`) already cast payloads via `GetPayload<T>()` or map them to concrete outbox models using compile-time delegates.

---

## Consequences
- **Positive**: Technical debt is documented, prioritized, and scheduled for the next major release cycle.
- **Negative**: Downstream dispatchers must use `GetPayload<T>()` or string type hints when dispatching heterogeneous effect lists.
