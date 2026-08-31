// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Events.Contracts;
using EricksonLopez.Events.Identifiers;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Processes.Events.Tests;

public sealed class EventProcessDispatcherTests
{
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly EventProcessDispatcher _sut;
    private readonly ProcessId _processId = ProcessId.NewId();

    public EventProcessDispatcherTests()
    {
        _sut = new EventProcessDispatcher(_eventPublisher);
    }

    private sealed record TestDomainEvent(string Value) : IDomainEvent
    {
        public EventId Id { get; init; } = EventId.New();
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    }

    private sealed record TestIntegrationEvent(string Value) : IIntegrationEvent
    {
        public EventId Id { get; init; } = EventId.New();
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    }

    private sealed class AsyncYieldingEventPublisher : IEventPublisher
    {
        public async ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class TrackingSynchronizationContext : SynchronizationContext
    {
        public int PostCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref PostCount);
            d(state);
        }
    }

    [Fact]
    public void Constructor_WhenEventPublisherNull_ThrowsArgumentNullException()
    {
        var act = () => new EventProcessDispatcher(null!);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("eventPublisher");
    }

#pragma warning disable xUnit1031
    [Fact]
    public void DispatchEffectsAsync_Batch_WithAsyncPublisher_ShouldNotCaptureSynchronizationContext()
    {
        var syncContext = new TrackingSynchronizationContext();
        var prev = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(syncContext);
            var sut = new EventProcessDispatcher(new AsyncYieldingEventPublisher());
            var evt = new TestDomainEvent("sync-test");
            sut.DispatchEffectsAsync([new ProcessEffect.Event(evt), new ProcessEffect.Event(evt)], _processId)
               .AsTask()
               .GetAwaiter()
               .GetResult();

            syncContext.PostCount.Should().Be(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }
#pragma warning restore xUnit1031

    [Fact]
    public async Task DispatchEffectsAsync_WhenEffectsNull_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await _sut.DispatchEffectsAsync(null!, _processId);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("effects");
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenEffectNull_ThrowsArgumentNullException()
    {
        Func<Task> act = async () => await _sut.DispatchEffectAsync(null!, _processId);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("effect");
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenEventEffectWithDomainEvent_PublishesEventWithCancellationToken()
    {
        var evt = new TestDomainEvent("order-created");
        var effect = new ProcessEffect.Event(evt);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectAsync(effect, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestDomainEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenEventEffectWithIntegrationEvent_PublishesEventWithCancellationToken()
    {
        var evt = new TestIntegrationEvent("payment-completed");
        var effect = new ProcessEffect.Event(evt);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectAsync(effect, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestIntegrationEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenCommandWithEventPayload_PublishesEventWithCancellationToken()
    {
        var evt = new TestDomainEvent("command-payload-event");
        var effect = new ProcessEffect.Command(evt);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectAsync(effect, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestDomainEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenCompensationWithEventPayload_PublishesEventWithCancellationToken()
    {
        var evt = new TestDomainEvent("compensation-event");
        var compensationAction = new CompensationAction("compensate-step", evt);
        var effect = new ProcessEffect.Compensation(compensationAction);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectAsync(effect, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestDomainEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenScheduleTimeoutWithEventPayload_PublishesEventWithCancellationToken()
    {
        var evt = new TestDomainEvent("timeout-event");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromMinutes(5), evt);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectAsync(effect, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestDomainEvent>(e => ReferenceEquals(e, evt)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public async Task DispatchEffectsAsync_WhenBatch_PublishesAllEventsInSequenceWithCancellationToken()
    {
        var evt1 = new TestDomainEvent("event-1");
        var evt2 = new TestIntegrationEvent("event-2");
        var effects = new List<ProcessEffect>
        {
            new ProcessEffect.Event(evt1),
            new ProcessEffect.Event(evt2)
        };
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        await _sut.DispatchEffectsAsync(effects, _processId, ct);

        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestDomainEvent>(e => ReferenceEquals(e, evt1)),
            Arg.Is<CancellationToken>(c => c == ct));
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TestIntegrationEvent>(e => ReferenceEquals(e, evt2)),
            Arg.Is<CancellationToken>(c => c == ct));
    }

    [Fact]
    public void AddProcessEventsDispatcher_WhenServicesNull_ThrowsArgumentNullException()
    {
        Action act = () => ProcessEventsServiceCollectionExtensions.AddProcessEventsDispatcher(null!);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenEventPayloadIsNotIEvent_DoesNotPublish()
    {
        var effect = new ProcessEffect.Event("not-an-ievent");
        await _sut.DispatchEffectAsync(effect, _processId);

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<IEvent>(default!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenCommandPayloadIsNotIEvent_DoesNotPublish()
    {
        var effect = new ProcessEffect.Command("not-an-ievent");
        await _sut.DispatchEffectAsync(effect, _processId);

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<IEvent>(default!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenCompensationPayloadIsNotIEvent_DoesNotPublish()
    {
        var effect = new ProcessEffect.Compensation(new CompensationAction("step", "not-an-ievent"));
        await _sut.DispatchEffectAsync(effect, _processId);

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<IEvent>(default!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_WhenScheduleTimeoutTriggerIsNotIEvent_DoesNotPublish()
    {
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromMinutes(1), "not-an-ievent");
        await _sut.DispatchEffectAsync(effect, _processId);

        await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync<IEvent>(default!, default);
    }

    [Fact]
    public void AddProcessEventsDispatcher_RegistersServiceInServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_eventPublisher);

        var returnedServices = services.AddProcessEventsDispatcher();
        returnedServices.Should().BeSameAs(services);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventProcessDispatcher));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<EventProcessDispatcher>();

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IEventProcessDispatcher>();

        dispatcher.Should().NotBeNull();
        dispatcher.Should().BeOfType<EventProcessDispatcher>();
    }
}

