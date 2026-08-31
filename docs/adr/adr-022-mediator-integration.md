# ADR-022: Mediator Ecosystem Integration

## Context
When a process issues commands (e.g. `ProcessEffect.Command(new ChargePaymentCommand(...))`), those commands may need in-process dispatch via `EricksonLopez.Mediator`.

## Problem
Should `EricksonLopez.Processes` have a hard reference to `EricksonLopez.Mediator`?

## Options
1. Hard reference to `EricksonLopez.Mediator` in core.
2. Produce agnostic `CommandIntent` effects that can be dispatched by application workers or an optional mediator adapter.

## Decision
We adopt **Option 2: Agnostic command intents with optional dispatch adapters**.

The process produces pure command intents. The host execution pipeline or mediator dispatcher iterates over emitted effects and dispatches them via `IMediator.SendAsync(...)` or writes them to an outbox.

## Rationale
- Preserves the purity of `Processes` core.
- Enables usage in microservices that use message brokers directly without an in-memory mediator.

## Consequences
- Flexible dispatch architecture.

## Rejected Alternatives
- Requiring `IMediator` inside the `IProcess` handler signature.
