// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Outbox.Tests;

using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Outbox;
using EricksonLopez.Outbox.Persistence;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

[Trait("Category", "Unit")]
public sealed class OutboxProcessDispatcherTests
{
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly IOutboxTransactionContext _transaction = Substitute.For<IOutboxTransactionContext>();

    public sealed record SampleCommand(string Value);
    public sealed record SampleEvent(string EventName);
    public sealed record CustomUnmatchedEffect : ProcessEffect;

    [Fact]
    public void Constructor_NullOutbox_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ = new OutboxProcessDispatcher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("outbox");
    }

    [Fact]
    public async Task DispatchEffectsAsync_NullEffects_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();

        // Act
        Func<Task> act = async () => await dispatcher.DispatchEffectsAsync(null!, processId, _transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("effects");
    }

    [Fact]
    public async Task DispatchEffectsAsync_EmptyEffects_DoesNothing()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();

        // Act
        await dispatcher.DispatchEffectsAsync([], processId, _transaction);

        // Assert
        await _outbox.DidNotReceiveWithAnyArgs().StoreAsync<object>(
            default!,
            default!,
            default!,
            default,
            default);
    }

    [Fact]
    public async Task DispatchEffectsAsync_MultipleEffects_DispatchesAllWithTransactionAndCancellationToken()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var cmd = new SampleCommand("BatchCmd");
        var evt = new SampleEvent("BatchEvt");
        using var cts = new CancellationTokenSource();

        var effects = new ProcessEffect[]
        {
            new ProcessEffect.Command(cmd, "BatchCommand"),
            new ProcessEffect.Event(evt, "BatchEvent")
        };

        // Act
        await dispatcher.DispatchEffectsAsync(effects, processId, _transaction, cts.Token);

        // Assert
        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleCommand>(c => ReferenceEquals(c, cmd)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m => m.CorrelationId == processId.Value.ToString() && m.CausationId == processId.Value.ToString() && m.MessageType == "BatchCommand"),
            Arg.Is<DateTimeOffset?>(d => d == null),
            Arg.Is<CancellationToken>(c => c == cts.Token));

        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m => m.CorrelationId == processId.Value.ToString() && m.CausationId == processId.Value.ToString() && m.MessageType == "BatchEvent"),
            Arg.Is<DateTimeOffset?>(d => d == null),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_NullEffect_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();

        // Act
        Func<Task> act = async () => await dispatcher.DispatchEffectAsync(null!, processId, _transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("effect");
    }

    [Fact]
    public async Task DispatchEffectAsync_NullTransaction_DoesNotStoreInOutbox()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var cmd = new SampleCommand("TestPayload");
        var effect = new ProcessEffect.Command(cmd, "SampleCommand");

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, transaction: null);

        // Assert
        await _outbox.DidNotReceiveWithAnyArgs().StoreAsync<object>(
            default!,
            default!,
            default!,
            default,
            default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CommandEffect_StoresInOutboxWithCorrectMetadataAndNoDelay()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var cmd = new SampleCommand("TestPayload");
        var effect = new ProcessEffect.Command(cmd, "SampleCommand");
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, _transaction, cts.Token);

        // Assert
        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleCommand>(o => ReferenceEquals(o, cmd)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m =>
                m.CorrelationId == processId.Value.ToString() &&
                m.CausationId == processId.Value.ToString() &&
                m.MessageType == "SampleCommand"),
            Arg.Is<DateTimeOffset?>(d => d == null),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_EventEffect_StoresInOutboxWithCorrectMetadataAndNoDelay()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var evt = new SampleEvent("OrderPlaced");
        var effect = new ProcessEffect.Event(evt, "OrderPlacedEvent");
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, _transaction, cts.Token);

        // Assert
        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleEvent>(o => ReferenceEquals(o, evt)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m =>
                m.CorrelationId == processId.Value.ToString() &&
                m.CausationId == processId.Value.ToString() &&
                m.MessageType == "OrderPlacedEvent"),
            Arg.Is<DateTimeOffset?>(d => d == null),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_CompensationEffect_StoresInOutboxWithCorrectMetadataAndNoDelay()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var payload = new SampleCommand("Refund");
        var compensationAction = new CompensationAction("RefundStep", payload);
        var effect = new ProcessEffect.Compensation(compensationAction);
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, _transaction, cts.Token);

        // Assert
        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleCommand>(o => ReferenceEquals(o, payload)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m =>
                m.CorrelationId == processId.Value.ToString() &&
                m.CausationId == processId.Value.ToString() &&
                m.MessageType == "RefundStep"),
            Arg.Is<DateTimeOffset?>(d => d == null),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_StoresInOutboxWithCalculatedDeliverAt()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2030, 5, 1, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(baseTime);
        var dispatcher = new OutboxProcessDispatcher(_outbox, timeProvider);
        var processId = ProcessId.NewId();
        var timeoutTrigger = new SampleEvent("WakeUp");
        var delay = TimeSpan.FromMinutes(10);
        var effect = new ProcessEffect.ScheduleTimeout(delay, timeoutTrigger, "WakeUpEvent");
        using var cts = new CancellationTokenSource();
        var expectedDeliverAt = baseTime.Add(delay);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, _transaction, cts.Token);

        // Assert
        await _outbox.Received(1).StoreAsync(
            Arg.Is<SampleEvent>(o => ReferenceEquals(o, timeoutTrigger)),
            Arg.Is<IOutboxTransactionContext>(t => ReferenceEquals(t, _transaction)),
            Arg.Is<OutboxMessageMetadata>(m =>
                m.CorrelationId == processId.Value.ToString() &&
                m.CausationId == processId.Value.ToString() &&
                m.MessageType == "WakeUpEvent"),
            Arg.Is<DateTimeOffset?>(d => d == expectedDeliverAt),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_CustomEffect_DoesNotCallOutbox()
    {
        // Arrange
        var dispatcher = new OutboxProcessDispatcher(_outbox);
        var processId = ProcessId.NewId();
        var customEffect = new CustomUnmatchedEffect();

        // Act
        await dispatcher.DispatchEffectAsync(customEffect, processId, _transaction);

        // Assert
        await _outbox.DidNotReceiveWithAnyArgs().StoreAsync<object>(
            default!,
            default!,
            default!,
            default,
            default);
    }

    [Fact]
    public void AddProcessesOutbox_NullServices_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => ProcessOutboxServiceCollectionExtensions.AddProcessesOutbox(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddProcessesOutbox_RegistersServiceInContainer_WithScopedLifetimeAndReturnsServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddProcessesOutbox();

        // Assert
        result.Should().BeSameAs(services);
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProcessOutboxDispatcher));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<OutboxProcessDispatcher>();
    }
}





