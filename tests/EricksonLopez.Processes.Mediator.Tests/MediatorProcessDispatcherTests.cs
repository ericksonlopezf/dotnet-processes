// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Mediator.Tests;

using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Mediator;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

[Trait("Category", "Unit")]
public class MediatorProcessDispatcherTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public sealed record SampleProcessCommand(string Value) : ICommand<bool>;
    public sealed record SampleProcessNotification(string EventName) : INotification;
    public sealed record SamplePlainObject(string Name);
    private sealed record CustomUnmatchedEffect() : ProcessEffect;

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MediatorProcessDispatcher(null!);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public async Task DispatchEffectsAsync_NullEffects_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();

        // Act
        Func<Task> act = async () => await dispatcher.DispatchEffectsAsync(null!, processId);

        // Assert
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("effects");
    }

    [Fact]
    public async Task DispatchEffectsAsync_EmptyEffects_DoesNothing()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();

        // Act
        await dispatcher.DispatchEffectsAsync(Enumerable.Empty<ProcessEffect>(), processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectsAsync_MultipleEffects_DispatchesAllWithCancellationToken()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var cmd = new SampleProcessCommand("Action1");
        var notif = new SampleProcessNotification("Event1");
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var effects = new ProcessEffect[]
        {
            new ProcessEffect.Command(cmd),
            new ProcessEffect.Event(notif)
        };

        // Act
        await dispatcher.DispatchEffectsAsync(effects, processId, ct);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<ICommand<bool>>(c => ReferenceEquals(c, cmd)),
            ct);
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
    }

    [Fact]
    public async Task DispatchEffectAsync_NullEffect_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();

        // Act
        Func<Task> act = async () => await dispatcher.DispatchEffectAsync(null!, processId);

        // Assert
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("effect");
    }

    [Fact]
    public async Task DispatchEffectAsync_CommandEffect_WithNotification_PublishesNotificationViaMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("CommandAsNotification");
        var effect = new ProcessEffect.Command(notif);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CommandEffect_WithCommandBool_SendsCommandViaMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var cmd = new SampleProcessCommand("Action");
        var effect = new ProcessEffect.Command(cmd);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<ICommand<bool>>(c => ReferenceEquals(c, cmd)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CommandEffect_WithUnsupportedPayload_DoesNotCallMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("Unsupported");
        var effect = new ProcessEffect.Command(plain);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_EventEffect_WithNotification_PublishesNotificationViaMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("OrderCompleted");
        var effect = new ProcessEffect.Event(notif);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_EventEffect_WithNonNotificationPayload_DoesNotCallMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainEvent");
        var effect = new ProcessEffect.Event(plain);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CompensationEffect_WithNotification_PublishesNotificationViaMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("CompensateNotification");
        var action = new CompensationAction("CompensateStep", notif);
        var effect = new ProcessEffect.Compensation(action);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CompensationEffect_WithCommandBool_SendsCommandViaMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var cmd = new SampleProcessCommand("CompensateCommand");
        var action = new CompensationAction("CompensateStep", cmd);
        var effect = new ProcessEffect.Compensation(action);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<ICommand<bool>>(c => ReferenceEquals(c, cmd)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CompensationEffect_WithUnsupportedPayload_DoesNotCallMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainCompensate");
        var action = new CompensationAction("CompensateStep", plain);
        var effect = new ProcessEffect.Compensation(action);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithPositiveDelay_AwaitsDelayBeforePublishingNotification()
    {
        // Arrange
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        var dispatcher = new MediatorProcessDispatcher(_mediator, fakeTime);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("TimeoutTriggered");
        var delay = TimeSpan.FromSeconds(10);
        var effect = new ProcessEffect.ScheduleTimeout(delay, notif);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        var task = dispatcher.DispatchEffectAsync(effect, processId, ct).AsTask();

        // Assert: Before time advances, delay is active and notification has NOT been published
        task.IsCompleted.Should().BeFalse();
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);

        // Advance time to trigger timeout
        fakeTime.Advance(delay);
        await task.WaitAsync(TimeSpan.FromSeconds(2));
        task.IsCompleted.Should().BeTrue();

        // Assert: After time advances, notification is published
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithZeroDelay_PublishesImmediatelyWithoutDelay()
    {
        // Arrange
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        var dispatcher = new MediatorProcessDispatcher(_mediator, fakeTime);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("TimeoutImmediate");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.Zero, notif);

        // Act
        var task = dispatcher.DispatchEffectAsync(effect, processId);

        // Assert: Completes immediately without needing time to advance
        task.IsCompleted.Should().BeTrue();
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithExactlyZeroDelay_CompletesImmediatelyWithoutAdvancingTime()
    {
        // This test specifically kills the mutant: `timeout.Delay >= TimeSpan.Zero`
        // With the mutant, TimeSpan.Zero would enter `await Task.Delay(...)` path.
        // Using FakeTimeProvider that is never advanced, the task would only complete
        // if time is advanced. The test verifies the task completes WITHOUT advancing time.
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        var dispatcher = new MediatorProcessDispatcher(_mediator, fakeTime);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("ZeroBoundary");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.Zero, notif);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await dispatcher.DispatchEffectAsync(effect, processId, cts.Token);

        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            Arg.Is<CancellationToken>(c => c == cts.Token));
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithZeroDelayAndCanceledToken_DoesNotInvokeDelayAndProceedsToMediator()
    {
        // Arrange: A canceled token causes Task.Delay(TimeSpan.Zero, ct) to throw immediately.
        // If timeout.Delay > TimeSpan.Zero is correctly evaluated, Task.Delay is skipped and mediator receives the invocation.
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("TimeoutCanceled");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.Zero, notif);
        var ct = new CancellationToken(canceled: true);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId, ct);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            ct);
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithNegativeDelay_PublishesImmediatelyWithoutThrowing()
    {
        // Arrange
        var fakeTime = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        var dispatcher = new MediatorProcessDispatcher(_mediator, fakeTime);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("TimeoutNegative");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromSeconds(-5), notif);

        // Act
        var task = dispatcher.DispatchEffectAsync(effect, processId);

        // Assert: Completes immediately without invoking Task.Delay with negative duration (which would throw)
        task.IsCompleted.Should().BeTrue();
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Constructor_NullTimeProvider_DefaultsToSystemTimeProvider()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator, null);
        var processId = ProcessId.NewId();
        var notif = new SampleProcessNotification("TimeoutSystemTime");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromMilliseconds(1), notif);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<SampleProcessNotification>(n => ReferenceEquals(n, notif)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithNonNotificationPayload_DoesNotCallMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainTimeout");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromMinutes(5), plain);

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public async Task DispatchEffectAsync_CustomEffect_DoesNotCallMediator()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var custom = new CustomUnmatchedEffect();

        // Act
        await dispatcher.DispatchEffectAsync(custom, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }

    [Fact]
    public void AddProcessesMediator_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddProcessesMediator();

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddProcessesMediator_RegistersServiceInContainer_WithScopedLifetimeAndReturnsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_mediator);

        // Act
        var returnedServices = services.AddProcessesMediator();

        // Assert
        returnedServices.Should().BeSameAs(services);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediatorProcessDispatcher));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<MediatorProcessDispatcher>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IMediatorProcessDispatcher>();
        dispatcher.Should().NotBeNull();
        dispatcher.Should().BeOfType<MediatorProcessDispatcher>();
    }

    [Fact]
    public async Task DispatchEffectAsync_CommandEffect_WithUnsupportedPayload_InvokesOnUnrecognizedPayloadCallback()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("Unsupported");
        var effect = new ProcessEffect.Command(plain);

        ProcessId? capturedProcessId = null;
        ProcessEffect? capturedEffect = null;
        object? capturedPayload = null;

        dispatcher.OnUnrecognizedPayload = (pid, fx, payload) =>
        {
            capturedProcessId = pid;
            capturedEffect = fx;
            capturedPayload = payload;
        };

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
        capturedProcessId.Should().Be(processId);
        capturedEffect.Should().BeSameAs(effect);
        capturedPayload.Should().BeSameAs(plain);
    }

    [Fact]
    public async Task DispatchEffectAsync_EventEffect_WithUnsupportedPayload_InvokesOnUnrecognizedPayloadCallback()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainEvent");
        var effect = new ProcessEffect.Event(plain);

        ProcessEffect? capturedEffect = null;
        dispatcher.OnUnrecognizedPayload = (_, fx, _) => capturedEffect = fx;

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        capturedEffect.Should().BeSameAs(effect);
    }

    [Fact]
    public async Task DispatchEffectAsync_ScheduleTimeoutEffect_WithUnsupportedPayload_InvokesOnUnrecognizedPayloadCallback()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainTimeout");
        var effect = new ProcessEffect.ScheduleTimeout(TimeSpan.FromMinutes(1), plain);

        ProcessId? capturedProcessId = null;
        ProcessEffect? capturedEffect = null;
        object? capturedPayload = null;

        dispatcher.OnUnrecognizedPayload = (pid, fx, payload) =>
        {
            capturedProcessId = pid;
            capturedEffect = fx;
            capturedPayload = payload;
        };

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
        capturedProcessId.Should().Be(processId);
        capturedEffect.Should().BeSameAs(effect);
        capturedPayload.Should().BeSameAs(plain);
    }

    [Fact]
    public async Task DispatchEffectAsync_CompensationEffect_WithUnsupportedPayload_InvokesOnUnrecognizedPayloadCallback()
    {
        // Arrange
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("PlainCompensate");
        var effect = new ProcessEffect.Compensation(new CompensationAction("step", plain));

        ProcessId? capturedProcessId = null;
        ProcessEffect? capturedEffect = null;
        object? capturedPayload = null;

        dispatcher.OnUnrecognizedPayload = (pid, fx, payload) =>
        {
            capturedProcessId = pid;
            capturedEffect = fx;
            capturedPayload = payload;
        };

        // Act
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
        capturedProcessId.Should().Be(processId);
        capturedEffect.Should().BeSameAs(effect);
        capturedPayload.Should().BeSameAs(plain);
    }

    [Fact]
    public async Task DispatchEffectAsync_NoOnUnrecognizedPayload_UnsupportedPayload_DoesNotCallMediator()
    {
        // Arrange: dispatcher with no callback registered
        var dispatcher = new MediatorProcessDispatcher(_mediator);
        var processId = ProcessId.NewId();
        var plain = new SamplePlainObject("NoCallback");
        var effect = new ProcessEffect.Command(plain);

        // Act — should not throw even without callback registered
        await dispatcher.DispatchEffectAsync(effect, processId);

        // Assert
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default(INotification)!, default);
        await _mediator.DidNotReceiveWithAnyArgs().Send(default(ICommand<bool>)!, default);
    }
}





