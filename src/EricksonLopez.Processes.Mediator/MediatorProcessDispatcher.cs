// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Mediator;

using EricksonLopez.Mediator;
using EricksonLopez.Processes.Abstractions;

/// <summary>
/// Dispatches process manager side-effect intents directly through an in-memory mediator.
/// </summary>
public sealed class MediatorProcessDispatcher : IMediatorProcessDispatcher
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorProcessDispatcher"/> class with the specified mediator.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mediator"/> is <see langword="null"/></exception>
    public MediatorProcessDispatcher(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets or sets an optional callback invoked when a <see cref="ProcessEffect"/> payload cannot be dispatched.
    /// </summary>
    /// <remarks>
    /// Use this callback to log, trace, or handle payloads that are not recognized by the dispatcher.
    /// </remarks>
    public Action<ProcessId, ProcessEffect, object?>? OnUnrecognizedPayload { get; set; }

    /// <inheritdoc/>
    public async ValueTask DispatchEffectsAsync(
        IEnumerable<ProcessEffect> effects,
        ProcessId processId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);

        foreach (var effect in effects)
        {
            await DispatchEffectAsync(effect, processId, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DispatchEffectAsync(
        ProcessEffect effect,
        ProcessId processId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        switch (effect)
        {
            case ProcessEffect.Command command:
                {
                    await DispatchPayloadAsync(command.CommandPayload, processId, effect, cancellationToken);
                    break;
                }

            case ProcessEffect.Event evt:
                {
                    if (evt.EventPayload is INotification notification)
                    {
                        await _mediator.Publish(notification, cancellationToken);
                    }
                    else
                    {
                        OnUnrecognizedPayload?.Invoke(processId, effect, evt.EventPayload);
                    }
                    break;
                }

            case ProcessEffect.Compensation compensation:
                {
                    await DispatchPayloadAsync(compensation.Action.Payload, processId, effect, cancellationToken);
                    break;
                }

            case ProcessEffect.ScheduleTimeout timeout:
                {
                    if (timeout.TimeoutTrigger is INotification notification)
                    {
                        await _mediator.Publish(notification, cancellationToken);
                    }
                    else
                    {
                        OnUnrecognizedPayload?.Invoke(processId, effect, timeout.TimeoutTrigger);
                    }
                    break;
                }
        }
    }

    private async ValueTask DispatchPayloadAsync(
        object? payload,
        ProcessId processId,
        ProcessEffect effect,
        CancellationToken cancellationToken)
    {
        if (payload is INotification notification)
        {
            await _mediator.Publish(notification, cancellationToken);
        }
        else if (payload is ICommand<bool> cmdBool)
        {
            await _mediator.Send(cmdBool, cancellationToken);
        }
        else
        {
            OnUnrecognizedPayload?.Invoke(processId, effect, payload);
        }
    }
}




