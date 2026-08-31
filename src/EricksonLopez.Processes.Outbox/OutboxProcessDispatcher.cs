// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Outbox;

using EricksonLopez.Outbox;
using EricksonLopez.Outbox.Persistence;
using EricksonLopez.Processes.Abstractions;

/// <summary>
/// Dispatches process manager side-effect intents reliably via an outbox persistence mechanism.
/// </summary>
public sealed class OutboxProcessDispatcher : IProcessOutboxDispatcher
{
    private readonly IOutbox _outbox;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessDispatcher"/> class with the specified outbox and time provider.
    /// </summary>
    /// <param name="outbox">The outbox instance.</param>
    /// <param name="timeProvider">The optional time provider for deterministic timestamp generation, or <see langword="null"/> to use <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outbox"/> is <see langword="null"/></exception>
    public OutboxProcessDispatcher(IOutbox outbox, TimeProvider? timeProvider = null)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask DispatchEffectsAsync(
        IEnumerable<ProcessEffect> effects,
        ProcessId processId,
        IOutboxTransactionContext? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effects);

        foreach (var effect in effects)
        {
            await DispatchEffectAsync(effect, processId, transaction, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DispatchEffectAsync(
        ProcessEffect effect,
        ProcessId processId,
        IOutboxTransactionContext? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (transaction is null)
        {
            return;
        }

        string correlationId = processId.Value.ToString();

        switch (effect)
        {
            case ProcessEffect.Command command:
                {
                    var metadata = new OutboxMessageMetadata(
                        correlationId: correlationId,
                        causationId: correlationId,
                        messageType: command.CommandType);

                    await _outbox.StoreAsync(
                        command.CommandPayload,
                        transaction,
                        metadata,
                        null,
                        cancellationToken);
                    break;
                }

            case ProcessEffect.Event evt:
                {
                    var metadata = new OutboxMessageMetadata(
                        correlationId: correlationId,
                        causationId: correlationId,
                        messageType: evt.EventType);

                    await _outbox.StoreAsync(
                        evt.EventPayload,
                        transaction,
                        metadata,
                        null,
                        cancellationToken);
                    break;
                }

            case ProcessEffect.Compensation compensation:
                {
                    var metadata = new OutboxMessageMetadata(
                        correlationId: correlationId,
                        causationId: correlationId,
                        messageType: compensation.Action.StepName);

                    await _outbox.StoreAsync(
                        compensation.Action.Payload,
                        transaction,
                        metadata,
                        null,
                        cancellationToken);
                    break;
                }

            case ProcessEffect.ScheduleTimeout timeout:
                {
                    var metadata = new OutboxMessageMetadata(
                        correlationId: correlationId,
                        causationId: correlationId,
                        messageType: timeout.TriggerType);

                    DateTimeOffset deliverAt = _timeProvider.GetUtcNow().Add(timeout.Delay);

                    await _outbox.StoreAsync(
                        timeout.TimeoutTrigger,
                        transaction,
                        metadata,
                        deliverAt,
                        cancellationToken);
                    break;
                }
        }
    }
}



