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
