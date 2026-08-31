# Architectural Decision Record: REJECT-010
## Rejection of Saga Orchestration inside Mediator IPipelineBehavior

### Status
**REJECTED (Permanent Directorial Invariant)**

### Context
Proposals suggested implementing long-running business processes and compensating transactions as Mediator pipeline behaviors.

### Decision
Permanently rejected. Sagas require persistent process states, versioned revisions, concurrency locking, and outbox event publishing. These concerns are exclusively owned and executed by `EricksonLopez.Processes` and its dedicated storage providers.

### Consequences
- Clear capability boundaries: Mediator = Request/Response dispatch; Processes = Stateful Saga / Process Manager.
- High resilience and state recoverability across process crashes and restarts.
