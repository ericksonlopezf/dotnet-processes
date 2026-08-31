// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Events;

/// <summary>
/// Defines a contract for dispatching process effects as domain or integration events through an event publisher.
/// </summary>
public interface IEventProcessDispatcher
{
    /// <summary>
    /// Dispatches a batch of process effects through the event publisher.
    /// </summary>
    /// <param name="effects">The collection of process effects to dispatch.</param>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask DispatchEffectsAsync(
        IEnumerable<ProcessEffect> effects,
        ProcessId processId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a single process effect through the event publisher.
    /// </summary>
    /// <param name="effect">The process effect to dispatch.</param>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask DispatchEffectAsync(
        ProcessEffect effect,
        ProcessId processId,
        CancellationToken cancellationToken = default);
}
