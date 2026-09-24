// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes;

/// <summary>
/// Provides configuration options for the <see cref="ProcessCoordinator{TState}"/> execution pipeline.
/// </summary>
public sealed class ProcessCoordinatorOptions
{
    private int _maxConcurrencyRetries = 3;
    private TimeSpan _initialBackoffDelay = TimeSpan.FromMilliseconds(50);
    private int _maxCompensations = 1000;

    /// <summary>
    /// Gets or sets the maximum number of optimistic concurrency retry attempts.
    /// </summary>
    public int MaxConcurrencyRetries
    {
        get => _maxConcurrencyRetries;
        set => _maxConcurrencyRetries = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets the initial backoff delay used for retrying on concurrency conflicts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than or equal to <see cref="TimeSpan.Zero"/></exception>
    public TimeSpan InitialBackoffDelay
    {
        get => _initialBackoffDelay;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Initial backoff delay must be greater than zero.");
            }
            _initialBackoffDelay = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of compensation steps allowed before throwing an exception to prevent infinite saga growth.
    /// </summary>
    public int MaxCompensations
    {
        get => _maxCompensations;
        set => _maxCompensations = Math.Max(0, value);
    }
}


