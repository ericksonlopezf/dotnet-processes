# ADR-036: GetByCorrelationIdAsync as Default Interface Method in IProcessStore

## Status
**Accepted**

---

## Context
`IProcessStore<TState>` defines the persistence contract with three mandatory methods: `GetByIdAsync`, `SaveAsync`, and `ExistsAsync`. Direct competitors (MassTransit, NServiceBus, Wolverine) support querying workflow instances by business correlation key (`CorrelationId`) in addition to primary `ProcessId`.

This capability is essential when an incoming message needs to correlate to an in-flight workflow instance using an external business key (e.g. `orderId`, `customerId`) without knowing the internal `ProcessId` upfront.

---

## Problem
How to add `GetByCorrelationIdAsync` to `IProcessStore<TState>` without:
1. Forcing breaking changes on existing custom store implementers?
2. Compromising Native AOT compliance?
3. Introducing runtime casting complexity for simple stores?

---

## Options Considered
1. **Mandatory Abstract Interface Method**: Breaking change for all existing implementers.
2. **Separate Interface (`ICorrelationQueryableProcessStore<TState>`)**: Requires runtime `is` casting checks.
3. **Default Interface Method Returning `null`**: Adds `GetByCorrelationIdAsync` as a C# Default Interface Method returning `null` by default. Stores that index `CorrelationId` override it.

---

## Decision
We adopt **Option 3: Default Interface Method** in `IProcessStore<TState>`:

```csharp
ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(
    CorrelationId correlationId,
    CancellationToken cancellationToken = default)
    => ValueTask.FromResult<ProcessInstance<TState>?>(null);
```

---

## Rationale
- **Non-Breaking Evolution**: Existing implementers continue compiling without modification.
- **Explicit Opt-In**: Production database providers (PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite) index `correlation_id` and override the method.
- **Native AOT Compliance**: C# Default Interface Methods are resolved statically at compile time without runtime reflection.
- **Test Doubles Supported**: `InMemoryProcessStore` and `FaultInjectingProcessStore` fully override this method.

---

## Consequences
- **Positive**:
  - Seamless non-breaking evolution of the storage contract.
  - Production storage engines provide indexed correlation lookups.
- **Negative**:
  - Default `null` return value represents "unsupported / not found", documented explicitly in XML docs.
