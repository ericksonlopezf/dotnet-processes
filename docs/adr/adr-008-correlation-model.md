# ADR-008: Correlation, Causation, and Message Identity Model

## Context
Event-driven workflows must trace cause-and-effect across multiple messages and determine which process instance should handle an incoming event.

## Problem
How should correlation and causation identifiers be structured in `EricksonLopez.Processes`?

## Options
1. Untyped strings or dictionary baggage.
2. Strongly typed zero-allocation value types (`CorrelationId`, `CausationId`, `MessageId`) paired with compile-time correlation extractors (`IProcessCorrelation<TEvent>`).

## Decision
We adopt **Option 2: Strongly typed value types with static correlation extractors**.

- `CorrelationId`: Identifies the overarching business transaction / root conversation.
- `CausationId`: Identifies the immediate direct cause (the prior message ID) that triggered this step.
- `MessageId`: Identifies the unique identity of this specific message/event.
- `IProcessCorrelation<TEvent>`: Interface defining `CorrelationKey Extract(TEvent @event)` to locate the target `ProcessId` without runtime reflection.

## Rationale
- Zero allocation `readonly record struct` types.
- Static correlation extractors enable AOT-safe lookup without property reflection or expression compilation.

## Consequences
- Fast compile-time verified message correlation.

## Rejected Alternatives
- Property-name reflection scanning (e.g. searching for `.OrderId` at runtime).
