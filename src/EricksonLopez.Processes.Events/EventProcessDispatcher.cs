// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Events.Contracts;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Events;

/// <summary>
/// Dispatches process manager side-effect intents through an event publisher.
/// </summary>
public sealed class EventProcessDispatcher : IEventProcessDispatcher
{
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventProcessDispatcher"/> class with the specified event publisher.
    /// </summary>
    /// <param name="eventPublisher">The event publisher instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="eventPublisher"/> is <see langword="null"/></exception>
    public EventProcessDispatcher(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <inheritdoc/>
    public async ValueTask DispatchEffectsAsync(
        IEnumerable<ProcessEffect> effects,
        ProcessId processId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);

        foreach (var effect in effects)
        {
            await DispatchEffectAsync(effect, processId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public ValueTask DispatchEffectAsync(
        ProcessEffect effect,
        ProcessId processId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        switch (effect)
        {
            case ProcessEffect.Event evt when evt.EventPayload is IEvent eventInstance:
                return _eventPublisher.PublishAsync(eventInstance, cancellationToken);

            case ProcessEffect.Command cmd when cmd.CommandPayload is IEvent eventInstance:
                return _eventPublisher.PublishAsync(eventInstance, cancellationToken);

            case ProcessEffect.Compensation comp when comp.Action.Payload is IEvent eventInstance:
                return _eventPublisher.PublishAsync(eventInstance, cancellationToken);

            case ProcessEffect.ScheduleTimeout timeout when timeout.TimeoutTrigger is IEvent eventInstance:
                return _eventPublisher.PublishAsync(eventInstance, cancellationToken);

            default:
                return ValueTask.CompletedTask;
        }
    }
}
