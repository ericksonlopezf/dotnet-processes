# ADR-001: Core Responsibility and Architectural Boundaries

## Context
In distributed event-driven systems, business operations frequently span multiple aggregates, bounded contexts, and asynchronous messaging boundaries. Developers often conflate workflow orchestration with infrastructure concerns such as network transport, transaction management, message storage, database migrations, and scheduling.

## Problem
What is the exact scope of responsibility for `EricksonLopez.Processes`, and what must be strictly excluded to preserve minimalism, modularity, and Native AOT compatibility?

## Options
1. **Monolithic Workflow Engine**: Embed message broker adapters, database drivers (EF Core/Dapper), timer schedulers, and distributed lock mechanisms.
2. **Pure Process Manager & Saga Primitives**: Model stateful workflows, deterministic state transitions, correlation mapping, and explicit compensation actions as pure C# abstractions, leaving transport, persistence execution, outbox publishing, and scheduling to the host infrastructure or surrounding ecosystem packages.

## Decision
We adopt **Option 2: Pure Process Manager & Saga Primitives**.

`EricksonLopez.Processes` defines how stateful workflows react to events, transition state, emit output intents (commands/events), and handle compensation. It has zero awareness of specific databases, message brokers, or distributed locks.

## Rationale
- Eliminates heavy dependencies and prevents runtime reflection.
- Keeps core abstractions 100% testable in-memory with deterministic clocks.
- Cleanly integrates with `EricksonLopez.Events`, `EricksonLopez.Outbox`, and `EricksonLopez.Mediator` via composition and intent adapters.

## Consequences
- **Positive**: Blazing fast performance, Native AOT compliance, zero trimming warnings, minimal package surface.
- **Negative**: Applications must supply persistence stores (`IProcessStore<TState>`) and wire output intents to their preferred outbox or transport.

## Rejected Alternatives
- Embedding RabbitMQ/Kafka transports or EF Core in the core package (violates Clean Architecture and AOT principles).
