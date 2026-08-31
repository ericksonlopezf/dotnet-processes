// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.State;

[Trait("Category", "Unit")]
public class ProcessInstanceTests
{
    private sealed record OrderState(string CustomerId, decimal Amount, bool IsPaid) : IProcessState;

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenStateIsNull()
    {
        var act = () => new ProcessInstance<OrderState>(
            id: ProcessId.NewId(),
            type: ProcessType.From("order.fulfillment"),
            version: ProcessVersion.Initial,
            status: ProcessStatus.Initialized,
            revision: Revision.Initial,
            correlationId: CorrelationId.NewId(),
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            completedAt: null,
            state: null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void Create_ShouldInitializeInstanceWithExpectedMetadata()
    {
        var id = ProcessId.NewId();
        var type = ProcessType.From("order.fulfillment");
        var version = ProcessVersion.Initial;
        var correlationId = CorrelationId.NewId();
        var initialState = new OrderState("cust-123", 99.99m, false);
        var now = DateTimeOffset.UtcNow;

        var instance = ProcessInstance<OrderState>.Create(
            id: id,
            type: type,
            version: version,
            correlationId: correlationId,
            initialState: initialState,
            now: now);

        instance.Id.Should().Be(id);
        instance.Type.Should().Be(type);
        instance.Version.Should().Be(version);
        instance.Status.Should().Be(ProcessStatus.Initialized);
        instance.Revision.Should().Be(Revision.Initial);
        instance.CorrelationId.Should().Be(correlationId);
        instance.CreatedAt.Should().Be(now);
        instance.UpdatedAt.Should().Be(now);
        instance.CompletedAt.Should().BeNull();
        instance.State.Should().Be(initialState);
    }

    [Fact]
    public void Advance_ShouldUpdateStateRevisionAndTimestamp()
    {
        var id = ProcessId.NewId();
        var now = DateTimeOffset.UtcNow;
        var instance = ProcessInstance<OrderState>.Create(
            id, ProcessType.From("order.fulfillment"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("cust-123", 99.99m, false), now);

        var nextTime = now.AddMinutes(5);
        var updatedState = instance.State with { IsPaid = true };

        var advanced = instance.Advance(updatedState, ProcessStatus.Running, nextTime);

        advanced.Revision.Value.Should().Be(2);
        advanced.Status.Should().Be(ProcessStatus.Running);
        advanced.State.IsPaid.Should().BeTrue();
        advanced.UpdatedAt.Should().Be(nextTime);
        advanced.CompletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(ProcessStatus.Completed)]
    [InlineData(ProcessStatus.Compensated)]
    [InlineData(ProcessStatus.Failed)]
    public void Advance_ToTerminalStatuses_ShouldSetCompletedAt(ProcessStatus terminalStatus)
    {
        var id = ProcessId.NewId();
        var now = DateTimeOffset.UtcNow;
        var instance = ProcessInstance<OrderState>.Create(
            id, ProcessType.From("order.fulfillment"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("cust-123", 99.99m, false), now);

        var completedTime = now.AddMinutes(10);
        var completed = instance.Advance(instance.State, terminalStatus, completedTime);

        completed.Status.Should().Be(terminalStatus);
        completed.CompletedAt.Should().Be(completedTime);
    }

    [Theory]
    [InlineData(ProcessStatus.Running)]
    [InlineData(ProcessStatus.Suspended)]
    [InlineData(ProcessStatus.Compensating)]
    [InlineData(ProcessStatus.Initialized)]
    public void Advance_ToNonTerminalStatuses_ShouldKeepCompletedAtNull(ProcessStatus nonTerminalStatus)
    {
        var id = ProcessId.NewId();
        var now = DateTimeOffset.UtcNow;
        var instance = ProcessInstance<OrderState>.Create(
            id, ProcessType.From("order.fulfillment"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("cust-123", 99.99m, false), now);

        var nextTime = now.AddMinutes(10);
        var advanced = instance.Advance(instance.State, nonTerminalStatus, nextTime);

        advanced.Status.Should().Be(nonTerminalStatus);
        advanced.CompletedAt.Should().BeNull();

        // Also test transitioning back from a completed state to non-terminal resets completedAt to null
        var previouslyCompleted = instance.Advance(instance.State, ProcessStatus.Completed, nextTime);
        var resumed = previouslyCompleted.Advance(instance.State, nonTerminalStatus, nextTime.AddMinutes(5));
        resumed.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void ProcessStatus_EnumValues_ShouldMatchDefinitions()
    {
        ((int)ProcessStatus.Initialized).Should().Be(0);
        ((int)ProcessStatus.Running).Should().Be(1);
        ((int)ProcessStatus.Suspended).Should().Be(2);
        ((int)ProcessStatus.Completed).Should().Be(3);
        ((int)ProcessStatus.Compensating).Should().Be(4);
        ((int)ProcessStatus.Compensated).Should().Be(5);
        ((int)ProcessStatus.Failed).Should().Be(6);
    }

    [Fact]
    public void ProcessDefinitionAttribute_ShouldSetPropertiesAndValidateInput()
    {
        var attrDefault = new ProcessDefinitionAttribute("order.fulfillment");
        attrDefault.ProcessType.Should().Be("order.fulfillment");
        attrDefault.Version.Should().Be(1);

        var attrCustom = new ProcessDefinitionAttribute("order.fulfillment", 3);
        attrCustom.ProcessType.Should().Be("order.fulfillment");
        attrCustom.Version.Should().Be(3);

        var actNull = () => new ProcessDefinitionAttribute(null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => new ProcessDefinitionAttribute("   ");
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SagaDefinitionAttribute_ShouldSetPropertiesAndValidateInput()
    {
        var attrDefault = new SagaDefinitionAttribute("travel.saga");
        attrDefault.ProcessType.Should().Be("travel.saga");
        attrDefault.Version.Should().Be(1);

        var attrCustom = new SagaDefinitionAttribute("travel.saga", 5);
        attrCustom.ProcessType.Should().Be("travel.saga");
        attrCustom.Version.Should().Be(5);

        var actNull = () => new SagaDefinitionAttribute(null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => new SagaDefinitionAttribute("   ");
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessTypeAttribute_ShouldSetPropertiesAndValidateInput()
    {
        var attr = new ProcessTypeAttribute("payment.type");
        attr.ProcessType.Should().Be("payment.type");

        var actNull = () => new ProcessTypeAttribute(null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => new ProcessTypeAttribute("   ");
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessHandlerAttribute_ShouldSetCanInitiate()
    {
        var attrDefault = new ProcessHandlerAttribute();
        attrDefault.CanInitiate.Should().BeFalse();

        var attrExplicitFalse = new ProcessHandlerAttribute(false);
        attrExplicitFalse.CanInitiate.Should().BeFalse();

        var attrExplicitTrue = new ProcessHandlerAttribute(true);
        attrExplicitTrue.CanInitiate.Should().BeTrue();
    }
}





