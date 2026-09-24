// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a persistent container that encapsulates execution metadata alongside the domain state.
/// </summary>
/// <typeparam name="TState">The type of the domain state.</typeparam>
public sealed record ProcessInstance<TState>
    where TState : notnull
{
    /// <summary>
    /// Gets the unique identifier of the process instance.
    /// </summary>
    public ProcessId Id { get; init; }

    /// <summary>
    /// Gets the logical process type identifier.
    /// </summary>
    public ProcessType Type { get; init; }

    /// <summary>
    /// Gets the schema or definition version of the process instance.
    /// </summary>
    public ProcessVersion Version { get; init; }

    /// <summary>
    /// Gets the current lifecycle status of the process instance.
    /// </summary>
    public ProcessStatus Status { get; init; }

    /// <summary>
    /// Gets the optimistic concurrency revision token.
    /// </summary>
    public Revision Revision { get; init; }

    /// <summary>
    /// Gets the business correlation identifier associated with this instance.
    /// </summary>
    public CorrelationId CorrelationId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process instance was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the process reached a terminal status, if any.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the immutable domain state payload.
    /// </summary>
    public TState State { get; init; }

    /// <summary>
    /// Gets the list of recorded compensation steps, if any.
    /// </summary>
    public IReadOnlyList<CompensationStep> RecordedCompensations { get; init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessInstance{TState}"/> record with the specified metadata and domain state.
    /// </summary>
    /// <param name="id">The unique identifier of the process instance.</param>
    /// <param name="type">The logical process type identifier.</param>
    /// <param name="version">The schema or definition version.</param>
    /// <param name="status">The lifecycle execution status.</param>
    /// <param name="revision">The optimistic concurrency revision token.</param>
    /// <param name="correlationId">The business correlation identifier.</param>
    /// <param name="createdAt">The UTC creation timestamp.</param>
    /// <param name="updatedAt">The UTC last-updated timestamp.</param>
    /// <param name="completedAt">The optional UTC completion timestamp.</param>
    /// <param name="state">The immutable domain state payload.</param>
    /// <param name="recordedCompensations">The optional list of recorded compensation steps.</param>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/></exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Comprehensive domain state record requires all core instance properties")]
    public ProcessInstance(
        ProcessId id,
        ProcessType type,
        ProcessVersion version,
        ProcessStatus status,
        Revision revision,
        CorrelationId correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? completedAt,
        TState state,
        IReadOnlyList<CompensationStep>? recordedCompensations = null)
    {
        Id = id;
        Type = type;
        Version = version;
        Status = status;
        Revision = revision;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        CompletedAt = completedAt;
        State = state ?? throw new ArgumentNullException(nameof(state));
        RecordedCompensations = recordedCompensations ?? [];
    }

    /// <summary>
    /// Creates a new <see cref="ProcessInstance{TState}"/> in the initialized status with the initial revision.
    /// </summary>
    /// <param name="id">The unique process identifier.</param>
    /// <param name="type">The logical process type identifier.</param>
    /// <param name="version">The schema or definition version.</param>
    /// <param name="correlationId">The business correlation identifier.</param>
    /// <param name="initialState">The initial domain state payload.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <returns>A new <see cref="ProcessInstance{TState}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="initialState"/> is <see langword="null"/></exception>
    public static ProcessInstance<TState> Create(
        ProcessId id,
        ProcessType type,
        ProcessVersion version,
        CorrelationId correlationId,
        TState initialState,
        DateTimeOffset now)
    {
        return new ProcessInstance<TState>(
            id: id,
            type: type,
            version: version,
            status: ProcessStatus.Initialized,
            revision: Revision.Initial,
            correlationId: correlationId,
            createdAt: now,
            updatedAt: now,
            completedAt: null,
            state: initialState,
            recordedCompensations: []);
    }

    /// <summary>
    /// Advances the process instance to a new state and status with an incremented revision.
    /// </summary>
    /// <param name="newState">The updated domain state payload.</param>
    /// <param name="newStatus">The updated lifecycle status.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <param name="newCompensations">The optional new compensation steps recorded during this transition.</param>
    /// <param name="maxCompensations">The maximum allowed compensations before throwing an exception. Defaults to 1000.</param>
    /// <returns>A new <see cref="ProcessInstance{TState}"/> record representing the advanced state.</returns>
    /// <exception cref="InvalidProcessTransitionException">The process instance is in a terminal status and cannot be advanced</exception>
    /// <exception cref="InvalidOperationException">The number of compensations exceeded the maximum allowed</exception>
    public ProcessInstance<TState> Advance(
        TState newState,
        ProcessStatus newStatus,
        DateTimeOffset now,
        IReadOnlyList<CompensationStep>? newCompensations = null,
        int maxCompensations = 1000)
    {
        if (Status is ProcessStatus.Completed or ProcessStatus.Compensated or ProcessStatus.Failed)
        {
            throw new InvalidProcessTransitionException(
                Id, Status, newStatus, $"Cannot advance process '{Id}' because it is in terminal status '{Status}'.");
        }

        var completedAt = newStatus is ProcessStatus.Completed or ProcessStatus.Compensated or ProcessStatus.Failed
            ? now
            : (DateTimeOffset?)null;

        var compensations = (newCompensations is { Count: > 0 })
            ? (RecordedCompensations is { Count: > 0 } ? [.. RecordedCompensations, .. newCompensations] : newCompensations)
            : RecordedCompensations;

        if (compensations.Count > maxCompensations)
        {
            throw new InvalidOperationException(
                $"Process '{Id}' exceeded the maximum allowed compensation steps ({maxCompensations}). This is to prevent memory leaks in infinite processes.");
        }

        return this with
        {
            State = newState,
            Status = newStatus,
            Revision = Revision.Next(),
            UpdatedAt = now,
            CompletedAt = completedAt,
            RecordedCompensations = compensations
        };
    }

    /// <summary>
    /// Advances the process instance to a new state during a compensation step, removing the last recorded compensation.
    /// </summary>
    /// <param name="newState">The updated domain state payload.</param>
    /// <param name="newStatus">The updated lifecycle status.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <returns>A new <see cref="ProcessInstance{TState}"/> record representing the advanced state.</returns>
    /// <exception cref="InvalidProcessTransitionException">The process instance is in a terminal status and cannot be advanced</exception>
    public ProcessInstance<TState> AdvanceCompensation(
        TState newState,
        ProcessStatus newStatus,
        DateTimeOffset now)
    {
        if (Status is ProcessStatus.Completed or ProcessStatus.Compensated or ProcessStatus.Failed)
        {
            throw new InvalidProcessTransitionException(
                Id, Status, newStatus, $"Cannot advance compensation for process '{Id}' because it is in terminal status '{Status}'.");
        }

        var completedAt = newStatus is ProcessStatus.Completed or ProcessStatus.Compensated or ProcessStatus.Failed
            ? now
            : (DateTimeOffset?)null;

        var updatedCompensations = RecordedCompensations is { Count: > 0 }
            ? RecordedCompensations.Take(RecordedCompensations.Count - 1).ToList()
            : [];

        return this with
        {
            State = newState,
            Status = newStatus,
            Revision = Revision.Next(),
            UpdatedAt = now,
            CompletedAt = completedAt,
            RecordedCompensations = updatedCompensations
        };
    }
}




