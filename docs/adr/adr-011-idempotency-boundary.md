# ADR-011: Idempotency Boundary

## Context
At-least-once message delivery in brokers (Kafka, RabbitMQ, SQS, Azure Service Bus) inevitably causes duplicate message delivery.

## Problem
How should duplicate event handling and idempotency be architected in `EricksonLopez.Processes`?

## Options
1. Require `Processes` to maintain a global deduplication database and transactional inbox table.
2. Separate idempotency concerns: The process state itself records milestone flags (e.g. `PaymentCompleted = true`) or processed message IDs, while transport/inbox deduplication belongs to the messaging and host layer.

## Decision
We adopt **Option 2: State milestone idempotency and transport/inbox decoupling**.

Process handlers are designed as pure state transitions. If an event is replayed:
1. The handler detects that the state has already transitioned past that milestone (`if (state.PaymentCompleted) return NoChange(state);`).
2. No duplicate command intents are emitted.
3. The inbox pattern or message deduplicator at the transport/adapter layer handles physical message deduplication before dispatch.

## Rationale
- Keeps the process core lightweight and free from duplicate storage tables.
- Aligns with DDD and state machine best practices.

## Consequences
- Process handlers should explicitly check state preconditions before emitting outgoing command intents.

## Rejected Alternatives
- Embedding an entire inbox store engine inside the process primitives library.
