# ADR-014: Timeout and Expiration Modeling

## Context
Processes such as electronic invoice certification or customer onboarding often require expiration deadlines (e.g. "Wait up to 48 hours for certification; if no response, expire the invoice").

## Problem
How should timeouts and deadlines be modeled without turning the library into a quartz scheduler or background timer manager?

## Options
1. Run in-memory `System.Threading.Timer` or `Task.Delay` inside the process instance.
2. Emit a declarative `TimeoutIntent(TimeSpan Delay, object TimeoutCommandOrEvent)` or store `ExpiresAt: DateTimeOffset` in process state, delegating timer execution to the host scheduler.

## Decision
We adopt **Option 2: Declarative `TimeoutIntent` & State Expiration**.

When a process wishes to set a deadline, it emits a `ProcessEffect.ScheduleTimeout(...)` intent and records `ExpiresAt` in its state. The hosting environment (e.g., Quartz, Hangfire, Azure Service Bus delayed messages, or Dapr timers) receives this intent and delivers the timeout message when due.

## Rationale
- In-memory timers die when pods restart or scale down.
- Keeps `Processes` completely transport- and scheduler-agnostic.

## Consequences
- The hosting infrastructure reads timeout intents and schedules external wakeups.

## Rejected Alternatives
- Embedding `System.Threading.Timer` or a database scheduler table in core.
