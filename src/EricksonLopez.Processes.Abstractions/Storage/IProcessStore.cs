// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines the persistence contract for loading, querying, and saving process instances in durable storage.
/// </summary>
/// <typeparam name="TState">The process domain state type.</typeparam>
public interface IProcessStore<TState>
    where TState : notnull
{
    /// <summary>
    /// Retrieves a process instance by its unique identifier.
    /// </summary>
    /// <param name="id">The unique process identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the persisted <see cref="ProcessInstance{TState}"/>, or <see langword="null"/> if not found.</returns>
    ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new or updated process instance using optimistic concurrency control.
    /// </summary>
    /// <param name="instance">The process instance to save.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains a <see cref="ProcessSaveResult"/> indicating the outcome.</returns>
    ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a process instance with the specified identifier exists in storage.
    /// </summary>
    /// <param name="id">The unique process identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains <see langword="true"/> if the instance exists; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a process instance by its business correlation identifier.
    /// </summary>
    /// <param name="correlationId">The business correlation identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the matching <see cref="ProcessInstance{TState}"/>, or <see langword="null"/> if not found.</returns>
    ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<ProcessInstance<TState>?>(null);
}





