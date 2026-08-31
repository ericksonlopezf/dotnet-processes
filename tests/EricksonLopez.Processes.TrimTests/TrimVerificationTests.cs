// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.DependencyInjection;
using EricksonLopez.Processes.SystemTextJson;
using EricksonLopez.Processes.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Processes.TrimTests;

// JsonSerializerContext must be a top-level partial class at namespace scope — not nested.
[JsonSerializable(typeof(TrimTestOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
internal sealed partial class TrimTestJsonContext : JsonSerializerContext { }

internal sealed record TrimTestOrderState(string OrderId, decimal Amount, bool IsComplete) : IProcessState;

internal sealed record TrimTestOrderEvent(Guid OrderId, decimal Amount);

/// <summary>
/// IL Trimming compatibility verification suite for EricksonLopez.Processes.
///
/// These tests run against the normally-built (debug) assembly but the project is configured
/// with EnableTrimAnalyzer=true (inherited from Directory.Build.props) which catches
/// trim-unsafe patterns at build time. If this project compiles with 0 warnings under
/// TreatWarningsAsErrors=true, the library is trim-safe.
///
/// Per ARCHITECTURAL_AUDIT §18 and Phase 8:
///   "TrimTests project — PublishTrimmed=true; CI: 0 warnings"
/// </summary>
[Trait("Category", "Trim")]
public class TrimVerificationTests
{
    private sealed class TrimTestCorrelation : IProcessCorrelation<TrimTestOrderEvent>
    {
        public ProcessId ExtractProcessId(TrimTestOrderEvent @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(TrimTestOrderEvent @event) => CorrelationId.From(@event.OrderId.ToString());
    }

    private sealed class TrimTestHandler : IProcessHandler<TrimTestOrderState, TrimTestOrderEvent>
    {
        public ProcessType Type => ProcessType.From("trim.test.order");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<TrimTestOrderState>> HandleAsync(
            TrimTestOrderState state, TrimTestOrderEvent @event, ProcessContext context)
        {
            var updated = state with { Amount = @event.Amount, IsComplete = true };
            return ValueTask.FromResult(
                ProcessTransitionResult<TrimTestOrderState>.Advance(updated, ProcessStatus.Completed));
        }
    }

    /// <summary>
    /// Verifies that all strongly-typed identifiers work correctly after trimming.
    /// </summary>
    [Fact]
    public void Identifiers_AreFullyTrimSafe()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("trim.safe.process");
        var version = ProcessVersion.Initial;
        var revision = Revision.Initial;
        var corr = CorrelationId.NewId();
        var cause = CausationId.NewId();
        var msg = MessageId.NewId();

        id.Value.Should().NotBe(Guid.Empty);
        type.Value.Should().Be("trim.safe.process");
        version.Value.Should().Be(1);
        revision.Value.Should().Be(1);
        corr.Value.Should().NotBeNullOrEmpty();
        cause.Value.Should().NotBeNullOrEmpty();
        msg.Value.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies ISpanParsable and ISpanFormattable on identifiers are trim-safe.
    /// </summary>
    [Fact]
    public void Identifiers_SpanParsableAndFormattable_AreTrimSafe()
    {
        var id = ProcessId.NewId();
        Span<char> buffer = stackalloc char[36];
        var formatted = id.TryFormat(buffer, out var written, default, null);
        formatted.Should().BeTrue();
        written.Should().Be(36);

        var reparsed = ProcessId.Parse(buffer, null);
        reparsed.Should().Be(id);
    }

    /// <summary>
    /// Verifies that ProcessInstance creation and state transition pipeline work trim-safe.
    /// </summary>
    [Fact]
    public void ProcessInstance_LifecycleTransitions_AreTrimSafe()
    {
        var id = ProcessId.NewId();
        var state = new TrimTestOrderState(id.ToString(), 99.99m, false);

        var instance = ProcessInstance<TrimTestOrderState>.Create(
            id,
            ProcessType.From("trim.test"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            state,
            DateTimeOffset.UtcNow);

        instance.Should().NotBeNull();
        instance.Status.Should().Be(ProcessStatus.Initialized);
        instance.Revision.Should().Be(Revision.Initial);

        var advanced = instance.Advance(
            state with { IsComplete = true },
            ProcessStatus.Completed,
            DateTimeOffset.UtcNow);

        advanced.Status.Should().Be(ProcessStatus.Completed);
        advanced.Revision.Value.Should().Be(2);
    }

    /// <summary>
    /// Verifies that the ProcessCoordinator full pipeline works trim-safe with InMemoryProcessStore.
    /// </summary>
    [Fact]
    public async Task ProcessCoordinator_FullPipeline_IsTrimSafe()
    {
        var store = new InMemoryProcessStore<TrimTestOrderState>();
        var coordinator = new ProcessCoordinator<TrimTestOrderState>(store);
        var handler = new TrimTestHandler();
        var correlation = new TrimTestCorrelation();

        var orderId = Guid.NewGuid();
        var @event = new TrimTestOrderEvent(orderId, 199.99m);

        var result = await coordinator.ExecuteAsync(
            handler,
            correlation,
            @event,
            initialStateFactory: e => new TrimTestOrderState(e.OrderId.ToString(), e.Amount, false),
            canInitiate: true);

        result.Should().NotBeNull();
        result.Instance.Status.Should().Be(ProcessStatus.Completed);
        result.Instance.State.IsComplete.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that SystemTextJson AOT serialization works trim-safe.
    /// </summary>
    [Fact]
    public void SystemTextJsonSerializer_SerializeDeserialize_IsTrimSafe()
    {
        var serializer = new SystemTextJsonProcessStateSerializer<TrimTestOrderState>(
            TrimTestJsonContext.Default.TrimTestOrderState);

        var state = new TrimTestOrderState("order-123", 299.99m, true);
        var bytes = serializer.Serialize(state);
        bytes.Should().NotBeNullOrEmpty();

        var deserialized = serializer.Deserialize(bytes);
        deserialized.OrderId.Should().Be("order-123");
        deserialized.Amount.Should().Be(299.99m);
        deserialized.IsComplete.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the DI extensions register services correctly without reflection scanning.
    /// </summary>
    [Fact]
    public void DependencyInjection_Extensions_AreTrimSafe()
    {
        var services = new ServiceCollection();
        var store = new InMemoryProcessStore<TrimTestOrderState>();

        services.AddSingleton<IProcessStore<TrimTestOrderState>>(store);
        services.AddProcessCoordinator<TrimTestOrderState>(options =>
        {
            options.MaxConcurrencyRetries = 3;
            options.InitialBackoffDelay = TimeSpan.FromMilliseconds(10);
        });

        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<ProcessCoordinator<TrimTestOrderState>>();
        coordinator.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the StateMigrationPipeline works trim-safe without reflection.
    /// </summary>
    [Fact]
    public void StateMigrationPipeline_IsTrimSafe()
    {
        var pipeline = ProcessStateMigrationPipeline
            .Create<TrimTestOrderState>(ProcessVersion.Initial)
            .AddStep(
                ProcessVersion.From(2),
                old => old with { OrderId = old.OrderId + "_migrated" })
            .Build<TrimTestOrderState>();

        var original = new TrimTestOrderState("order-trim-1", 50m, false);
        var migrated = pipeline.Migrate(original);

        migrated.OrderId.Should().EndWith("_migrated");
        migrated.Amount.Should().Be(50m);
    }
}





