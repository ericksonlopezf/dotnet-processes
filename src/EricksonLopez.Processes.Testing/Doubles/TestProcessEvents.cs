// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Represents a domain event for the creation of a test order.
/// </summary>
/// <param name="OrderId">The process and order identifier.</param>
/// <param name="Amount">The initial order amount.</param>
public sealed record TestOrderCreatedEvent(Guid OrderId, decimal Amount);

/// <summary>
/// Represents a domain event for payment completion of a test order.
/// </summary>
/// <param name="OrderId">The process and order identifier.</param>
public sealed record TestOrderPaidEvent(Guid OrderId);

/// <summary>
/// Represents a domain event for incrementing a test counter process.
/// </summary>
/// <param name="TargetId">The target process identifier.</param>
/// <param name="Delta">The amount to increment.</param>
public sealed record TestIncrementEvent(ProcessId TargetId, int Delta = 1);

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


