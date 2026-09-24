# Showcase — Level 01: Quick Start (Minimal Process)

**Level 01** demonstrates the minimal configuration required to define and execute a complete functional process with in-memory persistence.

---

## Covered Components

- `IProcessState`: Immutable state contract.
- `IProcess<TState>`: Process definition contract.
- `IProcessHandler<TState, TEvent>`: Event transition logic.
- `IProcessCorrelation<TEvent>`: Inbound message routing to the appropriate instance.
- `InMemoryProcessStore<TState>`: Thread-safe in-memory store.
- `ProcessCoordinator<TState>`: Core orchestration engine.

---

## Example Flow

```mermaid
sequenceDiagram
    participant Host
    participant Coordinator as ProcessCoordinator&lt;OrderState&gt;
    participant Store as InMemoryProcessStore&lt;OrderState&gt;
    participant Process as MinimalOrderProcess

    Host->>Coordinator: ExecuteAsync(OrderCreatedEvent)
    Coordinator->>Store: GetByIdAsync(ProcessId) -> null (new instance)
    Coordinator->>Process: HandleAsync(initialState, OrderCreatedEvent)
    Process-->>Coordinator: ProcessTransitionResult.Advance(Running)
    Coordinator->>Store: SaveAsync(ProcessInstance with Revision.Initial)
    Coordinator-->>Host: ProcessExecutionResult (IsSuccess=true, Status=Running)
```

---

## Reference Code

```csharp
public sealed record OrderState(string OrderId, decimal Amount, bool Paid) : IProcessState;

public sealed record OrderCreatedEvent(Guid OrderId, decimal Amount);

[ProcessDefinition("order.minimal", 1)]
public sealed class MinimalOrderProcess :
    IProcess<OrderState>,
    IProcessHandler<OrderState, OrderCreatedEvent>
{
    public ProcessType Type => ProcessType.FromString("order.minimal");
    public ProcessVersion Version => ProcessVersion.FromInt32(1);

    public ValueTask<ProcessTransitionResult<OrderState>> HandleAsync(
        OrderState state, OrderCreatedEvent eventMessage, ProcessContext context)
    {
        var newState = state with { OrderId = eventMessage.OrderId.ToString(), Amount = eventMessage.Amount };
        return ValueTask.FromResult(ProcessTransitionResult<OrderState>.Advance(newState, ProcessStatus.Running));
    }
}
```

---

## Showcase Execution

Executable code: `samples/EricksonLopez.Processes.Showcase/Level01_QuickStart/QuickStartDemo.cs`
```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=1
```
