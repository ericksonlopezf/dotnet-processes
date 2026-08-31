// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.State;

[Trait("Category", "Unit")]
public class ProcessStateAndInstanceTests
{
    private sealed record V1OrderState(string OrderId, decimal Amount) : IProcessState;
    private sealed record V2OrderState(string OrderId, decimal Amount, string Currency) : IProcessState;

    private sealed class OrderStateMigratorV1ToV2 : IProcessStateMigrator<V1OrderState, V2OrderState>
    {
        public ProcessVersion FromVersion => ProcessVersion.From(1);
        public ProcessVersion ToVersion => ProcessVersion.From(2);

        public V2OrderState Migrate(V1OrderState sourceState)
        {
            ArgumentNullException.ThrowIfNull(sourceState);
            return new V2OrderState(sourceState.OrderId, sourceState.Amount, "USD");
        }
    }

    private sealed class Utf8StringStateSerializer : IProcessStateSerializer<V1OrderState>
    {
        public byte[] Serialize(V1OrderState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            return Encoding.UTF8.GetBytes($"{state.OrderId}:{state.Amount}");
        }

        public V1OrderState Deserialize(ReadOnlySpan<byte> data)
        {
            var text = Encoding.UTF8.GetString(data);
            var parts = text.Split(':');
            return new V1OrderState(parts[0], decimal.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    #region ProcessInstance Tests

    [Fact]
    public void ProcessInstance_Constructor_ShouldValidateArgumentsAndSetProperties()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.billing");
        var version = ProcessVersion.From(1);
        var correlationId = CorrelationId.NewId();
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);
        var completedAt = createdAt.AddMinutes(2);
        var state = new V1OrderState("ord-1", 150m);

        var instance = new ProcessInstance<V1OrderState>(
            id: id,
            type: type,
            version: version,
            status: ProcessStatus.Completed,
            revision: Revision.From(5),
            correlationId: correlationId,
            createdAt: createdAt,
            updatedAt: updatedAt,
            completedAt: completedAt,
            state: state);

        instance.Id.Should().Be(id);
        instance.Type.Should().Be(type);
        instance.Version.Should().Be(version);
        instance.Status.Should().Be(ProcessStatus.Completed);
        instance.Revision.Value.Should().Be(5);
        instance.CorrelationId.Should().Be(correlationId);
        instance.CreatedAt.Should().Be(createdAt);
        instance.UpdatedAt.Should().Be(updatedAt);
        instance.CompletedAt.Should().Be(completedAt);
        instance.State.Should().BeSameAs(state);

        var actNull = () => new ProcessInstance<V1OrderState>(
            id, type, version, ProcessStatus.Running, Revision.Initial, correlationId,
            createdAt, updatedAt, null, null!);

        actNull.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void ProcessInstance_Create_ShouldInitializeWithExpectedDefaults()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.billing");
        var version = ProcessVersion.From(1);
        var correlationId = CorrelationId.NewId();
        var state = new V1OrderState("ord-1", 150m);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<V1OrderState>.Create(
            id, type, version, correlationId, state, now);

        instance.Id.Should().Be(id);
        instance.Type.Should().Be(type);
        instance.Version.Should().Be(version);
        instance.Status.Should().Be(ProcessStatus.Initialized);
        instance.Revision.Should().Be(Revision.Initial);
        instance.CorrelationId.Should().Be(correlationId);
        instance.CreatedAt.Should().Be(now);
        instance.UpdatedAt.Should().Be(now);
        instance.CompletedAt.Should().BeNull();
        instance.State.Should().BeSameAs(state);
    }

    [Theory]
    [InlineData(ProcessStatus.Completed)]
    [InlineData(ProcessStatus.Compensated)]
    [InlineData(ProcessStatus.Failed)]
    public void ProcessInstance_Advance_ToTerminalStatus_ShouldSetCompletedAtAndIncrementRevision(ProcessStatus terminalStatus)
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.billing");
        var version = ProcessVersion.From(1);
        var correlationId = CorrelationId.NewId();
        var state = new V1OrderState("ord-1", 150m);
        var createdTime = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<V1OrderState>.Create(id, type, version, correlationId, state, createdTime);

        var advanceTime = createdTime.AddMinutes(5);
        var newState = new V1OrderState("ord-1", 200m);

        var advanced = instance.Advance(newState, terminalStatus, advanceTime);

