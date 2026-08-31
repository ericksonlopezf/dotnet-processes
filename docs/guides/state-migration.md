# Schema Evolution & State Migration

As business requirements evolve, the schema of a process state often changes. In `EricksonLopez.Processes`, state migrations are explicit and strongly typed via `IProcessStateMigrator<TFrom, TTo>`.

## Defining a State Migrator

```csharp
using EricksonLopez.Processes.Abstractions;

public sealed record OrderStateV1(string OrderId, decimal Amount) : IProcessState;
public sealed record OrderStateV2(string OrderId, decimal Amount, string Currency) : IProcessState;

public sealed class OrderStateV1ToV2Migrator : IProcessStateMigrator<OrderStateV1, OrderStateV2>
{
    public ProcessVersion FromVersion => ProcessVersion.From(1);
    public ProcessVersion ToVersion => ProcessVersion.From(2);

    public OrderStateV2 Migrate(OrderStateV1 sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);

        return new OrderStateV2(
            OrderId: sourceState.OrderId,
            Amount: sourceState.Amount,
            Currency: "USD");
    }
}
```

## Applying Migrations

When hydrating instances from older versions:

```csharp
var v1Instance = await store.GetByIdAsync(processId);
var migrator = new OrderStateV1ToV2Migrator();

var v2State = migrator.Migrate(v1Instance.State);
var v2Instance = new ProcessInstance<OrderStateV2>(
    id: v1Instance.Id,
    type: v1Instance.Type,
    version: migrator.ToVersion,
    status: v1Instance.Status,
    revision: v1Instance.Revision.Next(),
    correlationId: v1Instance.CorrelationId,
    createdAt: v1Instance.CreatedAt,
    updatedAt: DateTimeOffset.UtcNow,
    completedAt: v1Instance.CompletedAt,
    state: v2State);

await store.SaveAsync(v2Instance);
```
