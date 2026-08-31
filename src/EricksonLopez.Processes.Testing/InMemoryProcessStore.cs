// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing;

/// <summary>
/// Provides a thread-safe in-memory store simulating atomic optimistic concurrency control for unit and integration tests.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
public sealed class InMemoryProcessStore<TState> : IProcessStore<TState>
    where TState : notnull
{
    private readonly ConcurrentDictionary<ProcessId, ProcessInstance<TState>> _instances = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public ValueTask<ProcessInstance<TState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        _instances.TryGetValue(id, out var instance);
        return ValueTask.FromResult(instance);
    }

    /// <inheritdoc />
    public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TState> instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        lock (_lock)
        {
            if (!_instances.TryGetValue(instance.Id, out var existing))
            {
                // New instance creation: must have revision Initial (1)
                _instances[instance.Id] = instance;
                return ValueTask.FromResult(ProcessSaveResult.Success);
            }

            // Existing update: expected revision in DB is (instance.Revision - 1)
            if (existing.Revision.Value != instance.Revision.Value - 1)
            {
                return ValueTask.FromResult(ProcessSaveResult.ConcurrencyConflict);
            }

            _instances[instance.Id] = instance;
            return ValueTask.FromResult(ProcessSaveResult.Success);
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_instances.ContainsKey(id));
    }

    /// <inheritdoc />
    public ValueTask<ProcessInstance<TState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default)
    {
        foreach (var instance in _instances.Values)
        {
            if (instance.CorrelationId == correlationId)
            {
                return ValueTask.FromResult<ProcessInstance<TState>?>(instance);
            }
        }

        return ValueTask.FromResult<ProcessInstance<TState>?>(null);
    }
}





