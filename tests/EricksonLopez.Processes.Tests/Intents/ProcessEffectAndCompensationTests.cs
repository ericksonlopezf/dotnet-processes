// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Intents;

[Trait("Category", "Unit")]
public class ProcessEffectAndCompensationTests
{
    private sealed record TestPayload(string Name, int Value);
    private sealed record OtherPayload(double Amount);

    #region ProcessEffect.Command Tests

    [Fact]
    public void ProcessEffect_Command_ConstructorAndProperties_ShouldWork()
    {
        var payload = new TestPayload("Cmd", 1);
        var cmd = new ProcessEffect.Command(payload, "MyCommand");

        cmd.CommandPayload.Should().BeSameAs(payload);
        cmd.CommandType.Should().Be("MyCommand");
        cmd.GetPayload<TestPayload>().Should().BeSameAs(payload);

        cmd.TryGetPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        cmd.TryGetPayload<OtherPayload>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();

        var actCastFail = () => cmd.GetPayload<OtherPayload>();
        actCastFail.Should().Throw<InvalidCastException>();

        var actNull = () => new ProcessEffect.Command(null!, "Cmd");
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("CommandPayload");
    }

    [Fact]
    public void ProcessEffect_CreateCommand_ShouldHandleExplicitAndDefaultType()
    {
        var payload = new TestPayload("Cmd", 1);

        var explicitCmd = ProcessEffect.CreateCommand(payload, "CustomType");
        explicitCmd.CommandPayload.Should().BeSameAs(payload);
        explicitCmd.CommandType.Should().Be("CustomType");

        var defaultTypeCmd = ProcessEffect.CreateCommand(payload);
        defaultTypeCmd.CommandPayload.Should().BeSameAs(payload);
        defaultTypeCmd.CommandType.Should().Be(nameof(TestPayload));
    }

    #endregion

    #region ProcessEffect.Event Tests

    [Fact]
    public void ProcessEffect_Event_ConstructorAndProperties_ShouldWork()
    {
        var payload = new TestPayload("Evt", 2);
        var ev = new ProcessEffect.Event(payload, "MyEvent");

        ev.EventPayload.Should().BeSameAs(payload);
        ev.EventType.Should().Be("MyEvent");
        ev.GetPayload<TestPayload>().Should().BeSameAs(payload);

        ev.TryGetPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        ev.TryGetPayload<OtherPayload>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();

        var actCastFail = () => ev.GetPayload<OtherPayload>();
        actCastFail.Should().Throw<InvalidCastException>();

        var actNull = () => new ProcessEffect.Event(null!, "Evt");
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("EventPayload");
    }

    [Fact]
    public void ProcessEffect_CreateEvent_ShouldHandleExplicitAndDefaultType()
    {
        var payload = new TestPayload("Evt", 2);

        var explicitEvt = ProcessEffect.CreateEvent(payload, "CustomEventType");
        explicitEvt.EventPayload.Should().BeSameAs(payload);
        explicitEvt.EventType.Should().Be("CustomEventType");

        var defaultTypeEvt = ProcessEffect.CreateEvent(payload);
        defaultTypeEvt.EventPayload.Should().BeSameAs(payload);
        defaultTypeEvt.EventType.Should().Be(nameof(TestPayload));
    }

    #endregion

    #region ProcessEffect.ScheduleTimeout Tests

    [Fact]
    public void ProcessEffect_ScheduleTimeout_ConstructorAndProperties_ShouldWork()
    {
        var delay = TimeSpan.FromHours(1);
        var trigger = new TestPayload("Timeout", 3);
        var timeout = new ProcessEffect.ScheduleTimeout(delay, trigger, "MyTimeout");

        timeout.Delay.Should().Be(delay);
        timeout.TimeoutTrigger.Should().BeSameAs(trigger);
        timeout.TriggerType.Should().Be("MyTimeout");
        timeout.GetTrigger<TestPayload>().Should().BeSameAs(trigger);

        timeout.TryGetTrigger<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(trigger);

        timeout.TryGetTrigger<OtherPayload>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();

        var actCastFail = () => timeout.GetTrigger<OtherPayload>();
        actCastFail.Should().Throw<InvalidCastException>();

        var actNull = () => new ProcessEffect.ScheduleTimeout(delay, null!, "MyTimeout");
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("TimeoutTrigger");
    }

