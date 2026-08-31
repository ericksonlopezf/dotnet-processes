// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Events;

/// <summary>
/// Provides extension and mapping methods between process identifiers and event identifiers.
/// </summary>
public static class ProcessEventsIdentifierExtensions
{
    /// <summary>
    /// Converts a process correlation identifier to an events correlation identifier.
    /// </summary>
    /// <param name="id">The process correlation identifier to convert.</param>
    /// <returns>A new <see cref="EricksonLopez.Events.Identifiers.CorrelationId"/> with the same value.</returns>
    public static EricksonLopez.Events.Identifiers.CorrelationId ToEventsCorrelationId(this CorrelationId id) =>
        new(id.Value);

    /// <summary>
    /// Converts an events correlation identifier to a process correlation identifier.
    /// </summary>
    /// <param name="id">The events correlation identifier to convert.</param>
    /// <returns>A new <see cref="EricksonLopez.Processes.Abstractions.CorrelationId"/> with the same value.</returns>
    public static CorrelationId ToProcessesCorrelationId(this EricksonLopez.Events.Identifiers.CorrelationId id) =>
        new(id.Value);

    /// <summary>
    /// Converts a process causation identifier to an events causation identifier.
    /// </summary>
    /// <param name="id">The process causation identifier to convert.</param>
    /// <returns>A new <see cref="EricksonLopez.Events.Identifiers.CausationId"/> with the same value.</returns>
    public static EricksonLopez.Events.Identifiers.CausationId ToEventsCausationId(this CausationId id) =>
        new(id.Value);

    /// <summary>
    /// Converts an events causation identifier to a process causation identifier.
    /// </summary>
    /// <param name="id">The events causation identifier to convert.</param>
    /// <returns>A new <see cref="EricksonLopez.Processes.Abstractions.CausationId"/> with the same value.</returns>
    public static CausationId ToProcessesCausationId(this EricksonLopez.Events.Identifiers.CausationId id) =>
        new(id.Value);
}
