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
    /// Executes all recorded compensation steps in reverse order (LIFO).
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="initialState">The current state of the saga before compensation starts.</param>
    /// <param name="recordedSteps">The list of steps recorded during forward execution.</param>
    /// <param name="handler">The compensation handler.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the final compensated or failed transition result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> or <paramref name="context"/> is <see langword="null"/></exception>
    public static async ValueTask<ProcessTransitionResult<TState>> ExecuteCompensationAsync<TState>(
        TState initialState,
        IReadOnlyList<CompensationStep> recordedSteps,
        ICompensationHandler<TState> handler,
        ProcessContext context)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(context);

        if (recordedSteps is null || recordedSteps.Count == 0)
        {
            return ProcessTransitionResult<TState>.Compensated(initialState);
        }

        var currentState = initialState;
        var accumulatedEffects = new List<ProcessEffect>();

        // Execute in reverse order (LIFO)
        for (var i = recordedSteps.Count - 1; i >= 0; i--)
        {
            var step = recordedSteps[i];
            var action = new CompensationAction(step.StepName, step.Payload);

            try
            {
                var stepResult = await handler.CompensateAsync(currentState, action, context);
                currentState = stepResult.State;

                foreach (var effect in stepResult.Effects)
                {
                    accumulatedEffects.Add(effect);
                }

                if (stepResult.Status == ProcessStatus.Failed)
                {
                    return ProcessTransitionResult<TState>.Fail(
                        currentState,
                        $"Compensation step '{step.StepName}' explicitly returned Failed status: {stepResult.FailureReason}",
                        accumulatedEffects);
                }
            }
            catch (Exception ex) when (ex is not (ProcessException or OperationCanceledException))
            {
                return ProcessTransitionResult<TState>.Fail(
                    currentState,
                    $"Compensation step '{step.StepName}' encountered an unexpected exception: {ex.Message}",
                    accumulatedEffects);
            }
        }

        return ProcessTransitionResult<TState>.Compensated(currentState, accumulatedEffects);
    }
}






