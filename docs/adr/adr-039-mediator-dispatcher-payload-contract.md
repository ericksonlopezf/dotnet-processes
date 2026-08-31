# ADR-039: MediatorProcessDispatcher Explicit Payload Dispatch Contract

## Status
**Accepted**

---

## Context
`MediatorProcessDispatcher` dispatches process effects (`ProcessEffect`) across `IMediator` pipelines. To determine whether an untyped payload represents a command or an event notification, it previously performed pattern matching against `is INotification` and `is ICommand<bool>`.

Earlier reviews identified that unrecognized payloads were silently ignored without diagnostic feedback, reducing runtime visibility for developers.

---

## Decision
Enhance `MediatorProcessDispatcher`:

1. **Introduce `OnUnrecognizedPayload` Callback**: Provide an optional `Action<ProcessId, ProcessEffect, object?>?` property invoked when a payload matches neither `INotification` nor `ICommand<bool>`.
2. **Extract Private `DispatchPayloadAsync` Helper**: Consolidate repetitive dispatch logic between `Command` and `Compensation` effects.
3. **Document AOT Safety**: Clarify that static interface pattern matching against known compiled interfaces (`INotification`, `ICommand<bool>`) is 100% Native AOT-safe.

```csharp
private async ValueTask DispatchPayloadAsync(object? payload, ProcessId processId, ProcessEffect effect, CancellationToken ct)
{
    if (payload is INotification n)
    {
        await _mediator.Publish(n, ct);
    }
    else if (payload is ICommand<bool> c)
    {
        await _mediator.Send(c, ct);
    }
    else
    {
        OnUnrecognizedPayload?.Invoke(processId, effect, payload);
    }
}
```

---

## Consequences
- **Positive**:
  - Full observability for unrecognized payloads via configurable diagnostic callbacks.
  - DRY, maintainable dispatch implementation.
  - Zero breaking changes (`OnUnrecognizedPayload` defaults to `null`).
  - 100% Native AOT compliance.
- **Negative**:
  - Full static payload guarantees remain deferred to v3.0 (ADR-037).
