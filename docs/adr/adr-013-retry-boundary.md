# ADR-013: Retry Strategy Boundary

## Context
Errors in distributed workflows fall into two distinct categories:
1. **Technical / Transient Failures**: Network glitches, temporary database timeouts, broker partition rebalancing.
2. **Business / Domain Failures**: Insufficient credit, item out of stock, KYC identity verification rejected.

## Problem
Where should retry policies and retry loops be implemented?

## Options
1. Embed Polly / resilience pipeline engines inside `EricksonLopez.Processes` core.
2. Separate technical retries (delegated to message transport/Polly/infrastructure) from domain retries (modeled explicitly as state transitions with attempt counters).

## Decision
We adopt **Option 2: Boundary separation for retries**.

- **Technical Retries**: Handled at the host / transport / persistence level before/around the `ProcessCoordinator`.
- **Domain Retries**: Modeled explicitly in the process state (e.g. `PaymentAttemptCount: state.PaymentAttemptCount + 1`), emitting retry commands or transitioning to `Failed` when maximum domain attempts are exceeded.

## Rationale
- Prevents bloat and redundant retry mechanisms in the core.
- Eliminates dependency on Polly or third-party resilience libraries in the abstractions package.

## Consequences
- Clean, deterministic state transitions without hidden background timers.

## Rejected Alternatives
- Embedding Polly policies inside core `IProcess` definitions.
