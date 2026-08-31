# ADR-032: `ISpanParsable<TSelf>` and `ISpanFormattable` Implementation in Identifiers

## Status
**Accepted**

---

## Context
In high-throughput event-driven systems, identifier value objects (`ProcessId`, `Revision`, `ProcessVersion`, `CorrelationId`, `CausationId`, `MessageId`, `ProcessType`) are continuously parsed, serialized, and formatted across the hotpaths of message decoding, logging, and header mapping. Relying solely on `string`-based parsing causes unnecessary Heap allocations and Garbage Collection pressure.

---

## Decision
1. Implement `ISpanParsable<TSelf>` and `ISpanFormattable` across all strongly typed identifier structs in `EricksonLopez.Processes.Abstractions`.
2. Provide zero-allocation overloads for `Parse(ReadOnlySpan<char>)`, `TryParse(ReadOnlySpan<char>, ...)`, and `TryFormat(Span<char>, ...)`.

---

## Consequences
- **Positive**:
  - Zero Heap allocations when formatting directly into stack buffers (`Span<char>`) or `Utf8JsonWriter` instances.
  - Fully aligned with modern .NET BCL idioms and generic math/span interfaces.
- **Negative**:
  - Increased code surface and test verification requirements for each value object struct.
