// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.Results;

[Trait("Category", "Unit")]
public class ResultsAndContextTests
{
    public sealed record TestState(string Name, int Value) : IProcessState;
    public sealed record TestEvent(string Message);

    #region ProcessTransitionResult Tests

    [Fact]
    public void ProcessTransitionResult_Constructor_ShouldThrowOnNullState()
    {
        var act = () => new ProcessTransitionResult<TestState>(null!, ProcessStatus.Running);
        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void ProcessTransitionResult_Constructor_WithExplicitValues_ShouldSetAllProperties()
    {
        var state = new TestState("explicit", 1);
        var effect = new ProcessEffect.Command(new { Cmd = 1 });
        var compensation = new CompensationStep("StepA", new { Payload = "a" }, DateTimeOffset.UtcNow);

        var result = new ProcessTransitionResult<TestState>(
            state: state,
            status: ProcessStatus.Failed,
            effects: [effect],
            recordedCompensations: [compensation],
            failureReason: "Explicit reason");

        result.State.Should().Be(state);
        result.Status.Should().Be(ProcessStatus.Failed);
        result.Effects.Should().ContainSingle().Which.Should().Be(effect);
        result.RecordedCompensations.Should().ContainSingle().Which.Should().Be(compensation);
        result.FailureReason.Should().Be("Explicit reason");
    }

    [Fact]
    public void ProcessTransitionResult_Constructor_WithNullCollections_ShouldDefaultToEmpty()
    {
        var state = new TestState("defaults", 2);

        var result = new ProcessTransitionResult<TestState>(
            state: state,
            status: ProcessStatus.Running,
            effects: null,
            recordedCompensations: null,
            failureReason: null);

        result.State.Should().Be(state);
        result.Status.Should().Be(ProcessStatus.Running);
        result.Effects.Should().BeEmpty();
        result.RecordedCompensations.Should().BeEmpty();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public void ProcessTransitionResult_Advance_ShouldSetProperties()
    {
        var state = new TestState("test", 10);
        var effect = new ProcessEffect.Command(new { Foo = "bar" });
        var step = new CompensationStep("Step1", new { A = 1 }, DateTimeOffset.UtcNow);

        var result = ProcessTransitionResult<TestState>.Advance(
            state,
            ProcessStatus.Running,
            effects: [effect],
            recordedCompensations: [step]);

        result.State.Should().Be(state);
        result.Status.Should().Be(ProcessStatus.Running);
        result.Effects.Should().ContainSingle().Which.Should().Be(effect);
        result.RecordedCompensations.Should().ContainSingle().Which.Should().Be(step);
        result.FailureReason.Should().BeNull();

        var resultDefaults = ProcessTransitionResult<TestState>.Advance(state);
        resultDefaults.Status.Should().Be(ProcessStatus.Running);
        resultDefaults.Effects.Should().BeEmpty();
        resultDefaults.RecordedCompensations.Should().BeEmpty();

        var resultCustomStatus = ProcessTransitionResult<TestState>.Advance(state, ProcessStatus.Suspended, null, null);
        resultCustomStatus.Status.Should().Be(ProcessStatus.Suspended);
        resultCustomStatus.Effects.Should().BeEmpty();
        resultCustomStatus.RecordedCompensations.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_Complete_ShouldSetStatusCompleted()
    {
        var state = new TestState("complete", 100);
        var effect = new ProcessEffect.Event(new { Done = true });
        var step = new CompensationStep("StepFinal", new { B = 2 }, DateTimeOffset.UtcNow);

        var result = ProcessTransitionResult<TestState>.Complete(state, [effect], [step]);

        result.Status.Should().Be(ProcessStatus.Completed);
        result.State.Should().Be(state);
        result.Effects.Should().ContainSingle().Which.Should().Be(effect);
        result.RecordedCompensations.Should().ContainSingle().Which.Should().Be(step);

        var resultDefaults = ProcessTransitionResult<TestState>.Complete(state);
        resultDefaults.Status.Should().Be(ProcessStatus.Completed);
        resultDefaults.Effects.Should().BeEmpty();
        resultDefaults.RecordedCompensations.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_Suspend_ShouldSetStatusSuspended()
    {
        var state = new TestState("suspend", 50);
        var timeout = new ProcessEffect.ScheduleTimeout(TimeSpan.FromHours(1), new { Trigger = 1 });

        var result = ProcessTransitionResult<TestState>.Suspend(state, [timeout]);

        result.Status.Should().Be(ProcessStatus.Suspended);
        result.Effects.Should().ContainSingle().Which.Should().Be(timeout);

        var resultDefaults = ProcessTransitionResult<TestState>.Suspend(state);
        resultDefaults.Status.Should().Be(ProcessStatus.Suspended);
        resultDefaults.Effects.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_Compensate_ShouldMapActionsToEffects()
    {
        var state = new TestState("compensating", 0);
        var action1 = new CompensationAction("Action1", new { Payload = 1 });
        var action2 = new CompensationAction("Action2", new { Payload = 2 });

        var result = ProcessTransitionResult<TestState>.Compensate(state, [action1, action2]);

        result.Status.Should().Be(ProcessStatus.Compensating);
        result.Effects.Should().HaveCount(2);
        result.Effects[0].Should().BeOfType<ProcessEffect.Compensation>()
            .Which.Action.Should().Be(action1);
        result.Effects[1].Should().BeOfType<ProcessEffect.Compensation>()
            .Which.Action.Should().Be(action2);

        var resultDefaults = ProcessTransitionResult<TestState>.Compensate(state);
        resultDefaults.Status.Should().Be(ProcessStatus.Compensating);
        resultDefaults.Effects.Should().BeEmpty();

        var resultNullList = ProcessTransitionResult<TestState>.Compensate(state, null);
        resultNullList.Status.Should().Be(ProcessStatus.Compensating);
        resultNullList.Effects.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_Compensated_ShouldSetStatusCompensated()
    {
        var state = new TestState("compensated", 0);
        var effect = new ProcessEffect.Event(new { RolledBack = true });

        var result = ProcessTransitionResult<TestState>.Compensated(state, [effect]);
        result.Status.Should().Be(ProcessStatus.Compensated);
        result.Effects.Should().ContainSingle().Which.Should().Be(effect);

        var resultDefaults = ProcessTransitionResult<TestState>.Compensated(state);
        resultDefaults.Status.Should().Be(ProcessStatus.Compensated);
        resultDefaults.Effects.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_Fail_ShouldSetStatusAndReason()
    {
        var state = new TestState("failed", -1);
        var effect = new ProcessEffect.Event(new { Error = "Fatal" });

        var result = ProcessTransitionResult<TestState>.Fail(state, "Database unreachable", [effect]);
        result.Status.Should().Be(ProcessStatus.Failed);
        result.FailureReason.Should().Be("Database unreachable");
        result.Effects.Should().ContainSingle().Which.Should().Be(effect);

        var resultDefaults = ProcessTransitionResult<TestState>.Fail(state, "Failed without effects");
        resultDefaults.Status.Should().Be(ProcessStatus.Failed);
        resultDefaults.FailureReason.Should().Be("Failed without effects");
        resultDefaults.Effects.Should().BeEmpty();

        var actNull = () => ProcessTransitionResult<TestState>.Fail(state, null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => ProcessTransitionResult<TestState>.Fail(state, string.Empty);
        actEmpty.Should().Throw<ArgumentException>();

        var actWhitespace = () => ProcessTransitionResult<TestState>.Fail(state, "   \t\n  ");
        actWhitespace.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessTransitionResult_Unchanged_ShouldPreserveStatusAndClearEffects()
    {
        var state = new TestState("unchanged", 42);

        var resultRunning = ProcessTransitionResult<TestState>.Unchanged(state);
        resultRunning.Status.Should().Be(ProcessStatus.Running);
        resultRunning.Effects.Should().BeEmpty();

        var resultSuspended = ProcessTransitionResult<TestState>.Unchanged(state, ProcessStatus.Suspended);
        resultSuspended.Status.Should().Be(ProcessStatus.Suspended);
        resultSuspended.Effects.Should().BeEmpty();

        var resultCompleted = ProcessTransitionResult<TestState>.Unchanged(state, ProcessStatus.Completed);
        resultCompleted.Status.Should().Be(ProcessStatus.Completed);
        resultCompleted.Effects.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTransitionResult_RecordSemantics_EqualityAndWith()
    {
        var state = new TestState("rec", 1);
        var res1 = ProcessTransitionResult<TestState>.Advance(state, ProcessStatus.Running);
        var res2 = ProcessTransitionResult<TestState>.Advance(state, ProcessStatus.Running);

        (res1 == res2).Should().BeTrue();
        res1.Equals(res2).Should().BeTrue();
        res1.GetHashCode().Should().Be(res2.GetHashCode());
        res1.ToString().Should().NotBeNullOrWhiteSpace();

        var modified = res1 with { Status = ProcessStatus.Completed };
        modified.Status.Should().Be(ProcessStatus.Completed);
        (modified != res1).Should().BeTrue();
    }

    #endregion

    #region ProcessExecutionResult Tests

    [Fact]
    public void ProcessExecutionResult_Constructor_ShouldValidateAndSetProperties()
    {
        var instance = ProcessInstance<TestState>.Create(
            ProcessId.NewId(), ProcessType.From("test.proc"), ProcessVersion.Initial,
            CorrelationId.NewId(), new TestState("abc", 1), DateTimeOffset.UtcNow);

        var effect = new ProcessEffect.Command(new { X = 1 });

        var successResult = new ProcessExecutionResult<TestState>(instance, [effect], ProcessSaveResult.Success);
        successResult.Instance.Should().Be(instance);
        successResult.Effects.Should().ContainSingle().Which.Should().Be(effect);
        successResult.SaveResult.Should().Be(ProcessSaveResult.Success);
        successResult.IsSuccess.Should().BeTrue();

        var conflictResult = new ProcessExecutionResult<TestState>(instance, null!, ProcessSaveResult.ConcurrencyConflict);
        conflictResult.Effects.Should().BeEmpty();
        conflictResult.SaveResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);
        conflictResult.IsSuccess.Should().BeFalse();

        var notFoundResult = new ProcessExecutionResult<TestState>(instance, [], ProcessSaveResult.NotFound);
        notFoundResult.SaveResult.Should().Be(ProcessSaveResult.NotFound);
        notFoundResult.IsSuccess.Should().BeFalse();

        var persistenceErrorResult = new ProcessExecutionResult<TestState>(instance, [], ProcessSaveResult.PersistenceError);
        persistenceErrorResult.SaveResult.Should().Be(ProcessSaveResult.PersistenceError);
        persistenceErrorResult.IsSuccess.Should().BeFalse();

        var actNullInstance = () => new ProcessExecutionResult<TestState>(null!, [], ProcessSaveResult.Success);
        actNullInstance.Should().Throw<ArgumentNullException>().WithParameterName("instance");
    }

    [Fact]
    public void ProcessExecutionResult_RecordSemantics_EqualityAndWith()
    {
        var instance = ProcessInstance<TestState>.Create(
            ProcessId.NewId(), ProcessType.From("test.proc"), ProcessVersion.Initial,
            CorrelationId.NewId(), new TestState("abc", 1), DateTimeOffset.UtcNow);

        var res1 = new ProcessExecutionResult<TestState>(instance, [], ProcessSaveResult.Success);
        var res2 = new ProcessExecutionResult<TestState>(instance, [], ProcessSaveResult.Success);

        (res1 == res2).Should().BeTrue();
        res1.Equals(res2).Should().BeTrue();
        res1.GetHashCode().Should().Be(res2.GetHashCode());
        res1.ToString().Should().NotBeNullOrWhiteSpace();

        var modified = res1 with { SaveResult = ProcessSaveResult.ConcurrencyConflict };
        modified.SaveResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);
        modified.IsSuccess.Should().BeFalse();
        (modified != res1).Should().BeTrue();
    }

    #endregion

    #region ProcessContext Tests

    [Fact]
    public void ProcessContext_Constructor_DirectInvocation_WithDefaultsAndCustomValues()
    {
        var pid = ProcessId.NewId();
        var cid = CorrelationId.NewId();
        var causeId = CausationId.NewId();
        var msgId = MessageId.NewId();
        var now = DateTimeOffset.UtcNow;

        // Constructor with nulls for optional parameters
        var contextDefault = new ProcessContext(
            processId: pid,
            correlationId: cid,
            causationId: causeId,
            messageId: msgId,
            now: now,
            timeProvider: null,
            items: null,
            cancellationToken: default);

        contextDefault.ProcessId.Should().Be(pid);
        contextDefault.CorrelationId.Should().Be(cid);
        contextDefault.CausationId.Should().Be(causeId);
        contextDefault.MessageId.Should().Be(msgId);
        contextDefault.Now.Should().Be(now);
        contextDefault.TimeProvider.Should().Be(TimeProvider.System);
        contextDefault.Items.Should().BeEmpty();
        contextDefault.CancellationToken.Should().Be(CancellationToken.None);

        // Constructor with custom non-null values
        var fakeTime = new FakeTimeProvider(now);
        using var cts = new CancellationTokenSource();
        var items = new Dictionary<string, object?> { ["Key1"] = "Val1" };

        var contextCustom = new ProcessContext(
            processId: pid,
            correlationId: cid,
            causationId: causeId,
            messageId: msgId,
            now: now,
            timeProvider: fakeTime,
            items: items,
            cancellationToken: cts.Token);

        contextCustom.TimeProvider.Should().Be(fakeTime);
        contextCustom.Items.Should().ContainKey("Key1").WhoseValue.Should().Be("Val1");
        contextCustom.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void ProcessContext_Create_WithDefaults_ShouldPopulateExpectedProperties()
    {
        var pid = ProcessId.NewId();
        var cid = CorrelationId.NewId();

        var context = ProcessContext.Create(pid, cid);

        context.ProcessId.Should().Be(pid);
        context.CorrelationId.Should().Be(cid);
        context.MessageId.Value.Should().NotBeNullOrWhiteSpace();
        context.CausationId.Value.Should().Be(context.MessageId.Value);
        context.TimeProvider.Should().Be(TimeProvider.System);
        context.Items.Should().BeEmpty();
        context.CancellationToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void ProcessContext_Create_WithCustomParameters_ShouldWork()
    {
        var pid = ProcessId.NewId();
        var cid = CorrelationId.NewId();
        var causeId = CausationId.NewId();
        var msgId = MessageId.NewId();

        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        using var cts = new CancellationTokenSource();
        var items = new Dictionary<string, object?> { ["TenantId"] = "tenant-42" };

        var context = ProcessContext.Create(
            processId: pid,
            correlationId: cid,
            causationId: causeId,
            messageId: msgId,
            timeProvider: fakeTime,
            items: items,
            cancellationToken: cts.Token);

        context.ProcessId.Should().Be(pid);
        context.CorrelationId.Should().Be(cid);
        context.CausationId.Should().Be(causeId);
        context.MessageId.Should().Be(msgId);
        context.TimeProvider.Should().Be(fakeTime);
        context.Now.Should().Be(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        context.Items["TenantId"].Should().Be("tenant-42");
        context.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void ProcessContext_Create_WithPartialParameters_ShouldWork()
    {
        var pid = ProcessId.NewId();
        var cid = CorrelationId.NewId();
        var causeId = CausationId.NewId();
        var msgId = MessageId.NewId();

        // Specific causationId but default messageId
        var ctxWithCausation = ProcessContext.Create(pid, cid, causationId: causeId);
        ctxWithCausation.CausationId.Should().Be(causeId);
        ctxWithCausation.MessageId.Value.Should().NotBeNullOrWhiteSpace();

        // Specific messageId but default causationId (causationId derives from messageId)
        var ctxWithMessage = ProcessContext.Create(pid, cid, messageId: msgId);
        ctxWithMessage.MessageId.Should().Be(msgId);
        ctxWithMessage.CausationId.Value.Should().Be(msgId.Value);
    }

    #endregion

    #region Definition Contracts Tests

    private sealed class SampleProcessHandler : IProcessHandler<TestState, TestEvent>, ISaga<TestState>, ICompensationHandler<TestState>
    {
        public ProcessType Type => ProcessType.From("test.process");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<TestState>> HandleAsync(TestState state, TestEvent eventMessage, ProcessContext context)
        {
            return ValueTask.FromResult(ProcessTransitionResult<TestState>.Advance(state with { Value = state.Value + 1 }));
        }

        public ValueTask<ProcessTransitionResult<TestState>> CompensateAsync(TestState state, CompensationAction action, ProcessContext context)
        {
            return ValueTask.FromResult(ProcessTransitionResult<TestState>.Compensated(state));
        }
    }

    [Fact]
    public async Task ProcessDefinitionContracts_CanBeImplementedAndExecuted()
    {
        var handler = new SampleProcessHandler();
        handler.Type.Should().Be(ProcessType.From("test.process"));
        handler.Version.Should().Be(ProcessVersion.Initial);

        var state = new TestState("initial", 10);
        var ctx = ProcessContext.Create(ProcessId.NewId(), CorrelationId.NewId());

        var transition = await handler.HandleAsync(state, new TestEvent("hello"), ctx);
        transition.State.Value.Should().Be(11);
        transition.Status.Should().Be(ProcessStatus.Running);

        var compensation = await handler.CompensateAsync(transition.State, new CompensationAction("step", new { }), ctx);
        compensation.Status.Should().Be(ProcessStatus.Compensated);
    }

    #endregion
}
