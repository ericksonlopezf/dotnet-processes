# Showcase — Level 05: Processing & Concurrency (OCC / CAS / Composite Keys / Effects)

**Level 05** explores in depth the mechanics of Optimistic Concurrency Control (OCC CAS), deterministic composite key generation, and exhaustive inspection of all `ProcessEffect` variants.

---

## Covered Components

1. **Optimistic Concurrency Control Loop (OCC CAS)**:
   - Monotonic `Revision` token.
   - `FaultInjectingProcessStore<TState>` to simulate concurrent CAS collisions on demand.
   - Linear backoff formula: `delay = InitialBackoffDelay * attempt`.
   - Automatic retry loop up to `MaxConcurrencyRetries` before raising `ConcurrencyConflictException`.

2. **Composite Correlation Keys**:
   - `CompositeCorrelationKey.From(...)` with 2, 3, or 4 parts.
   - Deterministic `CorrelationId` generation via SHA-256 hashing (UUIDv5) for atomic partition routing in distributed clusters.

3. **Complete Catalog of `ProcessEffect` Variants**:
   - `ProcessEffect.Command`: `GetPayload<T>()`, `TryGetPayload<T>()`.
   - `ProcessEffect.Event`: `GetPayload<T>()`, `TryGetPayload<T>()`.
   - `ProcessEffect.ScheduleTimeout`: `GetTrigger<T>()`, `TryGetTrigger<T>()`.
   - `ProcessEffect.Compensation`: `Action.ExtractPayload<T>()`.

---

## OCC Conflict Resolution Diagram

```mermaid
sequenceDiagram
    participant Worker1 as Worker A (Wins)
    participant Worker2 as Worker B (Collides)
    participant Store as IProcessStore
    
    Worker1->>Store: SaveAsync (expects Rev 1 -> saves Rev 2)
    Store-->>Worker1: ProcessSaveResult.Success
    
    Worker2->>Store: SaveAsync (expects Rev 1 -> collision!)
    Store-->>Worker2: ProcessSaveResult.ConcurrencyConflict
    Note over Worker2: Linear backoff (InitialBackoffDelay * 1)
    Worker2->>Store: GetByIdAsync (reloads state with Rev 2)
    Worker2->>Store: SaveAsync (expects Rev 2 -> saves Rev 3)
    Store-->>Worker2: ProcessSaveResult.Success
```

---

## Showcase Execution

Executable code:
- `samples/EricksonLopez.Processes.Showcase/Level05_ProcessingAndConcurrency/Level05ConcurrencyRetryDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level05_ProcessingAndConcurrency/Level05CompositeKeyDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level05_ProcessingAndConcurrency/Level05AllEffectsDemo.cs`

```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=5
```
