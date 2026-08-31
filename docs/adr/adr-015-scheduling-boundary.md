# ADR-015: Scheduling Boundary

## Context
Delayed workflows (e.g. "Send reminder after 7 days", "Run weekly billing checkpoint") require scheduled wake-ups.

## Problem
Should `EricksonLopez.Processes` include an integrated background cron/scheduler engine?

## Options
1. Implement a full-fledged scheduler engine (polling worker loops, cron expressions).
2. Prohibit scheduler implementation in core and express scheduling as intent.

## Decision
We adopt **Option 2: Prohibit scheduler implementation in core; express scheduling strictly as intent**.

The process library models what must happen upon receiving a scheduled trigger (e.g. `InvoiceExpiredEvent`), but delegates the physical timer mechanism to infrastructure.

## Rationale
- Scheduling is an infrastructure concern with many enterprise-grade solutions (Hangfire, Quartz.NET, Cloud Tasks, AWS EventBridge).
- Prevents bloat, concurrency bugs, and database lock contention inside the process library.

## Consequences
- Clean boundary between stateful logic and background polling/scheduling.

## Rejected Alternatives
- Reinventing cron parsers or background polling workers in `Processes`.
