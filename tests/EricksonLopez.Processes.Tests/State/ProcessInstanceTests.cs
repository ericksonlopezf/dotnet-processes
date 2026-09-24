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

        // Transitioning back from a completed state to non-terminal is forbidden and throws InvalidProcessTransitionException
        var previouslyCompleted = instance.Advance(instance.State, ProcessStatus.Completed, nextTime);
        Action act = () => previouslyCompleted.Advance(instance.State, nonTerminalStatus, nextTime.AddMinutes(5));
        act.Should().Throw<InvalidProcessTransitionException>();
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

    [Fact]
    public void Constructor_WithRecordedCompensations_ShouldSetOrDefaultList()
    {
        var step = new CompensationStep("StepA", new { Val = 1 }, DateTimeOffset.UtcNow);

        var instanceWithSteps = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Initialized, Revision.Initial, CorrelationId.NewId(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null,
            new OrderState("c1", 10m, false), [step]);

        instanceWithSteps.RecordedCompensations.Should().ContainSingle().Which.Should().Be(step);

        var instanceNullSteps = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Initialized, Revision.Initial, CorrelationId.NewId(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null,
            new OrderState("c1", 10m, false), null);

        instanceNullSteps.RecordedCompensations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Advance_WithNewCompensations_WhenRecordedIsEmpty_ShouldAssignNewCompensations()
    {
        var now = DateTimeOffset.UtcNow;
        var instance = ProcessInstance<OrderState>.Create(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("c1", 10m, false), now);

        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        IReadOnlyList<CompensationStep> newSteps = [step1];
        var advanced = instance.Advance(instance.State, ProcessStatus.Running, now, newSteps);

        advanced.RecordedCompensations.Should().BeSameAs(newSteps);
    }

    [Fact]
    public void Advance_WithNewCompensations_WhenRecordedIsNotEmpty_ShouldConcatenate()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        var instance = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Running, Revision.Initial, CorrelationId.NewId(),
            now, now, null, new OrderState("c1", 10m, false), [step1]);

        var step2 = new CompensationStep("Step2", new { B = 2 }, now);
        var advanced = instance.Advance(instance.State, ProcessStatus.Running, now, [step2]);

        advanced.RecordedCompensations.Should().HaveCount(2);
        advanced.RecordedCompensations[0].Should().Be(step1);
        advanced.RecordedCompensations[1].Should().Be(step2);
    }

    [Fact]
    public void Advance_WithEmptyOrNullNewCompensations_ShouldRetainExistingRecordedCompensationsReference()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        var instance = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Running, Revision.Initial, CorrelationId.NewId(),
            now, now, null, new OrderState("c1", 10m, false), [step1]);

        var advNull = instance.Advance(instance.State, ProcessStatus.Running, now, null);
        advNull.RecordedCompensations.Should().BeSameAs(instance.RecordedCompensations);

        var advEmpty = instance.Advance(instance.State, ProcessStatus.Running, now, []);
        advEmpty.RecordedCompensations.Should().BeSameAs(instance.RecordedCompensations);
    }

    [Fact]
    public void Advance_WhenCompensationsExceedMax_ShouldThrowInvalidOperationException()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        var step2 = new CompensationStep("Step2", new { B = 2 }, now);

        var instance = ProcessInstance<OrderState>.Create(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("c1", 10m, false), now);

        // Boundary: count == maxCompensations should succeed
        var advancedExact = instance.Advance(instance.State, ProcessStatus.Running, now, [step1, step2], maxCompensations: 2);
        advancedExact.RecordedCompensations.Should().HaveCount(2);

        // Exceeded: count > maxCompensations should throw
        var step3 = new CompensationStep("Step3", new { C = 3 }, now);
        var actExceeded = () => advancedExact.Advance(instance.State, ProcessStatus.Running, now, [step3], maxCompensations: 2);
        actExceeded.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("*exceeded the maximum allowed compensation steps (2)*");
    }

    [Theory]
    [InlineData(ProcessStatus.Completed)]
    [InlineData(ProcessStatus.Compensated)]
    [InlineData(ProcessStatus.Failed)]
    public void AdvanceCompensation_WhenTerminalStatus_ShouldThrowInvalidProcessTransitionException(ProcessStatus terminalStatus)
    {
        var now = DateTimeOffset.UtcNow;
        var instance = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            terminalStatus, Revision.Initial, CorrelationId.NewId(),
            now, now, now, new OrderState("c1", 10m, false), []);

        var act = () => instance.AdvanceCompensation(instance.State, ProcessStatus.Compensating, now);
        var ex = act.Should().ThrowExactly<InvalidProcessTransitionException>().Which;
        ex.CurrentStatus.Should().Be(terminalStatus);
        ex.AttemptedStatus.Should().Be(ProcessStatus.Compensating);
        ex.Message.Should().Contain("Cannot advance compensation for process");
    }

    [Fact]
    public void AdvanceCompensation_ToTerminalStatus_ShouldSetCompletedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        var instance = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Compensating, Revision.Initial, CorrelationId.NewId(),
            now, now, null, new OrderState("c1", 10m, false), [step1]);

        var terminalCompensated = instance.AdvanceCompensation(instance.State, ProcessStatus.Compensated, now);
        terminalCompensated.Status.Should().Be(ProcessStatus.Compensated);
        terminalCompensated.CompletedAt.Should().Be(now);

        var terminalCompleted = instance.AdvanceCompensation(instance.State, ProcessStatus.Completed, now);
        terminalCompleted.Status.Should().Be(ProcessStatus.Completed);
        terminalCompleted.CompletedAt.Should().Be(now);

        var terminalFailed = instance.AdvanceCompensation(instance.State, ProcessStatus.Failed, now);
        terminalFailed.Status.Should().Be(ProcessStatus.Failed);
        terminalFailed.CompletedAt.Should().Be(now);

        var nonTerminal = instance.AdvanceCompensation(instance.State, ProcessStatus.Compensating, now);
        nonTerminal.Status.Should().Be(ProcessStatus.Compensating);
        nonTerminal.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void AdvanceCompensation_ShouldRemoveLastRecordedCompensationStep()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = new CompensationStep("Step1", new { A = 1 }, now);
        var step2 = new CompensationStep("Step2", new { B = 2 }, now);
        var instance = new ProcessInstance<OrderState>(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            ProcessStatus.Compensating, Revision.Initial, CorrelationId.NewId(),
            now, now, null, new OrderState("c1", 10m, false), [step1, step2]);

        var advanced = instance.AdvanceCompensation(instance.State, ProcessStatus.Compensating, now);
        advanced.RecordedCompensations.Should().ContainSingle().Which.Should().Be(step1);
    }

    [Fact]
    public void AdvanceCompensation_WhenRecordedCompensationsIsEmpty_ShouldReturnSameEmptyList()
    {
        var now = DateTimeOffset.UtcNow;
        var instance = ProcessInstance<OrderState>.Create(
            ProcessId.NewId(), ProcessType.From("test"), ProcessVersion.Initial,
            CorrelationId.NewId(), new OrderState("c1", 10m, false), now);

        var advanced = instance.AdvanceCompensation(instance.State, ProcessStatus.Compensating, now);
        advanced.RecordedCompensations.Should().BeSameAs(instance.RecordedCompensations);
    }
}





