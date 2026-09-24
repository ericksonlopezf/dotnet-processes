// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Provides correlation extraction logic for <see cref="TestOrderCreatedEvent"/>.
/// </summary>
public sealed class TestOrderCreatedCorrelation : IProcessCorrelation<TestOrderCreatedEvent>
{
    /// <inheritdoc />
    public ProcessId ExtractProcessId(TestOrderCreatedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return ProcessId.From(@event.OrderId);
    }

    /// <inheritdoc />
    public CorrelationId ExtractCorrelationId(TestOrderCreatedEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return CorrelationId.From(@event.OrderId.ToString());
    }
}
