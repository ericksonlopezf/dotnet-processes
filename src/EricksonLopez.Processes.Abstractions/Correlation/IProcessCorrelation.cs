// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines a strongly typed correlation extractor mapping incoming events to process identifiers.
/// </summary>
/// <typeparam name="TEvent">The event type to correlate.</typeparam>
public interface IProcessCorrelation<in TEvent>
{
    /// <summary>
    /// Extracts the process instance identifier from the specified event.
    /// </summary>
    /// <param name="event">The incoming event instance.</param>
    /// <returns>The target <see cref="ProcessId"/>.</returns>
    ProcessId ExtractProcessId(TEvent @event);

    /// <summary>
    /// Extracts the business correlation identifier from the specified event.
    /// </summary>
    /// <param name="event">The incoming event instance.</param>
    /// <returns>The target <see cref="CorrelationId"/>.</returns>
    CorrelationId ExtractCorrelationId(TEvent @event);

    /// <summary>
    /// Extracts the optional causation identifier from the specified event.
    /// </summary>
    /// <param name="event">The incoming event instance.</param>
    /// <returns>The <see cref="CausationId"/> if present; otherwise, <see langword="null"/>.</returns>
    CausationId? ExtractCausationId(TEvent @event) => null;
}




