// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Executes reverse-order compensation workflows for sagas.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class SagaCompensationEngine
{
    /// <summary>
    /// Executes a single recorded compensation step.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="currentState">The current state of the saga before this compensation step starts.</param>
    /// <param name="step">The compensation step to execute.</param>
    /// <param name="handler">The compensation handler.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the compensated or failed transition result for this step.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> or <paramref name="context"/> is <see langword="null"/></exception>
    public static async ValueTask<ProcessTransitionResult<TState>> ExecuteNextCompensationStepAsync<TState>(
        TState currentState,
        CompensationStep step,
        ICompensationHandler<TState> handler,
        ProcessContext context)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(context);

        var action = new CompensationAction(step.StepName, step.Payload);

        try
        {
            var stepResult = await handler.CompensateAsync(currentState, action, context);

            if (stepResult.Status == ProcessStatus.Failed)
            {
                return ProcessTransitionResult<TState>.Fail(
                    stepResult.State,
                    $"Compensation step '{step.StepName}' explicitly returned Failed status: {stepResult.FailureReason}",
                    stepResult.Effects);
            }

            return stepResult;
        }
        catch (Exception ex) when (ex is not (ProcessException or OperationCanceledException))
        {
            return ProcessTransitionResult<TState>.Fail(
                currentState,
                $"Compensation step '{step.StepName}' encountered an unexpected exception: {ex.Message}",
                []);
        }
    }
}






