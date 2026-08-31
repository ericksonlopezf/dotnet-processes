// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines the snapshot storage contract for capturing and restoring process state checkpoints.
/// </summary>
/// <typeparam name="TState">The process state type.</typeparam>
public interface ISagaSnapshotRepository<TState>
    where TState : notnull
{
    /// <summary>
    /// Saves a snapshot checkpoint of the process state.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="revision">The revision at which the snapshot was captured.</param>
    /// <param name="state">The state instance to snapshot.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask SaveSnapshotAsync(ProcessId processId, Revision revision, TState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent snapshot checkpoint for the given process identifier.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the latest snapshot tuple of revision and state, or <see langword="null"/> if no snapshot exists.</returns>
    ValueTask<(Revision Revision, TState State)?> GetLatestSnapshotAsync(ProcessId processId, CancellationToken cancellationToken = default);
}