    [Fact]
    public void ProcessEffect_CreateTimeout_ShouldHandleExplicitAndDefaultType()
    {
        var delay = TimeSpan.FromMinutes(30);
        var trigger = new TestPayload("Timeout", 3);

        var explicitTimeout = ProcessEffect.CreateTimeout(delay, trigger, "CustomTriggerType");
        explicitTimeout.Delay.Should().Be(delay);
        explicitTimeout.TimeoutTrigger.Should().BeSameAs(trigger);
        explicitTimeout.TriggerType.Should().Be("CustomTriggerType");

        var defaultTypeTimeout = ProcessEffect.CreateTimeout(delay, trigger);
        defaultTypeTimeout.Delay.Should().Be(delay);
        defaultTypeTimeout.TimeoutTrigger.Should().BeSameAs(trigger);
        defaultTypeTimeout.TriggerType.Should().Be(nameof(TestPayload));
    }

    #endregion

    #region ProcessEffect.Compensation Tests

    [Fact]
    public void ProcessEffect_Compensation_ConstructorAndFactories_ShouldWork()
    {
        var action = new CompensationAction("CancelStep", new TestPayload("Comp", 4));
        var comp = new ProcessEffect.Compensation(action);

        comp.Action.Should().BeSameAs(action);

        var actNull = () => new ProcessEffect.Compensation(null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("Action");

        var factoryFromAction = ProcessEffect.CreateCompensation(action);
        factoryFromAction.Action.Should().BeSameAs(action);

        var payload = new TestPayload("Comp2", 5);
        var factoryTyped = ProcessEffect.CreateCompensation("RollbackStep", payload);
        factoryTyped.Action.StepName.Should().Be("RollbackStep");
        factoryTyped.Action.Payload.Should().BeSameAs(payload);
    }

    #endregion

    #region CompensationAction Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CompensationAction_ShouldThrowOnInvalidStepName(string? invalidStep)
    {
        var act = () => new CompensationAction(invalidStep!, new object());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompensationAction_ShouldThrowOnNullPayload()
    {
        var act = () => new CompensationAction("ValidStep", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("payload");
    }

    [Fact]
    public void CompensationAction_PropertiesAndExtract_ShouldWork()
    {
        var payload = new TestPayload("UndoOrder", 10);
        var action = CompensationAction.Create("UndoOrderStep", payload);

        action.StepName.Should().Be("UndoOrderStep");
        action.Payload.Should().BeSameAs(payload);
        action.ExtractPayload<TestPayload>().Should().BeSameAs(payload);

        action.TryExtractPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        action.TryExtractPayload<OtherPayload>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();

        var actCastFail = () => action.ExtractPayload<OtherPayload>();
        actCastFail.Should().Throw<InvalidCastException>();
    }

    #endregion

    #region CompensationStep Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CompensationStep_ShouldThrowOnInvalidStepName(string? invalidStep)
    {
        var act = () => new CompensationStep(invalidStep!, new object(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompensationStep_ShouldThrowOnNullPayload()
    {
        var act = () => new CompensationStep("ValidStep", null!, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentNullException>().WithParameterName("payload");
    }

    [Fact]
    public void CompensationStep_PropertiesAndExtract_ShouldWork()
    {
        var payload = new TestPayload("StepSnapshot", 20);
        var timestamp = DateTimeOffset.UtcNow;
        var step = CompensationStep.Create("RecordStep", payload, timestamp);

        step.StepName.Should().Be("RecordStep");
        step.Payload.Should().BeSameAs(payload);
        step.RecordedAt.Should().Be(timestamp);
        step.ExtractPayload<TestPayload>().Should().BeSameAs(payload);

        step.TryExtractPayload<TestPayload>(out var extracted).Should().BeTrue();
        extracted.Should().BeSameAs(payload);

        step.TryExtractPayload<OtherPayload>(out var wrongType).Should().BeFalse();
        wrongType.Should().BeNull();

        var actCastFail = () => step.ExtractPayload<OtherPayload>();
        actCastFail.Should().Throw<InvalidCastException>();
    }

    #endregion
}
