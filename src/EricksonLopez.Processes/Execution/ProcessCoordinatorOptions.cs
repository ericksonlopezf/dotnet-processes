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
    /// <summary>
    /// Gets or sets the maximum number of optimistic concurrency retry attempts.
    /// </summary>
    public int MaxConcurrencyRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial backoff delay used for retrying on concurrency conflicts.
    /// </summary>
    public TimeSpan InitialBackoffDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}


