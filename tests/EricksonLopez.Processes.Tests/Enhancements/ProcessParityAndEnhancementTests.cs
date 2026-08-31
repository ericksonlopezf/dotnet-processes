// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.Enhancements;

[Trait("Category", "Unit")]
public class ProcessParityAndEnhancementTests
{
    private sealed record TestPayload(string Name, int Value);
    private sealed record TestState(string Data) : IProcessState;

    #region ProcessEffect Typed Factories & Accessors

    [Fact]
    public void ProcessEffect_CreateCommand_ShouldCreateTypedCommand()
    {
        var payload = new TestPayload("Cmd1", 100);
        var effect = ProcessEffect.CreateCommand(payload, "CustomCommandType");

        effect.CommandPayload.Should().BeSameAs(payload);
        effect.CommandType.Should().Be("CustomCommandType");
        effect.GetPayload<TestPayload>().Should().BeSameAs(payload);

        effect.TryGetPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        effect.TryGetPayload<string>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();
    }

    [Fact]
    public void ProcessEffect_CreateEvent_ShouldCreateTypedEvent()
    {
        var payload = new TestPayload("Evt1", 200);
        var effect = ProcessEffect.CreateEvent(payload);

        effect.EventPayload.Should().BeSameAs(payload);
        effect.EventType.Should().Be(nameof(TestPayload));
        effect.GetPayload<TestPayload>().Should().BeSameAs(payload);

        effect.TryGetPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        effect.TryGetPayload<int>(out var wrongType).Should().BeFalse();
        wrongType.Should().Be(0);
    }

    [Fact]
    public void ProcessEffect_CreateTimeout_ShouldCreateTypedTimeout()
    {
        var delay = TimeSpan.FromMinutes(10);
        var payload = new TestPayload("Trigger1", 300);
        var effect = ProcessEffect.CreateTimeout(delay, payload);

        effect.Delay.Should().Be(delay);
        effect.TimeoutTrigger.Should().BeSameAs(payload);
        effect.TriggerType.Should().Be(nameof(TestPayload));
        effect.GetTrigger<TestPayload>().Should().BeSameAs(payload);

        effect.TryGetTrigger<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        effect.TryGetTrigger<bool>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeFalse();
    }

    [Fact]
    public void ProcessEffect_CreateCompensation_ShouldCreateTypedCompensation()
    {
        var payload = new TestPayload("Comp1", 400);
        var effect = ProcessEffect.CreateCompensation("StepRollback", payload);

        effect.Action.StepName.Should().Be("StepRollback");
        effect.Action.Payload.Should().BeSameAs(payload);
        effect.Action.ExtractPayload<TestPayload>().Should().BeSameAs(payload);

        effect.Action.TryExtractPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        effect.Action.TryExtractPayload<string>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();
    }

    #endregion

    #region CompensationStep Typed Helpers

    [Fact]
    public void CompensationStep_Create_ShouldStoreAndExtractTypedPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new TestPayload("StepData", 500);
        var step = CompensationStep.Create("StepA", payload, now);

        step.StepName.Should().Be("StepA");
        step.Payload.Should().BeSameAs(payload);
        step.RecordedAt.Should().Be(now);
        step.ExtractPayload<TestPayload>().Should().BeSameAs(payload);

