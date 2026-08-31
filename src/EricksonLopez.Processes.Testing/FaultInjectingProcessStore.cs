// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing;

/// <summary>
/// Provides an in-memory test store decorator that injects simulated faults, concurrency conflicts, or forced errors for resilience testing.
/// </summary>
/// <typeparam name="TState">The strongly typed process state type.</typeparam>
public sealed class FaultInjectingProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly IProcessStore<TState> _innerStore;
    private int _concurrencyConflictsToSimulate;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultInjectingProcessStore{TState}"/> class with an optional inner store.
    /// </summary>
    /// <param name="innerStore">The underlying process store, or <see langword="null"/> to create an <see cref="InMemoryProcessStore{TState}"/>.</param>
    public FaultInjectingProcessStore(IProcessStore<TState>? innerStore = null)
    {
        _innerStore = innerStore ?? new InMemoryProcessStore<TState>();
    }

    /// <summary>
    /// Gets the underlying wrapped <see cref="IProcessStore{TState}"/>.
    /// </summary>
    public IProcessStore<TState> InnerStore => _innerStore;

    /// <summary>
    /// Gets or sets the number of transient concurrency conflict results to simulate on successive saves.
    /// </summary>
    public int ConcurrencyConflictsToSimulate
    {
        get => _concurrencyConflictsToSimulate;
        set => _concurrencyConflictsToSimulate = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets a forced <see cref="ProcessSaveResult"/> to return unconditionally on saves.
    /// </summary>
    public ProcessSaveResult? ForcedSaveResult { get; set; }

    /// <summary>
    /// Gets or sets an exception to throw unconditionally during <see cref="SaveAsync"/>.
    /// </summary>
    public Exception? ExceptionToThrowOnSave { get; set; }

    /// <summary>
    /// Gets or sets an exception to throw unconditionally during <see cref="GetByIdAsync"/>.
    /// </summary>
    public Exception? ExceptionToThrowOnGet { get; set; }

    /// <summary>
    /// Gets or sets an exception to throw unconditionally during <see cref="ExistsAsync"/>.
    /// </summary>
    public Exception? ExceptionToThrowOnExists { get; set; }

    /// <summary>
    /// Gets or sets a forced result to return unconditionally during <see cref="ExistsAsync"/>.
    /// </summary>
    public bool? ForcedExistsResult { get; set; }

    /// <summary>
    /// Gets or sets an exception to throw unconditionally during <see cref="GetByCorrelationIdAsync"/>.
    /// </summary>
    public Exception? ExceptionToThrowOnGetByCorrelationId { get; set; }

    /// <summary>
    /// Gets or sets a forced result to return unconditionally during <see cref="GetByCorrelationIdAsync"/>.
    /// </summary>
    public ProcessInstance<TState>? ForcedGetByCorrelationIdResult { get; set; }

    /// <inheritdoc />
    public ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrowOnGet is not null)
        {
            throw ExceptionToThrowOnGet;
        }

        return _innerStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (ExceptionToThrowOnSave is not null)
        {
            throw ExceptionToThrowOnSave;
        }

        if (ForcedSaveResult.HasValue)
        {
            return ValueTask.FromResult(ForcedSaveResult.Value);
        }

        if (_concurrencyConflictsToSimulate > 0)
        {
            Interlocked.Decrement(ref _concurrencyConflictsToSimulate);
            return ValueTask.FromResult(ProcessSaveResult.ConcurrencyConflict);
        }

        return _innerStore.SaveAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrowOnExists is not null)
        {
            throw ExceptionToThrowOnExists;
        }

        if (ForcedExistsResult.HasValue)
        {
            return ValueTask.FromResult(ForcedExistsResult.Value);
        }

        return _innerStore.ExistsAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrowOnGetByCorrelationId is not null)
        {
            throw ExceptionToThrowOnGetByCorrelationId;
        }

        if (ForcedGetByCorrelationIdResult is not null)
        {
            return ValueTask.FromResult<ProcessInstance<TState>?>(ForcedGetByCorrelationIdResult);
        }

        return _innerStore.GetByCorrelationIdAsync(correlationId, cancellationToken);
    }
}



