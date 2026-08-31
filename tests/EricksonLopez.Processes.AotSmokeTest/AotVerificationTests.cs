// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.SystemTextJson;
using EricksonLopez.Processes.Testing;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace EricksonLopez.Processes.AotTests;

internal sealed record AotTestState(string Id, int Count) : IProcessState;
internal sealed record AotTestStateV2(string Id, int Count, string Status) : IProcessState;

[JsonSerializable(typeof(AotTestState))]
[JsonSerializable(typeof(AotTestStateV2))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(ProcessType))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(CausationId))]
[JsonSerializable(typeof(MessageId))]
internal sealed partial class AotJsonTestContext : JsonSerializerContext
{
}

[Trait("Category", "Aot")]
public class AotVerificationTests
{
    private sealed record TestIncrementEvent(Guid OrderId, int Delta);

    private sealed class TestAotCorrelation : IProcessCorrelation<TestIncrementEvent>
    {
        public ProcessId ExtractProcessId(TestIncrementEvent @event) => ProcessId.From(@event.OrderId);
        public CorrelationId ExtractCorrelationId(TestIncrementEvent @event) => CorrelationId.From(@event.OrderId.ToString());
    }

    private sealed class TestAotHandler : IProcessHandler<AotTestState, TestIncrementEvent>
    {
        public ProcessType Type => ProcessType.From("aot.process");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<AotTestState>> HandleAsync(
            AotTestState state,
            TestIncrementEvent eventMessage,
            ProcessContext context)
        {
            var updated = state with { Count = state.Count + eventMessage.Delta };
            var effect = new ProcessEffect.Command(new { Action = "AotEffect", Count = updated.Count });

            return ValueTask.FromResult(ProcessTransitionResult<AotTestState>.Advance(
                updated,
                ProcessStatus.Running,
                effects: [effect]));
        }
    }

    private sealed class TestAotMigrator : IProcessStateMigrator<AotTestState, AotTestStateV2>
    {
        public ProcessVersion FromVersion => ProcessVersion.From(1);
        public ProcessVersion ToVersion => ProcessVersion.From(2);

        public AotTestStateV2 Migrate(AotTestState sourceState)
        {
            ArgumentNullException.ThrowIfNull(sourceState);
            return new AotTestStateV2(sourceState.Id, sourceState.Count, "Migrated");
        }
    }

    [Fact]
    public void CorePrimitives_ShouldInstantiateWithoutReflection()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("test.process");
        var version = ProcessVersion.Initial;
        var revision = Revision.Initial;
        var corr = CorrelationId.NewId();
        var cause = CausationId.NewId();
        var msg = MessageId.NewId();

        var state = new AotTestState("test-1", 42);
        var instance = ProcessInstance<AotTestState>.Create(id, type, version, corr, state, DateTimeOffset.UtcNow);

        instance.Should().NotBeNull();
        instance.State.Count.Should().Be(42);
        revision.Value.Should().Be(1);
        cause.Value.Should().NotBeNullOrEmpty();
        msg.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SystemTextJson_AotSerializerContext_ShouldRoundtripStateWithoutReflection()
    {
        var typeInfo = AotJsonTestContext.Default.AotTestState;
        var originalState = new AotTestState("order-aot-999", 100);

        var serializer = new SystemTextJsonProcessStateSerializer<AotTestState>(typeInfo);
        var serialized = serializer.Serialize(originalState);

        serialized.Should().NotBeEmpty();

        var deserialized = serializer.Deserialize(serialized);
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("order-aot-999");
        deserialized.Count.Should().Be(100);
    }

    [Fact]
    public async Task ProcessCoordinator_WithInMemoryStore_ShouldExecuteWithoutReflection()
    {
        var store = new InMemoryProcessStore<AotTestState>();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var coordinator = new ProcessCoordinator<AotTestState>(store, timeProvider: fakeTime);
        var handler = new TestAotHandler();
        var correlation = new TestAotCorrelation();
        var orderId = Guid.NewGuid();

        var result = await coordinator.ExecuteAsync(
            handler: handler,
            correlation: correlation,
            eventMessage: new TestIncrementEvent(orderId, 10),
            initialStateFactory: e => new AotTestState(e.OrderId.ToString(), 0),
            canInitiate: true);

        result.IsSuccess.Should().BeTrue();
        result.Instance.State.Count.Should().Be(10);
        result.Instance.Status.Should().Be(ProcessStatus.Running);
        result.Effects.Should().HaveCount(1);

        var stored = await store.GetByIdAsync(ProcessId.From(orderId));
        stored.Should().NotBeNull();
        stored!.Revision.Value.Should().Be(2);
    }

    [Fact]
    public void ProcessStateMigrationPipeline_ShouldExecuteAotSafeMigrations()
    {
        var migrator = new TestAotMigrator();
        var pipeline = ProcessStateMigrationPipeline
            .Create<AotTestState>(ProcessVersion.From(1))
            .AddStep(migrator)
            .Build<AotTestState>();

        var v1 = new AotTestState("ORD-AOT", 55);
        var v2 = pipeline.Migrate(v1);

        v2.Id.Should().Be("ORD-AOT");
        v2.Count.Should().Be(55);
        v2.Status.Should().Be("Migrated");
    }

    [Fact]
    public void Identifiers_SpanFormattingAndParsing_ShouldBeDeterministicInAot()
    {
        var id = ProcessId.NewId();
        Span<char> buffer = stackalloc char[36];

        var formatted = id.TryFormat(buffer, out var written, default, null);
        formatted.Should().BeTrue();
        written.Should().Be(36);

        var parsed = ProcessId.Parse(buffer, null);
        parsed.Should().Be(id);
    }
}





