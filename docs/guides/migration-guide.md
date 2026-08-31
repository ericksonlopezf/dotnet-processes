# Migration Guide — EricksonLopez.Processes

Zero-downtime process state schema evolution using `ProcessStateMigrationPipeline<TState>` and `IProcessStateMigrator<TFrom, TTo>`.

---

## Overview

Process instances are long-lived. They may be created under state schema version `1` and still be active when the codebase has advanced to version `3`. The migration pipeline handles this transparently during `ProcessCoordinator.ExecuteAsync`:

1. The coordinator loads the `ProcessStateRecord` from storage.
2. It reads the stored `Version`.
3. If `Version < CurrentVersion`, it runs the registered migration chain: `v1 → v2 → v3`.
4. The migrated state is handed to the handler.
5. The handler's output is saved with `Version = CurrentVersion`.

---

## Step 1: Define the New State

Add the new state record. Keep the old record until all live instances have migrated.

```csharp
// Old (v1) — keep until all instances are migrated
public sealed record PaymentStateV1(
    string PaymentId,
    decimal Amount,
    string Status) : IProcessState;

// New (v2) — adds CurrencyCode
public sealed record PaymentStateV2(
    string PaymentId,
    decimal Amount,
    string Status,
    string CurrencyCode) : IProcessState;
```

---

## Step 2: Implement the Migrator

```csharp
using EricksonLopez.Processes.Abstractions;

public sealed class PaymentStateV1ToV2 : IProcessStateMigrator<PaymentStateV1, PaymentStateV2>
{
    public Task<PaymentStateV2> MigrateAsync(PaymentStateV1 old, CancellationToken ct) =>
        Task.FromResult(new PaymentStateV2(
            old.PaymentId,
            old.Amount,
            old.Status,
            CurrencyCode: "USD"   // Default for all pre-existing instances
        ));
}
```

---

## Step 3: Register the Migration Step

```csharp
services.AddProcessStateMigrator<PaymentStateV1, PaymentStateV2, PaymentStateV1ToV2>();
```

---

## Step 4: Bump the Version on the Process Definition

```csharp
[SagaDefinition("payment.processing", version: 2)]   // Was: version: 1
public sealed class PaymentProcessingSaga :
    ISaga<PaymentStateV2>,
    // ... handlers
```

---

## Step 5: Deploy and Migrate

**No downtime required.** The migration is applied lazily on-demand when each instance is next triggered. Old instances stored with `Version=1` will be migrated to `Version=2` transparently on their next `ExecuteAsync` call.

---

## Multi-Step Migration Chain

For migrations spanning multiple versions (e.g., `v1 → v2 → v3`):

```csharp
// Register each step
services.AddProcessStateMigrator<OrderStateV1, OrderStateV2, OrderV1ToV2>();
services.AddProcessStateMigrator<OrderStateV2, OrderStateV3, OrderV2ToV3>();

// ProcessStateMigrationPipeline chains them automatically:
// v1 → v2 → v3
```

---

## Version Coexistence During Rolling Deployments

During a rolling deployment, both old and new process versions may run simultaneously. The library handles this safely:

- **Old instances** (stored as `v1`): migrated to `v2` on first access by new code.
- **Old pods** (running `v1` code): will fail to deserialize `v2` state (unknown fields). Use graceful rolling deployments.
- **Recommendation**: Always deploy in a single step without rollback risk. If rollback is needed, maintain schema backward-compatibility (additive-only changes).

See [ADR-027](../adr/ADR-027-version-coexistence.md) for the full coexistence strategy.

---

## Rules for Safe Migrations

| Rule | Description |
| :--- | :--- |
| **Additive only** | New fields should have defaults. Never remove or rename fields between minor versions. |
| **Idempotent** | Applying the same migration twice must produce the same result. |
| **No external I/O in migration** | Migrators must be pure functions (no DB calls, no HTTP). |
| **Test with both old and new state** | Unit-test migrators with real `v1` JSON fixtures before deploying. |

---

## Testing Migrations

```csharp
[Fact]
public async Task MigrateV1ToV2_ShouldDefaultCurrencyCode()
{
    var migrator = new PaymentStateV1ToV2();
    var oldState = new PaymentStateV1("PAY-001", 100m, "Running");

    var newState = await migrator.MigrateAsync(oldState, CancellationToken.None);

    Assert.Equal("USD", newState.CurrencyCode);
    Assert.Equal(oldState.PaymentId, newState.PaymentId);
    Assert.Equal(oldState.Amount, newState.Amount);
}
```
