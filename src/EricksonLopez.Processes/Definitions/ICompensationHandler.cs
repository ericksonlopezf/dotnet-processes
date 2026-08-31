// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Defines a contract for executing compensation logic for a previously completed saga step.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
public interface ICompensationHandler<TState>
    where TState : notnull
{
    /// <summary>
    /// Executes compensation logic for the specified compensation action and payload.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="action">The compensation action containing the step name and payload.</param>
    /// <param name="context">The process execution context.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the resulting process state transition.</returns>
    ValueTask<ProcessTransitionResult<TState>> CompensateAsync(
        TState state,
        CompensationAction action,
        ProcessContext context);
}





