# FAQ — EricksonLopez.Processes

Frequently asked questions about `EricksonLopez.Processes`.

---

## General

### What is a Process Manager vs. a Saga?

**Process Manager** (`IProcess<TState>`): Coordinates a multi-step business workflow by reacting to events and emitting commands/effects. Does not define compensating actions — steps are not rollback-able.

**Saga** (`ISaga<TState>` + `ICompensationHandler<TState>`): A Process Manager with an explicit compensation model. Defines how to reverse each executed step in reverse (LIFO) order when a failure occurs.

In this library, both share the same execution engine (`ProcessCoordinator<TState>`). The distinction is whether `ICompensationHandler<TState>` is implemented.

---

### How is this different from a workflow engine like Temporal or Orleans Grains?

| Aspect | EricksonLopez.Processes | Temporal / Orleans |
| :--- | :--- | :--- |
| **Memory model** | State-only persistence (no thread held) | Thread/actor held in memory |
| **Replay model** | Deterministic state-machine transitions | Workflow replay or actor rehydration |
| **AOT/Trimming** | ✅ First-class | ❌ Reflection-heavy |
| **Dependencies** | Zero for Abstractions | Runtime agent required |
| **Scale-out** | Any scale (stateless coordinator) | Actor placement overhead |
| **Programming model** | Event-driven handlers | Async/await coroutines or message passing |

Use this library when you want **explicit stateful workflows** embedded in your .NET service without a workflow engine dependency.

---

### Is this a BPMN engine?

No. BPMN defines workflows visually with a diagram-first approach and requires an engine to interpret the diagram at runtime. This library defines workflows as **typed C# code** — the state machine is the code. There is no BPMN parser or runtime interpreter.

---

### Can I run this without a database?

Yes — for testing or local development, use `InMemoryProcessStore<TState>`. For production, choose from the 6 database providers: PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle.

---

## Concurrency

### Why does the library use optimistic concurrency instead of database locks?

Distributed locks are fragile and introduce cluster-wide bottlenecks. Optimistic Concurrency Control (OCC) with Compare-And-Swap (CAS) on `Revision` is:

- **Lock-free**: No global locking at the database level.
- **Scalable**: Multiple coordinators can race — only one wins per revision.
- **Resilient**: Retry logic is deterministic and bounded.

See [ADR-010](../adr/ADR-010-optimistic-concurrency.md) for the full decision rationale.

---

### What happens if `MaxConcurrencyRetries` is exhausted?

`ConcurrencyConflictException` is thrown. The caller is responsible for deciding whether to requeue the event, log, alert, or dead-letter it. This is intentional — the library does not silently discard events.

---

## Effects

### Why doesn't the library dispatch effects automatically?

Effects are **intents** (commands, events, outbox messages). Dispatching them is the responsibility of the host application, because:

1. The library is agnostic of the message broker, mediator, or outbox implementation.
2. Effects should only be dispatched **after** the state is successfully persisted — not before. The host controls this ordering.
3. Combining `ProcessEffect.OutboxMessage` with a transactional outbox pattern avoids dual-write issues.

The integration packages (`Events`, `Mediator`, `Outbox`) provide ready-to-use dispatchers if you use the corresponding ecosystem packages.

---

### Can one handler emit multiple effects?

Yes. `ProcessTransitionResult.Advance(state, status, effects: [...])` accepts `IReadOnlyList<ProcessEffect>`. Multiple effects of different types can be emitted in a single transition.

---

## State & Serialization

### Why must state implement `IProcessState`?

`IProcessState` is a marker interface. It provides a compile-time constraint ensuring only valid state types are passed to generic coordinators, stores, and migration pipelines. It has no runtime cost.

### What serialization format is used?

By default, you must provide an `IProcessStateSerializer` implementation. The `EricksonLopez.Processes.SystemTextJson` package provides `SystemTextJsonProcessStateSerializer` — JSON serialization using `System.Text.Json` with full Native AOT support via `JsonSerializerContext`.

---

## Source Generator

### Do I need the Source Generator?

No, it is optional. `AddGeneratedProcesses()` is a convenience extension. You can manually register coordinators and handlers via `AddProcessCoordinator<TState>()` and DI. The generator eliminates registration boilerplate and is the recommended approach for AOT-safe builds.

---

## Testing

### Can I test sagas without a real database?

Yes. Use `InMemoryProcessStore<TState>` — it is thread-safe, uses atomic `ConcurrentDictionary` CAS, and fully simulates the OCC behavior without any database dependency.

### How do I test compensation flows?

Inject the triggering "failure" event and verify:
1. The instance transitions to `ProcessStatus.Compensating`.
2. The `Effects` list contains compensation command effects.
3. After subsequent `CompensateAsync` calls, the status transitions to `ProcessStatus.Compensated`.

---

## Packages

### Why are there separate packages for each storage engine?

Each storage package brings its own ADO.NET driver as a dependency (`Npgsql`, `MySqlConnector`, etc.). Separating them avoids forcing all users to take all driver dependencies. Reference only the storage package for your chosen database.

### What is the target framework?

Most runtime packages target `net10.0`. Generator and Analyzer packages target `netstandard2.0` to be compatible with Roslyn build hosts. See [ADR-025](../adr/ADR-025-target-frameworks.md).

### Is `EricksonLopez.Processes.Abstractions` stable?

Yes. `Abstractions` is the zero-dependency contract package. It is designed to be maximally stable. Breaking changes to `Abstractions` increment the major version across the entire ecosystem.
