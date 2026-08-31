# ADR-021: Events Ecosystem Integration

## Context
In the `EricksonLopez.*` ecosystem, domain and integration events are defined with rich metadata and contracts by `EricksonLopez.Events`.

## Problem
How should `EricksonLopez.Processes` interact with `EricksonLopez.Events` without duplicating event contracts or forcing unnecessary dependencies?

## Options
1. Duplicate event abstractions inside `Processes`.
2. Design process handlers around generic event types (`TEvent`) so that any POCO or `EricksonLopez.Events` contract can be consumed seamlessly without forcing a required dependency.

## Decision
We adopt **Option 2: Generic event parameters (`TEvent`) with full ecosystem interoperability**.

`IProcessHandler<TState, TEvent>` accepts any `TEvent` type. Correlation extractors can extract event metadata (such as `CorrelationId`, `CausationId`, `EventId`) directly from `EricksonLopez.Events` metadata or custom record properties.

## Rationale
- Zero unnecessary package coupling.
- 100% interoperability with `EricksonLopez.Events`.

## Consequences
- Clean separation of concerns.

## Rejected Alternatives
- Reinventing a duplicate event taxonomy inside `Processes`.
