// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Provides correlation extraction logic for <see cref="TestIncrementEvent"/>.
/// </summary>
public sealed class TestIncrementCorrelation : IProcessCorrelation<TestIncrementEvent>
{
    /// <inheritdoc />
    public ProcessId ExtractProcessId(TestIncrementEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return @event.TargetId;
    }

    /// <inheritdoc />
    public CorrelationId ExtractCorrelationId(TestIncrementEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return CorrelationId.From(@event.TargetId.ToString());
    }
}
