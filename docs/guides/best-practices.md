# Best Practices — EricksonLopez.Processes

Architectural best practices, design rules, and anti-patterns for `EricksonLopez.Processes`.

---

## State Design

### Keep State Immutable

Always define state as immutable `sealed record`. Use `with` expressions for updates.

```csharp
// ✅ Correct — immutable record
public sealed record OrderState(
    string OrderId,
    bool PaymentCharged,
    bool InventoryReserved) : IProcessState;

// ✅ Correct — non-destructive mutation via 'with'
var updated = state with { PaymentCharged = true };

// ❌ Wrong — mutable class
public class OrderState : IProcessState
{
    public bool PaymentCharged { get; set; }  // Never
}
```

### Keep State Small and Serializable

State is serialized to JSON on every save. Avoid embedding large collections or graph structures.

```csharp
// ✅ Correct — reference by ID
public sealed record ShipmentState(string ShipmentId, string WarehouseId) : IProcessState;

// ❌ Wrong — embedding full object graphs
public sealed record ShipmentState(OrderDto FullOrder, List<ProductDto> Items) : IProcessState;
```

---

## Transition Design

### Keep Transitions Pure

Handler methods (`HandleAsync`, `CompensateAsync`) must be pure functions: no I/O, no DB calls, no HTTP. They produce `ProcessTransitionResult<TState>` containing **effects** (intents) — the host is responsible for executing effects.

```csharp
// ✅ Correct — pure handler, emits effect intent
public ValueTask<ProcessTransitionResult<MyState>> HandleAsync(
    MyState state, MyEvent e, ProcessContext ctx)
{
    var updated = state with { Processed = true };
    var effect = new ProcessEffect.Command(new DoSomethingCommand(e.Id));
    return ValueTask.FromResult(
        ProcessTransitionResult<MyState>.Advance(updated, ProcessStatus.Running, effects: [effect]));
}

// ❌ Wrong — I/O inside handler
public async ValueTask<ProcessTransitionResult<MyState>> HandleAsync(
    MyState state, MyEvent e, ProcessContext ctx)
{
    await _httpClient.PostAsync(...);  // Never — breaks OCC retry idempotency
    return ...;
}
```

### Effects Must be Idempotent or Deduplicated

Because the OCC retry loop may invoke `HandleAsync` multiple times before a successful save, the same effects may be emitted multiple times. Effect consumers (Mediator, Outbox, EventBus) must be idempotent, or the Outbox pattern must be used.

---

## Correlation Design

### Use Stable, Deterministic Correlation IDs

```csharp
// ✅ Correct — deterministic from business keys
public CorrelationId ExtractCorrelationId(OrderShippedEvent e) =>
    CompositeCorrelationKey.From(e.OrderId, e.RegionId).ToCorrelationId();

// ❌ Wrong — non-deterministic (new ID on each retry)
public CorrelationId ExtractCorrelationId(OrderShippedEvent e) =>
    CorrelationId.From(Guid.NewGuid());
```

---

## Saga Design

### Record Compensation Steps Immediately After Each Forward Action

```csharp
// ✅ Correct — record compensation immediately after the forward effect
return ProcessTransitionResult<OrderState>.Advance(
    updated,
    ProcessStatus.Running,
    effects: [new ProcessEffect.Command(new ChargePaymentCommand(e.OrderId, amount))],
    recordedCompensations: [new CompensationStep("ChargePayment", new { amount }, ctx.Now)]);
```

### Always Handle Every `StepName` in `CompensateAsync`

Use an exhaustive switch with a `default` that fails explicitly:

```csharp
public ValueTask<ProcessTransitionResult<MyState>> CompensateAsync(
    MyState state, CompensationAction action, ProcessContext ctx) =>
    action.StepName switch
    {
        "ChargePayment" => ValueTask.FromResult(
            ProcessTransitionResult<MyState>.Advance(
                state with { PaymentCharged = false },
                ProcessStatus.Compensating,
                effects: [new ProcessEffect.Command(new RefundPaymentCommand(...))])),
        _ => ValueTask.FromResult(
            ProcessTransitionResult<MyState>.Fail(state, $"Unknown compensation step: {action.StepName}"))
    };
```

---

## DI Registration

### Use `AddGeneratedProcesses()` — Never Manually Register Handlers

The Source Generator (`EricksonLopez.Processes.Generator`) emits a compile-time registration extension. Avoid manual handler registration in `IServiceCollection` — it defeats the AOT safety.

```csharp
// ✅ Correct
services.AddGeneratedProcesses();

// ❌ Wrong — manual registration bypasses generator safety
services.AddTransient<IProcessHandler<OrderState, OrderCreatedEvent>, OrderSaga>();
```

---

## Testing

### Isolate Store Per Test

Each test should use its own `InMemoryProcessStore<TState>` to avoid cross-test state leakage. See the [Cookbook Recipe 7](cookbook.md#recipe-7-in-memory-testing-with-inmemoryprocessstore).

### Test State Migrations Independently

Migration logic should be unit-tested with known input/output fixtures before deploying to production. See the [Migration Guide](migration-guide.md#testing-migrations).

### Use `FaultInjectingProcessStore` for Resilience Tests

```csharp
var store = new FaultInjectingProcessStore<MyState>(
    innerStore: new InMemoryProcessStore<MyState>(),
    injectSaveFailureAfterNthCall: 2);

// Coordinator will retry — test that it recovers correctly
```

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why | Fix |
| :--- | :--- | :--- |
| Mutable state classes | Breaks immutability invariant, causes OCC issues | Use `sealed record` |
| I/O in handlers | Breaks OCC idempotency and retry semantics | Emit effects; handle in host |
| Non-deterministic correlation IDs | Creates orphaned instances | Use `CompositeCorrelationKey` |
| Shared store in parallel tests | Causes flaky OCC conflicts | New store per test |
| Manual handler registration | Bypasses Source Generator AOT safety | Use `AddGeneratedProcesses()` |
| Catching `ConcurrencyConflictException` | Masks configuration errors | Let coordinator handle OCC |
| Business logic in migrators | Violates pure migration contract | Keep migrators pure transformations |
| Embedding full domain objects in state | Makes serialization expensive | Reference by ID |
