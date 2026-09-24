# Showcase — Level 00: Conceptual Architecture

**Level 00** establishes the architectural foundations, axioms, and design decisions that differentiate `EricksonLopez.Processes` from traditional workflow engines and state machine libraries.

---

## 1. What is the Library?

`EricksonLopez.Processes` is an ultra-lightweight engine for **Durable Processes and Distributed Sagas** for .NET 10 (C# 13). It is engineered with a strict **zero-reflection** approach, complete trimming safety (*trimming-safe*), and native **Native AOT** compatibility.

---

## 2. What Problem Does It Solve?

In distributed systems and microservice architectures, business workflows spanning multiple steps and services encounter recurring challenges:
1. **State loss across restarts**: In-memory state machines lose their state if the host container or node crashes.
2. **Race conditions and heavy locking**: Distributed locks (e.g. Redis Redlock) introduce latency, contention, and single points of failure.
3. **Framework bloat**: Heavy solutions impose massive dependencies, runtime reflection, and incompatibility with Native AOT.
4. **Unmanaged Dual-Write**: Saving database state and publishing events to a message broker in separate transactions leads to inconsistencies.

---

## 3. Principles and Design Axioms

- **Pure Transitions**: Transition handlers (`HandleAsync` and `CompensateAsync`) are pure functions: `(State, Event, Context) -> TransitionResult(NewState, Effects)`. They do not execute external I/O directly; they declare side-effect intentions as `ProcessEffect`.
- **State Persistence vs Runtime Locking**: No threads are held and no persistent actors remain in memory. The state is hydrated from storage, the transition is evaluated, and the state is persisted immediately with an atomic revision increment (OCC CAS).
- **Deterministic Optimistic Concurrency**: Every save checks `WHERE REVISION = @Revision`. If another node modified the row concurrently, the engine discards the in-flight state and retries with linear backoff.
- **Dual Model: Process Manager vs Saga**:
  - **Process Manager (`IProcess<TState>`)**: Orchestrates forward steps without automated compensation.
  - **Saga (`ISaga<TState>`)**: Supports automated rollback in reverse order (LIFO) upon failure via `ICompensationHandler<TState>`.

---

## 4. Comparison with Alternatives

| Feature | Stateless / Automatonymous | MassTransit / NServiceBus | EricksonLopez.Processes |
|---|---|---|---|
| **Concurrency Model** | In-memory / manual | Saga locks / correlation | **Atomic Versioned OCC CAS** |
| **Native AOT (.NET 10)** | ⚠️ Partial | ❌ Not supported | ✅ **100% Compatible and Verified** |
| **Memory Allocation** | High (Boxing/Reflection) | High | **Zero-Allocation Value Types** |
| **Storage Ports** | Non-existent / manual | Coupled to framework | **6 Official RDBMS Engines** |
| **Side Effects** | Impure direct invocation | Coupled to broker | **Decoupled Pure Intentions** |

---

## 5. Showcase Implementation

Inspect the executable code in `samples/EricksonLopez.Processes.Showcase/Level00_Conceptual/ConceptualOverview.cs`:
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=0
```
