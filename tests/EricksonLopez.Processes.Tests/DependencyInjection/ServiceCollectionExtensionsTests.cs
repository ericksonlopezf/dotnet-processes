// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.DependencyInjection;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    public sealed record TestDiState(string Id) : IProcessState;

    private sealed record TestEvent(Guid ProcessId);

    private sealed class TestCorrelation : IProcessCorrelation<TestEvent>
    {
        public ProcessId ExtractProcessId(TestEvent @event) => ProcessId.From(@event.ProcessId);
        public CorrelationId ExtractCorrelationId(TestEvent @event) => CorrelationId.From(@event.ProcessId.ToString());
    }

    private sealed class TestHandler : IProcessHandler<TestDiState, TestEvent>
    {
        public ProcessType Type => ProcessType.From("di.test");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<TestDiState>> HandleAsync(
            TestDiState state, TestEvent eventMessage, ProcessContext context) =>
            ValueTask.FromResult(ProcessTransitionResult<TestDiState>.Advance(state));
    }

    private sealed class FakeProcessStore : IProcessStore<TestDiState>
    {
        public ValueTask<ProcessInstance<TestDiState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ProcessInstance<TestDiState>?>(null);

        public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TestDiState> instance, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ProcessSaveResult.Success);

        public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);
    }

    [Fact]
    public void AddProcesses_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;
        var act = () => services.AddProcesses();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddProcesses_ShouldRegisterRequiredCoreServices()
    {
        var services = new ServiceCollection();
        services.AddProcesses();

        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IProcessRegistry>();
        registry.Should().NotBeNull();
        registry.Should().BeOfType<ProcessRegistry>();

        var concreteRegistry = provider.GetService<ProcessRegistry>();
        concreteRegistry.Should().NotBeNull();
        concreteRegistry.Should().BeSameAs(registry);

        var timeProvider = provider.GetService<TimeProvider>();
        timeProvider.Should().NotBeNull();
        timeProvider.Should().Be(TimeProvider.System);
    }

    [Fact]
    public void AddProcessCoordinator_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;
        var act = () => services.AddProcessCoordinator<TestDiState>();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddProcessCoordinator_ShouldResolveCoordinatorWithConfiguredDependencies()
    {
        var services = new ServiceCollection();
        services.AddProcesses();
        services.AddSingleton<IProcessStore<TestDiState>, FakeProcessStore>();
        var configured = false;
        services.AddProcessCoordinator<TestDiState>(configureOptions: opt =>
        {
            configured = true;
            opt.MaxConcurrencyRetries = 5;
        });

        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetService<ProcessCoordinator<TestDiState>>();
        coordinator.Should().NotBeNull();
        configured.Should().BeTrue();
    }

    [Fact]
    public void AddProcessCoordinator_WithDefaultRetries_ShouldResolveCoordinator()
    {
        var services = new ServiceCollection();
        services.AddProcesses();
        services.AddSingleton<IProcessStore<TestDiState>, FakeProcessStore>();
        services.AddProcessCoordinator<TestDiState>();

        var provider = services.BuildServiceProvider();

        var coordinator = provider.GetService<ProcessCoordinator<TestDiState>>();
        coordinator.Should().NotBeNull();
    }

    [Fact]
    public async Task AddProcessCoordinator_ShouldUseRegisteredCustomTimeProvider()
    {
        var services = new ServiceCollection();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        services.AddSingleton<TimeProvider>(fakeTime);
        services.AddSingleton<IProcessStore<TestDiState>, FakeProcessStore>();
        services.AddProcessCoordinator<TestDiState>();

        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<ProcessCoordinator<TestDiState>>();

        var result = await coordinator.ExecuteAsync(
            handler: new TestHandler(),
            correlation: new TestCorrelation(),
            eventMessage: new TestEvent(Guid.NewGuid()),
            initialStateFactory: e => new TestDiState(e.ProcessId.ToString()),
            canInitiate: true);

        result.Instance.CreatedAt.Year.Should().Be(2040);
    }

    [Fact]
    public async Task AddProcessCoordinator_WithoutRegisteredTimeProvider_ShouldFallbackToSystemTimeProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProcessStore<TestDiState>, FakeProcessStore>();
        services.AddProcessCoordinator<TestDiState>();

        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<ProcessCoordinator<TestDiState>>();

        var result = await coordinator.ExecuteAsync(
            handler: new TestHandler(),
            correlation: new TestCorrelation(),
            eventMessage: new TestEvent(Guid.NewGuid()),
            initialStateFactory: e => new TestDiState(e.ProcessId.ToString()),
            canInitiate: true);

        result.Instance.CreatedAt.Year.Should().Be(DateTimeOffset.UtcNow.Year);
    }
}







