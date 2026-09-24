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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventProcessDispatcher"/> class with the specified event publisher.
    /// </summary>
    /// <param name="eventPublisher">The event publisher instance.</param>
    /// <param name="timeProvider">An optional <see cref="TimeProvider"/> for controlling delay scheduling.</param>
    /// <exception cref="ArgumentNullException"><paramref name="eventPublisher"/> is <see langword="null"/></exception>
    public EventProcessDispatcher(IEventPublisher eventPublisher, TimeProvider? timeProvider = null)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _timeProvider = timeProvider ?? TimeProvider.System;
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
                if (timeout.Delay > TimeSpan.Zero)
                {
                    return ScheduleDelayedEventAsync(eventInstance, timeout.Delay, cancellationToken);
                }
                return _eventPublisher.PublishAsync(eventInstance, cancellationToken);

            default:
                return ValueTask.CompletedTask;
        }
    }

    private async ValueTask ScheduleDelayedEventAsync(IEvent eventInstance, TimeSpan delay, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
        await _eventPublisher.PublishAsync(eventInstance, cancellationToken).ConfigureAwait(false);
    }
}
