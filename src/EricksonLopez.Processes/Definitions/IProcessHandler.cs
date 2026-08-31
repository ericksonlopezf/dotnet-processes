// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Defines a strongly typed handler contract for reacting to an incoming domain event within a process manager.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
/// <typeparam name="TEvent">The incoming domain event type.</typeparam>
public interface IProcessHandler<TState, in TEvent> : IProcess<TState>
    where TState : notnull
{
    /// <summary>
    /// Handles the incoming event message, producing the resulting updated state and emitted effects.
    /// </summary>
    /// <param name="state">The current process state.</param>
    /// <param name="eventMessage">The incoming event payload.</param>
    /// <param name="context">The process execution context.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the state transition result.</returns>
    ValueTask<ProcessTransitionResult<TState>> HandleAsync(
        TState state,
        TEvent eventMessage,
        ProcessContext context);
}






