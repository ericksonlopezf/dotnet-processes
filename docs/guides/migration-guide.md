# State Schema Evolution & Migration Guide — EricksonLopez.Processes

Zero-downtime schema evolution for long-running process manager and saga states using `ProcessStateMigrationPipeline` and `IProcessStateMigrator<TFrom, TTo>`.

---

## 1. The State Schema Evolution Challenge

Process and saga instances often remain active for days, weeks, or months. While an instance is in-flight or suspended waiting for a business milestone or external deadline, domain models evolve across application releases (e.g. from Version 1 to Version 3).

To avoid complex batch database migration scripts that lock tables and degrade throughput, `EricksonLopez.Processes` implements **lazy on-demand state migration**:
1. The relational storage provider reads the persisted record with its stored `Version` (e.g., `1`).
2. The `ProcessCoordinator` detects that the persisted instance version is older than the target process version.
3. If versions differ, it executes the sequential, synchronous migration pipeline: `V1 → V2 → V3`.
4. The handler receives the fully migrated state in schema `V3`.
5. When saving the transition result, the record is atomically persisted with `Version = 3` and the incremented `Revision` token.

---

## 2. Step 1: Define Immutable State Schemas

Retain historical schema contracts as immutable records (`record`) until all historical in-flight instances have completed:

```csharp
using System;
using EricksonLopez.Processes.Abstractions;

// Original Schema (V1)
public sealed record OrderStateV1(
    string OrderId,
    decimal TotalAmount) : IProcessState;

// Intermediate Schema (V2) — Adds multi-currency support
public sealed record OrderStateV2(
    string OrderId,
    decimal TotalAmount,
    string Currency) : IProcessState;

// Current Target Schema (V3) — Adds itemized tax and audit timestamp
public sealed record OrderStateV3(
    string OrderId,
    decimal TotalAmount,
    string Currency,
    decimal TaxAmount,
    DateTimeOffset MigratedAt) : IProcessState;
```

---

## 3. Step 2: Implement Synchronous Migrators

Each migration step implements `IProcessStateMigrator<TFrom, TTo>` as a pure, deterministic transformation function:

```csharp
using System;
using EricksonLopez.Processes.Abstractions;

// Migrator from V1 to V2
public sealed class OrderV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public ProcessVersion FromVersion => ProcessVersion.From(1);
    public ProcessVersion ToVersion => ProcessVersion.From(2);

    public OrderStateV2 Migrate(OrderStateV1 sourceState) =>
        new OrderStateV2(
            sourceState.OrderId,
            sourceState.TotalAmount,
            Currency: "USD"); // Safe fallback default for pre-existing instances
}

// Migrator from V2 to V3
public sealed class OrderV2ToV3Migrator : IProcessStateMigrator<OrderStateV2, OrderStateV3>
{
    public ProcessVersion FromVersion => ProcessVersion.From(2);
    public ProcessVersion ToVersion => ProcessVersion.From(3);

    public OrderStateV3 Migrate(OrderStateV2 sourceState) =>
        new OrderStateV3(
            sourceState.OrderId,
            sourceState.TotalAmount,
            sourceState.Currency,
            TaxAmount: sourceState.TotalAmount * 0.18m,
            MigratedAt: DateTimeOffset.UtcNow);
}
```

---

## 4. Step 3: Build the Migration Pipeline

Use the fluent `ProcessStateMigrationPipeline.Create` factory to chain the transformations:

```csharp
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

public static class OrderMigrationPipelineConfig
{
    public static IProcessStateMigrator<OrderStateV1, OrderStateV3> BuildPipeline()
    {
        return ProcessStateMigrationPipeline
            .Create<OrderStateV1>(ProcessVersion.From(1))
            .AddStep(new OrderV1ToV2Migrator())
            .AddStep(new OrderV2ToV3Migrator())
            .Build();
    }
}
```

Transformations can also be registered inline using lambda functions:

```csharp
var pipeline = ProcessStateMigrationPipeline
    .Create<OrderStateV1>(ProcessVersion.From(1))
    .AddStep(new OrderV1ToV2Migrator())
    .AddStep(ProcessVersion.From(3), (OrderStateV2 v2) =>
        new OrderStateV3(v2.OrderId, v2.TotalAmount, v2.Currency, v2.TotalAmount * 0.18m, DateTimeOffset.UtcNow))
    .Build();
```

---

## 5. Step 4: Update the Process Definition

Increment the version in both the `[ProcessDefinition]` attribute and the `Version` property:

```csharp
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;

[ProcessDefinition("order.fulfillment", 3)] // Target Version 3
public sealed class OrderFulfillmentProcess :
    IProcess<OrderStateV3>,
    IProcessHandler<OrderStateV3, PaymentConfirmedEvent>
{
    public ProcessType Type => ProcessType.From("order.fulfillment");
    public ProcessVersion Version => ProcessVersion.From(3);

    public ValueTask<ProcessTransitionResult<OrderStateV3>> HandleAsync(
        OrderStateV3 state,
        PaymentConfirmedEvent eventMessage,
        ProcessContext context)
    {
        // Executes directly against the migrated V3 schema
        return ValueTask.FromResult(ProcessTransitionResult<OrderStateV3>.Complete(state));
    }
}
```

---

## 6. Coexistence During Rolling Deployments

During blue/green or rolling cluster deployments, multiple application instances running different code versions coexist:

1. **Read Compatibility**: Older replicas continue reading their supported schema version from the database.
2. **Write Safety (CAS)**: The monotonic `Revision` token ensures that an older replica cannot inadvertently overwrite a state that was already migrated to a newer version by an updated replica.

---

## 7. Best Practices

- ✅ **Pure Synchronous Functions**: The `Migrate(TFrom)` method must be strictly synchronous and free of side-effects or network I/O (no HTTP or database queries).
- ✅ **Deterministic Defaults**: Provide semantically safe, predictable fallback values for newly introduced required properties.
- ✅ **Monotonic Version Numbering**: Process versions must be strictly monotonic (`1, 2, 3...`). Never decrement version numbers.
- ✅ **Migration Unit Tests**: Write automated unit tests validating that historical JSON payloads from V1 migrate deterministically into the expected V3 objects.
