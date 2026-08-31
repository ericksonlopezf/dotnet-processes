// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Outbox;

using EricksonLopez.Outbox.Persistence;
using EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines a contract for dispatching process effects to a transactional outbox.
/// </summary>
public interface IProcessOutboxDispatcher
{
    /// <summary>
    /// Dispatches a sequence of process effects to the outbox within the specified transaction context.
    /// </summary>
    /// <param name="effects">The collection of process effects to dispatch.</param>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="transaction">The optional outbox transaction context.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask DispatchEffectsAsync(
        IEnumerable<ProcessEffect> effects,
        ProcessId processId,
        IOutboxTransactionContext? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a single process effect to the outbox within the specified transaction context.
    /// </summary>
    /// <param name="effect">The process effect to dispatch.</param>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="transaction">The optional outbox transaction context.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask DispatchEffectAsync(
        ProcessEffect effect,
        ProcessId processId,
        IOutboxTransactionContext? transaction = null,
        CancellationToken cancellationToken = default);
}



