# Building Distributed Sagas

A Saga coordinates multi-step business transactions across distributed microservices by executing forward actions and, upon failure, rolling back all previously completed steps in reverse order (LIFO).

## Defining a Saga

Implement `ISaga<TState>` and `ICompensationHandler<TState>`:

```csharp
[SagaDefinition("flight.booking", 1)]
public sealed class FlightBookingSaga :
    ISaga<BookingState>,
    ICompensationHandler<BookingState>,
    IProcessHandler<BookingState, ReserveFlightEvent>,
    IProcessHandler<BookingState, ReserveHotelFailedEvent>
{
    public ProcessType Type => ProcessType.From("flight.booking");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<BookingState>> HandleAsync(
        BookingState state,
        ReserveFlightEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { FlightBooked = true };
        var compensation = new CompensationStep("BookFlight", new { eventMessage.FlightId }, context.Now);

        return ValueTask.FromResult(ProcessTransitionResult<BookingState>.Advance(
            updated,
            ProcessStatus.Running,
            effects: [new ProcessEffect.Command(new BookHotelCommand(state.TripId))],
            recordedCompensations: [compensation]));
    }

    public ValueTask<ProcessTransitionResult<BookingState>> HandleAsync(
        BookingState state,
        ReserveHotelFailedEvent eventMessage,
        ProcessContext context)
    {
        // Trigger compensation rollback
        return ValueTask.FromResult(ProcessTransitionResult<BookingState>.Compensate(
            state,
            compensationActions: [new CompensationAction("BookFlight", new { state.FlightId })]));
    }

    public ValueTask<ProcessTransitionResult<BookingState>> CompensateAsync(
        BookingState state,
        CompensationAction action,
        ProcessContext context)
    {
        var updated = state with { FlightBooked = false };
        return ValueTask.FromResult(ProcessTransitionResult<BookingState>.Advance(
            updated,
            ProcessStatus.Compensating,
            effects: [new ProcessEffect.Command(new CancelFlightCommand(state.FlightId))]));
    }
}
```

## External Idempotency Requirements

While `EricksonLopez.Processes` guarantees step-by-step durability during Saga compensations (crash-tolerance), the system relies on **At-Least-Once delivery semantics** for dispatched effects.

If a host crashes *after* a `ProcessEffect` (such as a compensation command) is successfully dispatched to the message broker but *before* the saga state (`_store.SaveAsync`) is updated to record that the compensation step was completed, the orchestrator will re-execute the compensation step upon recovery.

Therefore, **all downstream handlers processing `ProcessEffect` messages MUST be fully idempotent**.

### Best Practices for Effect Handlers:
1. **Idempotency Keys**: Use the `ProcessId` and `CorrelationId` provided in the `ProcessEffect` as idempotency keys in your downstream service.
2. **Database Constraints**: Utilize unique constraints in your domain database to gracefully handle duplicate compensation commands (e.g., `CancelFlightCommand`).
3. **Outbox Pattern**: When possible, dispatch `ProcessEffect` instances using a Transactional Outbox that shares a database transaction with the saga's state store to achieve exactly-once processing within the same transactional boundary.
