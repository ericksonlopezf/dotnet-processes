// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

internal static class ProcessTransitionDefaults
{
    internal static readonly IReadOnlyList<ProcessEffect> EmptyEffects = Array.Empty<ProcessEffect>();
    internal static readonly IReadOnlyList<CompensationStep> EmptyCompensations = Array.Empty<CompensationStep>();
}

/// <summary>
/// Represents the outcome of handling an event within a process manager or saga.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
public sealed record ProcessTransitionResult<TState>
    where TState : notnull
{
    /// <summary>
    /// Gets the updated domain state.
    /// </summary>
    public TState State { get; init; }

    /// <summary>
    /// Gets the new lifecycle status of the process instance.
    /// </summary>
    public ProcessStatus Status { get; init; }

    /// <summary>
    /// Gets the list of side-effect intents emitted by this transition.
    /// </summary>
    public IReadOnlyList<ProcessEffect> Effects { get; init; }

    /// <summary>
    /// Gets the list of newly recorded compensation steps produced by forward step execution.
    /// </summary>
    public IReadOnlyList<CompensationStep> RecordedCompensations { get; init; }

    /// <summary>
    /// Gets the reason for failure if the process transitioned to <see cref="ProcessStatus.Failed"/>.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTransitionResult{TState}"/> record with state, status, and side effects.
    /// </summary>
    /// <param name="state">The updated domain state.</param>
    /// <param name="status">The target lifecycle status.</param>
    /// <param name="effects">The optional list of emitted side-effect intents.</param>
    /// <param name="recordedCompensations">The optional list of recorded compensation steps.</param>
    /// <param name="failureReason">The optional failure reason explanation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/></exception>
    public ProcessTransitionResult(
        TState state,
        ProcessStatus status,
        IReadOnlyList<ProcessEffect>? effects = null,
        IReadOnlyList<CompensationStep>? recordedCompensations = null,
        string? failureReason = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Status = status;
        Effects = effects ?? ProcessTransitionDefaults.EmptyEffects;
        RecordedCompensations = recordedCompensations ?? ProcessTransitionDefaults.EmptyCompensations;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Advances the process to a new state and status.
    /// </summary>
    /// <param name="state">The updated domain state.</param>
    /// <param name="status">The target lifecycle status (defaults to <see cref="ProcessStatus.Running"/>).</param>
    /// <param name="effects">The optional side-effect intents to emit.</param>
    /// <param name="recordedCompensations">The optional compensation steps to record.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Advance(
        TState state,
        ProcessStatus status = ProcessStatus.Running,
        IEnumerable<ProcessEffect>? effects = null,
        IEnumerable<CompensationStep>? recordedCompensations = null)
    {
        return new ProcessTransitionResult<TState>(
            state: state,
            status: status,
            effects: effects is not null ? [.. effects] : ProcessTransitionDefaults.EmptyEffects,
            recordedCompensations: recordedCompensations is not null ? [.. recordedCompensations] : ProcessTransitionDefaults.EmptyCompensations);
    }

    /// <summary>
    /// Transitions the process to the <see cref="ProcessStatus.Completed"/> terminal state.
    /// </summary>
    /// <param name="state">The final domain state.</param>
    /// <param name="effects">The optional side-effect intents to emit.</param>
    /// <param name="recordedCompensations">The optional compensation steps to record.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Complete(
        TState state,
        IEnumerable<ProcessEffect>? effects = null,
        IEnumerable<CompensationStep>? recordedCompensations = null)
    {
        return new ProcessTransitionResult<TState>(
            state: state,
            status: ProcessStatus.Completed,
            effects: effects is not null ? [.. effects] : ProcessTransitionDefaults.EmptyEffects,
            recordedCompensations: recordedCompensations is not null ? [.. recordedCompensations] : ProcessTransitionDefaults.EmptyCompensations);
    }

    /// <summary>
    /// Transitions the process to the <see cref="ProcessStatus.Suspended"/> state awaiting external confirmation or a timer.
    /// </summary>
    /// <param name="state">The suspended domain state.</param>
    /// <param name="effects">The optional side-effect intents to emit.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Suspend(
        TState state,
        IEnumerable<ProcessEffect>? effects = null)
    {
        return new ProcessTransitionResult<TState>(
            state: state,
            status: ProcessStatus.Suspended,
            effects: effects is not null ? [.. effects] : ProcessTransitionDefaults.EmptyEffects);
    }

    /// <summary>
    /// Transitions the saga to the <see cref="ProcessStatus.Compensating"/> state and emits compensating intents.
    /// </summary>
    /// <param name="state">The current domain state.</param>
    /// <param name="compensationActions">The optional compensation actions to trigger.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Compensate(
        TState state,
        IEnumerable<CompensationAction>? compensationActions = null)
    {
        IReadOnlyList<ProcessEffect> effects = compensationActions is not null
            ? [.. compensationActions.Select(action => new ProcessEffect.Compensation(action))]
            : ProcessTransitionDefaults.EmptyEffects;

        return new ProcessTransitionResult<TState>(
            state: state,
            status: ProcessStatus.Compensating,
            effects: effects);
    }

    /// <summary>
    /// Transitions the saga to the <see cref="ProcessStatus.Compensated"/> terminal state after all compensations execute.
    /// </summary>
    /// <param name="state">The compensated domain state.</param>
    /// <param name="effects">The optional side-effect intents to emit.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Compensated(
        TState state,
        IEnumerable<ProcessEffect>? effects = null)
    {
        return new ProcessTransitionResult<TState>(
            state: state,
            status: ProcessStatus.Compensated,
            effects: effects is not null ? [.. effects] : ProcessTransitionDefaults.EmptyEffects);
    }

    /// <summary>
    /// Transitions the process to the <see cref="ProcessStatus.Failed"/> terminal or escalated state.
    /// </summary>
    /// <param name="state">The failed domain state.</param>
    /// <param name="reason">The reason describing why the transition failed.</param>
    /// <param name="effects">The optional side-effect intents to emit.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is <see langword="null"/> or white-space</exception>
    public static ProcessTransitionResult<TState> Fail(
        TState state,
        string reason,
        IEnumerable<ProcessEffect>? effects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ProcessTransitionResult<TState>(
            state: state,
            status: ProcessStatus.Failed,
            effects: effects is not null ? [.. effects] : ProcessTransitionDefaults.EmptyEffects,
            failureReason: reason);
    }

    /// <summary>
    /// Returns a transition result that preserves the existing state and status without changes.
    /// </summary>
    /// <param name="state">The current domain state.</param>
    /// <param name="currentStatus">The current lifecycle status.</param>
    /// <returns>A new <see cref="ProcessTransitionResult{TState}"/> instance.</returns>
    public static ProcessTransitionResult<TState> Unchanged(TState state, ProcessStatus currentStatus = ProcessStatus.Running)
    {
        return new ProcessTransitionResult<TState>(
            state: state,
            status: currentStatus,
            effects: ProcessTransitionDefaults.EmptyEffects);
    }
}




