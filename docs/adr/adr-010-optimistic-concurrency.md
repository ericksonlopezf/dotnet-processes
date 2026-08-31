# ADR-010: Optimistic Concurrency Control and Compare-And-Swap (CAS)

## Context
In high-throughput distributed systems, multiple messages for the same process instance may arrive simultaneously across multiple worker threads or Kubernetes pods.

## Problem
How should concurrent modifications to the same process instance be handled safely and efficiently?

## Options
1. Distributed Locking (`RedLock`, Consul, Zookeeper).
2. Database row-level pessimistic locking (`SELECT ... FOR UPDATE`).
3. Optimistic Concurrency Control with integer `Revision` / ETag using Compare-And-Swap (CAS).

## Decision
We adopt **Option 3: Optimistic Concurrency Control with `Revision`**.

Each `ProcessInstance<TState>` contains a monotonically increasing `Revision(long Value)`. When saving, the store executes:
```sql
UPDATE processes 
SET state = @newState, revision = @newRevision, updated_at = @now
WHERE process_id = @id AND revision = @expectedRevision
```
If 0 rows are affected, `SaveAsync` returns `ProcessSaveResult.ConcurrencyConflict`. The coordinator can then reload the latest state and retry the transition, or raise a conflict.

## Rationale
- Zero infrastructure lock dependencies.
- Scales horizontally without distributed lock bottlenecks or deadlock hazards.

## Consequences
- Requires idempotent state transitions when reloaded on concurrency conflicts.

## Rejected Alternatives
- Requiring distributed locks in core (adds massive operational complexity and latency).