        step.TryExtractPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        step.TryExtractPayload<int>(out var wrongType).Should().BeFalse();
        wrongType.Should().Be(0);
    }

    #endregion

    #region CompositeCorrelationKey

    [Fact]
    public void CompositeCorrelationKey_ShouldCombinePartsDeterministically()
    {
        var key = CompositeCorrelationKey.From("tenant-1", "order-12345");
        key.Value.Should().Be("tenant-1:order-12345");
        key.ToString().Should().Be("tenant-1:order-12345");

        var correlationId1 = key.ToCorrelationId();
        var correlationId2 = CompositeCorrelationKey.From("tenant-1", "order-12345").ToCorrelationId();

        correlationId1.Should().Be(correlationId2);
        correlationId1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void CompositeCorrelationKey_DifferentParts_ProduceDifferentCorrelationIds()
    {
        var key1 = CompositeCorrelationKey.From("tenant-1", "order-123");
        var key2 = CompositeCorrelationKey.From("tenant-1", "order-456");

        key1.ToCorrelationId().Should().NotBe(key2.ToCorrelationId());
    }

    [Fact]
    public void CompositeCorrelationKey_ThreeAndFourParts_WorkCorrectly()
    {
        var key3 = CompositeCorrelationKey.From("tenant-1", "region-us", "order-999");
        key3.Value.Should().Be("tenant-1:region-us:order-999");

        var key4 = CompositeCorrelationKey.From("tenant-1", "region-us", "store-42", "order-999");
        key4.Value.Should().Be("tenant-1:region-us:store-42:order-999");
    }

    [Fact]
    public void CompositeCorrelationKey_Validation_ThrowsOnInvalidInputs()
    {
        var actNull = () => new CompositeCorrelationKey(null!);
        actNull.Should().ThrowExactly<ArgumentNullException>();

        var actEmpty = () => new CompositeCorrelationKey(Array.Empty<string>());
        actEmpty.Should().ThrowExactly<ArgumentException>();

        var actWhitespace = () => new CompositeCorrelationKey("valid", "   ");
        actWhitespace.Should().ThrowExactly<ArgumentException>();
    }

    #endregion

    #region ProcessRegistry RegisteredProcesses

    [Fact]
    public void ProcessRegistry_RegisteredProcesses_ShouldEnumerateAllRegisteredTypes()
    {
        var registry = new ProcessRegistry();
        registry.RegisteredProcesses.Should().BeEmpty();

        var type1 = ProcessType.From("order.fulfillment");
        var ver1 = ProcessVersion.From(1);
        var type2 = ProcessType.From("payment.saga");
        var ver2 = ProcessVersion.From(2);

        registry.Register(type1, ver1);
        registry.Register(type2, ver2);

        registry.RegisteredProcesses.Should().HaveCount(2);
        registry.RegisteredProcesses.Should().Contain((type1, ver1));
        registry.RegisteredProcesses.Should().Contain((type2, ver2));
    }

    #endregion

    #region Store GetByCorrelationId & Fault Injection

    [Fact]
    public async Task InMemoryProcessStore_GetByCorrelationIdAsync_ShouldFindInstance()
    {
        var store = new InMemoryProcessStore<TestState>();
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.NewId();
        var instance = ProcessInstance<TestState>.Create(
            id,
            ProcessType.From("test.process"),
            ProcessVersion.Initial,
            correlationId,
            new TestState("Initial"),
            DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        var retrieved = await store.GetByCorrelationIdAsync(correlationId);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(id);
        retrieved.CorrelationId.Should().Be(correlationId);

        var notFound = await store.GetByCorrelationIdAsync(CorrelationId.NewId());
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task FaultInjectingProcessStore_ExistsAsync_FaultInjection_Works()
    {
        var store = new FaultInjectingProcessStore<TestState>
        {
            ForcedExistsResult = true
        };

        var exists = await store.ExistsAsync(ProcessId.NewId());
        exists.Should().BeTrue();

        var expectedEx = new InvalidOperationException("Simulated Exists Failure");
        store.ForcedExistsResult = null;
        store.ExceptionToThrowOnExists = expectedEx;

        var act = async () => await store.ExistsAsync(ProcessId.NewId());
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Simulated Exists Failure");
    }

    [Fact]
    public async Task FaultInjectingProcessStore_GetByCorrelationIdAsync_FaultInjection_Works()
    {
        var forcedInstance = ProcessInstance<TestState>.Create(
            ProcessId.NewId(),
            ProcessType.From("forced.type"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new TestState("Forced"),
            DateTimeOffset.UtcNow);

        var store = new FaultInjectingProcessStore<TestState>
        {
            ForcedGetByCorrelationIdResult = forcedInstance
        };

        var result = await store.GetByCorrelationIdAsync(CorrelationId.NewId());
        result.Should().BeSameAs(forcedInstance);

        var expectedEx = new TimeoutException("Simulated Correlation Timeout");
        store.ForcedGetByCorrelationIdResult = null;
        store.ExceptionToThrowOnGetByCorrelationId = expectedEx;

        var act = async () => await store.GetByCorrelationIdAsync(CorrelationId.NewId());
        await act.Should().ThrowExactlyAsync<TimeoutException>()
            .WithMessage("Simulated Correlation Timeout");
    }

    #endregion
}