        advanced.Id.Should().Be(id);
        advanced.Type.Should().Be(type);
        advanced.Version.Should().Be(version);
        advanced.Status.Should().Be(terminalStatus);
        advanced.Revision.Value.Should().Be(Revision.Initial.Value + 1);
        advanced.CorrelationId.Should().Be(correlationId);
        advanced.CreatedAt.Should().Be(createdTime);
        advanced.UpdatedAt.Should().Be(advanceTime);
        advanced.CompletedAt.Should().Be(advanceTime);
        advanced.State.Should().BeSameAs(newState);
    }

    [Theory]
    [InlineData(ProcessStatus.Running)]
    [InlineData(ProcessStatus.Suspended)]
    [InlineData(ProcessStatus.Compensating)]
    [InlineData(ProcessStatus.Initialized)]
    public void ProcessInstance_Advance_ToNonTerminalStatus_ShouldKeepCompletedAtNull(ProcessStatus nonTerminalStatus)
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.billing");
        var version = ProcessVersion.From(1);
        var correlationId = CorrelationId.NewId();
        var state = new V1OrderState("ord-1", 150m);
        var createdTime = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<V1OrderState>.Create(id, type, version, correlationId, state, createdTime);

        var advanceTime = createdTime.AddMinutes(3);
        var newState = new V1OrderState("ord-1", 175m);

        var advanced = instance.Advance(newState, nonTerminalStatus, advanceTime);

        advanced.Status.Should().Be(nonTerminalStatus);
        advanced.CompletedAt.Should().BeNull();
        advanced.Revision.Value.Should().Be(Revision.Initial.Value + 1);
        advanced.UpdatedAt.Should().Be(advanceTime);
    }

    [Fact]
    public void ProcessInstance_RecordEquality_ShouldWork()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.billing");
        var version = ProcessVersion.From(1);
        var correlationId = CorrelationId.NewId();
        var state = new V1OrderState("ord-1", 150m);
        var now = DateTimeOffset.UtcNow;

        var inst1 = ProcessInstance<V1OrderState>.Create(id, type, version, correlationId, state, now);
        var inst2 = ProcessInstance<V1OrderState>.Create(id, type, version, correlationId, state, now);
        var inst3 = inst1.Advance(state, ProcessStatus.Running, now.AddSeconds(1));

        (inst1 == inst2).Should().BeTrue();
        (inst1 != inst3).Should().BeTrue();
        inst1.Equals(inst2).Should().BeTrue();
        inst1.Equals((object)inst2).Should().BeTrue();
        inst1.GetHashCode().Should().Be(inst2.GetHashCode());
    }

    #endregion

    #region Migrator & Serializer & Status Tests

    [Fact]
    public void IProcessStateMigrator_Implementation_ShouldMigrateSuccessfully()
    {
        var migrator = new OrderStateMigratorV1ToV2();
        migrator.FromVersion.Value.Should().Be(1);
        migrator.ToVersion.Value.Should().Be(2);

        var v1 = new V1OrderState("order-99", 500m);
        var v2 = migrator.Migrate(v1);

        v2.OrderId.Should().Be("order-99");
        v2.Amount.Should().Be(500m);
        v2.Currency.Should().Be("USD");
    }

    [Fact]
    public void IProcessStateSerializer_Implementation_ShouldSerializeAndDeserialize()
    {
        var serializer = new Utf8StringStateSerializer();
        var state = new V1OrderState("order-100", 350.50m);

        var bytes = serializer.Serialize(state);
        bytes.Should().NotBeEmpty();

        var deserialized = serializer.Deserialize(bytes);
        deserialized.OrderId.Should().Be("order-100");
        deserialized.Amount.Should().Be(350.50m);
    }

    [Fact]
    public void ProcessStatus_AllValues_ShouldMatchContract()
    {
        ((int)ProcessStatus.Initialized).Should().Be(0);
        ((int)ProcessStatus.Running).Should().Be(1);
        ((int)ProcessStatus.Suspended).Should().Be(2);
        ((int)ProcessStatus.Completed).Should().Be(3);
        ((int)ProcessStatus.Compensating).Should().Be(4);
        ((int)ProcessStatus.Compensated).Should().Be(5);
        ((int)ProcessStatus.Failed).Should().Be(6);
    }

    #endregion
}
