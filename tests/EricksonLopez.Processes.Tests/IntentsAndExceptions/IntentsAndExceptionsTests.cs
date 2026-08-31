// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.IntentsAndExceptions;

[Trait("Category", "Unit")]
public class IntentsAndExceptionsTests
{
    #region ProcessEffect Tests

    [Fact]
    public void ProcessEffect_Command_ShouldConstructAndValidate()
    {
        var payload = new { OrderId = 123 };
        var cmd = new ProcessEffect.Command(payload, "CreateOrder");

        cmd.CommandPayload.Should().Be(payload);
        cmd.CommandType.Should().Be("CreateOrder");

        var actNull = () => new ProcessEffect.Command(null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("CommandPayload");
    }

    [Fact]
    public void ProcessEffect_Event_ShouldConstructAndValidate()
    {
        var payload = new { InvoiceId = 456 };
        var ev = new ProcessEffect.Event(payload, "InvoiceCertified");

        ev.EventPayload.Should().Be(payload);
        ev.EventType.Should().Be("InvoiceCertified");

        var actNull = () => new ProcessEffect.Event(null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("EventPayload");
    }

    [Fact]
    public void ProcessEffect_ScheduleTimeout_ShouldConstructAndValidate()
    {
        var trigger = new { TriggerId = 789 };
        var timeout = new ProcessEffect.ScheduleTimeout(TimeSpan.FromHours(24), trigger, "TimeoutTrigger");

        timeout.Delay.Should().Be(TimeSpan.FromHours(24));
        timeout.TimeoutTrigger.Should().Be(trigger);
        timeout.TriggerType.Should().Be("TimeoutTrigger");

        var actNull = () => new ProcessEffect.ScheduleTimeout(TimeSpan.FromMinutes(5), null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("TimeoutTrigger");
    }

    [Fact]
    public void ProcessEffect_Compensation_ShouldConstructAndValidate()
    {
        var action = new CompensationAction("CancelPayment", new { PaymentId = 1 });
        var compensation = new ProcessEffect.Compensation(action);

        compensation.Action.Should().Be(action);

        var actNull = () => new ProcessEffect.Compensation(null!);
        actNull.Should().Throw<ArgumentNullException>().WithParameterName("Action");
    }

    #endregion

    #region CompensationStep and CompensationAction Tests

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
    public void CompensationAction_ShouldSetPropertiesCorrectly()
    {
        var payload = new { Amount = 100m };
        var action = new CompensationAction("Refund", payload);

        action.StepName.Should().Be("Refund");
        action.Payload.Should().Be(payload);
    }

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
    public void CompensationStep_ShouldSetPropertiesCorrectly()
    {
        var payload = new { Sku = "SKU-9" };
        var now = DateTimeOffset.UtcNow;
        var step = new CompensationStep("ReleaseInventory", payload, now);

        step.StepName.Should().Be("ReleaseInventory");
        step.Payload.Should().Be(payload);
        step.RecordedAt.Should().Be(now);
    }

    #endregion

    #region ProcessSaveResult Tests

    [Fact]
    public void ProcessSaveResult_EnumValues_ShouldMatchContract()
    {
        ((int)ProcessSaveResult.Success).Should().Be(0);
        ((int)ProcessSaveResult.ConcurrencyConflict).Should().Be(1);
        ((int)ProcessSaveResult.NotFound).Should().Be(2);
        ((int)ProcessSaveResult.PersistenceError).Should().Be(3);
    }

    #endregion

    #region Exceptions Tests

    [Fact]
    public void ProcessException_Constructors_ShouldSetProperties()
    {
        var exDefault = new ProcessException();
        exDefault.Message.Should().NotBeNullOrEmpty();
        exDefault.ProcessId.Should().BeNull();

        var exMsg = new ProcessException("custom message");
        exMsg.Message.Should().Be("custom message");

        var inner = new InvalidOperationException("inner");
        var exInner = new ProcessException("custom with inner", inner);
        exInner.InnerException.Should().Be(inner);

        var pid = ProcessId.NewId();
        var exPid = new ProcessException("with pid", pid, inner);
        exPid.ProcessId.Should().Be(pid);
        exPid.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ProcessNotFoundException_Constructors_ShouldSetProperties()
    {
        var exDefault = new ProcessNotFoundException();
        exDefault.Message.Should().NotBeNullOrEmpty();

        var exMsg = new ProcessNotFoundException("not found msg");
        exMsg.Message.Should().Be("not found msg");

        var inner = new InvalidOperationException("inner");
        var exInner = new ProcessNotFoundException("msg", inner);
        exInner.InnerException.Should().Be(inner);

        var pid = ProcessId.NewId();
        var exPid = new ProcessNotFoundException(pid);
        exPid.ProcessId.Should().Be(pid);
        exPid.Message.Should().Contain(pid.ToString());
    }

    [Fact]
    public void ConcurrencyConflictException_Constructors_ShouldSetProperties()
    {
        var exDefault = new ConcurrencyConflictException();
        exDefault.Message.Should().NotBeNullOrEmpty();

        var exMsg = new ConcurrencyConflictException("concurrency error");
        exMsg.Message.Should().Be("concurrency error");

        var inner = new InvalidOperationException("inner");
        var exInner = new ConcurrencyConflictException("msg", inner);
        exInner.InnerException.Should().Be(inner);

        var pid = ProcessId.NewId();
        var expectedRev = Revision.From(5);
        var exPid = new ConcurrencyConflictException(pid, expectedRev);
        exPid.ProcessId.Should().Be(pid);
        exPid.ExpectedRevision.Should().Be(expectedRev);
        exPid.Message.Should().Contain(pid.ToString());
        exPid.Message.Should().Contain("5");
    }

    [Fact]
    public void InvalidProcessTransitionException_Constructors_ShouldSetProperties()
    {
        var exDefault = new InvalidProcessTransitionException();
        exDefault.Message.Should().NotBeNullOrEmpty();

        var exMsg = new InvalidProcessTransitionException("invalid transition");
        exMsg.Message.Should().Be("invalid transition");

        var inner = new InvalidOperationException("inner");
        var exInner = new InvalidProcessTransitionException("msg", inner);
        exInner.InnerException.Should().Be(inner);

        var pid = ProcessId.NewId();
        var exPid = new InvalidProcessTransitionException(pid, ProcessStatus.Completed, ProcessStatus.Running, "Cannot restart completed process");
        exPid.ProcessId.Should().Be(pid);
        exPid.CurrentStatus.Should().Be(ProcessStatus.Completed);
        exPid.AttemptedStatus.Should().Be(ProcessStatus.Running);
        exPid.Message.Should().Contain("Cannot restart completed process");
    }

    [Fact]
    public void CompensationFailedException_Constructors_ShouldSetProperties()
    {
        var exDefault = new CompensationFailedException();
        exDefault.Message.Should().NotBeNullOrEmpty();
        exDefault.StepName.Should().BeEmpty();

        var exMsg = new CompensationFailedException("compensation failed");
        exMsg.Message.Should().Be("compensation failed");

        var inner = new InvalidOperationException("inner");
        var exInner = new CompensationFailedException("msg", inner);
        exInner.InnerException.Should().Be(inner);

        var pid = ProcessId.NewId();
        var exPid = new CompensationFailedException(pid, "ChargePayment", "Payment gateway unavailable", inner);
        exPid.ProcessId.Should().Be(pid);
        exPid.StepName.Should().Be("ChargePayment");
        exPid.InnerException.Should().Be(inner);
        exPid.Message.Should().Contain("ChargePayment");
    }

    #endregion

    #region Attributes Tests

    [Fact]
    public void ProcessDefinitionAttribute_ShouldSetProperties()
    {
        var attr = new ProcessDefinitionAttribute("order.fulfillment", 2);
        attr.ProcessType.Should().Be("order.fulfillment");
        attr.Version.Should().Be(2);

        var actNull = () => new ProcessDefinitionAttribute(null!);
        actNull.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SagaDefinitionAttribute_ShouldSetProperties()
    {
        var attr = new SagaDefinitionAttribute("travel.booking", 3);
        attr.ProcessType.Should().Be("travel.booking");
        attr.Version.Should().Be(3);

        var actNull = () => new SagaDefinitionAttribute(null!);
        actNull.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessTypeAttribute_ShouldSetProperties()
    {
        var attr = new ProcessTypeAttribute("invoice.audit");
        attr.ProcessType.Should().Be("invoice.audit");

        var actNull = () => new ProcessTypeAttribute(null!);
        actNull.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessHandlerAttribute_ShouldSetProperties()
    {
        var attrDefault = new ProcessHandlerAttribute();
        attrDefault.CanInitiate.Should().BeFalse();

        var attrInitiate = new ProcessHandlerAttribute(canInitiate: true);
        attrInitiate.CanInitiate.Should().BeTrue();
    }

    #endregion
}





