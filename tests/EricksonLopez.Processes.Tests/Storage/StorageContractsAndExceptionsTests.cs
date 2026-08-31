// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Storage;

[Trait("Category", "Unit")]
public class StorageContractsAndExceptionsTests
{
    private sealed record TestOrderState(string OrderId, decimal Total) : IProcessState;

    private sealed class StubProcessStore : IProcessStore<TestOrderState>
    {
        public ValueTask<ProcessInstance<TestOrderState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ProcessInstance<TestOrderState>?>(null);

        public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TestOrderState> instance, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ProcessSaveResult.Success);

        public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);
    }

    private sealed class CustomCorrelationProcessStore : IProcessStore<TestOrderState>
    {
        public ProcessInstance<TestOrderState>? StoredInstance { get; set; }

        public ValueTask<ProcessInstance<TestOrderState>?> GetByIdAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(StoredInstance);

        public ValueTask<ProcessSaveResult> SaveAsync(ProcessInstance<TestOrderState> instance, CancellationToken cancellationToken = default)
        {
            StoredInstance = instance;
            return ValueTask.FromResult(ProcessSaveResult.Success);
        }

        public ValueTask<bool> ExistsAsync(ProcessId id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(StoredInstance is not null && StoredInstance.Id == id);

        public ValueTask<ProcessInstance<TestOrderState>?> GetByCorrelationIdAsync(CorrelationId correlationId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(StoredInstance is not null && StoredInstance.CorrelationId == correlationId ? StoredInstance : null);
    }

    private sealed class InMemorySnapshotRepo : ISagaSnapshotRepository<TestOrderState>
    {
        private readonly Dictionary<ProcessId, (Revision Revision, TestOrderState State)> _snapshots = new();

        public ValueTask SaveSnapshotAsync(ProcessId processId, Revision revision, TestOrderState state, CancellationToken cancellationToken = default)
        {
            _snapshots[processId] = (revision, state);
            return ValueTask.CompletedTask;
        }

        public ValueTask<(Revision Revision, TestOrderState State)?> GetLatestSnapshotAsync(ProcessId processId, CancellationToken cancellationToken = default)
        {
            if (_snapshots.TryGetValue(processId, out var snapshot))
            {
                return ValueTask.FromResult<(Revision Revision, TestOrderState State)?>(snapshot);
            }

            return ValueTask.FromResult<(Revision Revision, TestOrderState State)?>(null);
        }
    }

    #region ProcessStateRecord & ProcessSaveResult Tests

    [Fact]
    public void ProcessStateRecord_PropertiesAndRecordSemantics_ShouldWork()
    {
        var now = DateTimeOffset.UtcNow;
        var record = new ProcessStateRecord
        {
            ProcessId = "pid-1",
            ProcessType = "OrderProcess",
            Version = "1",
            Status = 1,
            Revision = 2,
            CorrelationId = "corr-1",
            StatePayload = "{\"OrderId\":\"1\"}",
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = null
        };

        record.ProcessId.Should().Be("pid-1");
        record.ProcessType.Should().Be("OrderProcess");
        record.Version.Should().Be("1");
        record.Status.Should().Be(1);
        record.Revision.Should().Be(2);
        record.CorrelationId.Should().Be("corr-1");
        record.StatePayload.Should().Be("{\"OrderId\":\"1\"}");
        record.CreatedAt.Should().Be(now);
        record.UpdatedAt.Should().Be(now);
        record.CompletedAt.Should().BeNull();

        var recordCopy = record with { CompletedAt = now.AddMinutes(5) };
        recordCopy.CompletedAt.Should().Be(now.AddMinutes(5));
        (record == recordCopy).Should().BeFalse();

        var recordIdentical = record with { };
        (record == recordIdentical).Should().BeTrue();
        record.GetHashCode().Should().Be(recordIdentical.GetHashCode());
    }

    [Fact]
    public void ProcessSaveResult_EnumValues_ShouldMatchContract()
    {
        ((int)ProcessSaveResult.Success).Should().Be(0);
        ((int)ProcessSaveResult.ConcurrencyConflict).Should().Be(1);
        ((int)ProcessSaveResult.NotFound).Should().Be(2);
        ((int)ProcessSaveResult.PersistenceError).Should().Be(3);
    }

    #endregion

    #region IProcessStore & ISagaSnapshotRepository Tests

    [Fact]
    public async Task IProcessStore_DefaultGetByCorrelationIdAsync_ShouldReturnNull()
    {
        IProcessStore<TestOrderState> store = new StubProcessStore();
        var correlationId = CorrelationId.NewId();

        var result = await store.GetByCorrelationIdAsync(correlationId);
        result.Should().BeNull();

        var notFound = await store.GetByIdAsync(ProcessId.NewId());
        notFound.Should().BeNull();

        var exists = await store.ExistsAsync(ProcessId.NewId());
        exists.Should().BeFalse();

        var saveResult = await store.SaveAsync(ProcessInstance<TestOrderState>.Create(
            ProcessId.NewId(), ProcessType.From("order"), ProcessVersion.Initial, correlationId,
            new TestOrderState("ord-1", 10m), DateTimeOffset.UtcNow));
        saveResult.Should().Be(ProcessSaveResult.Success);
    }

    [Fact]
    public async Task IProcessStore_CustomImplementation_ShouldReturnStoredInstance()
    {
        var store = new CustomCorrelationProcessStore();
        var id = ProcessId.NewId();
        var correlationId = CorrelationId.NewId();
        var instance = ProcessInstance<TestOrderState>.Create(
            id, ProcessType.From("order"), ProcessVersion.Initial, correlationId,
            new TestOrderState("ord-1", 50m), DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        var byId = await store.GetByIdAsync(id);
        byId.Should().BeSameAs(instance);

        var byCorr = await store.GetByCorrelationIdAsync(correlationId);
        byCorr.Should().BeSameAs(instance);

        var byOtherCorr = await store.GetByCorrelationIdAsync(CorrelationId.NewId());
        byOtherCorr.Should().BeNull();

        var exists = await store.ExistsAsync(id);
        exists.Should().BeTrue();

        var notExists = await store.ExistsAsync(ProcessId.NewId());
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task ISagaSnapshotRepository_Implementation_ShouldSaveAndRetrieveSnapshots()
    {
        ISagaSnapshotRepository<TestOrderState> repo = new InMemorySnapshotRepo();
        var id = ProcessId.NewId();

        var initial = await repo.GetLatestSnapshotAsync(id);
        initial.Should().BeNull();

        var state = new TestOrderState("ord-2", 100m);
        var revision = Revision.From(3);
        await repo.SaveSnapshotAsync(id, revision, state);

        var snapshot = await repo.GetLatestSnapshotAsync(id);
        snapshot.Should().NotBeNull();
        snapshot!.Value.Revision.Should().Be(revision);
        snapshot.Value.State.Should().Be(state);
    }

    #endregion

    #region ProcessException Hierarchy Tests

    [Fact]
    public void ProcessException_Constructors_ShouldSetExpectedMessagesAndProperties()
    {
        var exDefault = new ProcessException();
        exDefault.Message.Should().Be("An error occurred during process execution.");
        exDefault.ProcessId.Should().BeNull();
        exDefault.InnerException.Should().BeNull();

        var exMsg = new ProcessException("custom error");
        exMsg.Message.Should().Be("custom error");
        exMsg.ProcessId.Should().BeNull();

        var inner = new InvalidOperationException("inner error");
        var exInner = new ProcessException("custom with inner", inner);
        exInner.Message.Should().Be("custom with inner");
        exInner.InnerException.Should().BeSameAs(inner);

        var pid = ProcessId.NewId();
        var exPid = new ProcessException("pid error", pid);
        exPid.ProcessId.Should().Be(pid);
        exPid.InnerException.Should().BeNull();

        var exPidInner = new ProcessException("pid error with inner", pid, inner);
        exPidInner.ProcessId.Should().Be(pid);
        exPidInner.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ProcessNotFoundException_Constructors_ShouldSetExpectedMessagesAndProperties()
    {
        var exDefault = new ProcessNotFoundException();
        exDefault.Message.Should().Be("Process instance was not found in storage.");
        exDefault.ProcessId.Should().BeNull();

        var exMsg = new ProcessNotFoundException("Instance 404");
        exMsg.Message.Should().Be("Instance 404");

        var inner = new InvalidOperationException("db error");
        var exInner = new ProcessNotFoundException("Instance 404", inner);
        exInner.InnerException.Should().BeSameAs(inner);

        var pid = ProcessId.NewId();
        var exPid = new ProcessNotFoundException(pid);
        exPid.ProcessId.Should().Be(pid);
        exPid.Message.Should().Be($"Process instance with ID '{pid}' was not found in storage.");
    }

    [Fact]
    public void ConcurrencyConflictException_Constructors_ShouldSetExpectedMessagesAndProperties()
    {
        var exDefault = new ConcurrencyConflictException();
        exDefault.Message.Should().Be("A concurrency conflict occurred during process persistence.");
        exDefault.ProcessId.Should().BeNull();
        exDefault.ExpectedRevision.Should().Be(default);

        var exMsg = new ConcurrencyConflictException("conflict occurred");
        exMsg.Message.Should().Be("conflict occurred");

        var inner = new InvalidOperationException("db conflict");
        var exInner = new ConcurrencyConflictException("conflict occurred", inner);
        exInner.InnerException.Should().BeSameAs(inner);

        var pid = ProcessId.NewId();
        var expectedRev = Revision.From(7);
        var exPid = new ConcurrencyConflictException(pid, expectedRev);
        exPid.ProcessId.Should().Be(pid);
        exPid.ExpectedRevision.Should().Be(expectedRev);
        exPid.Message.Should().Be($"Concurrency conflict detected for process '{pid}'. Expected revision '{expectedRev}'.");
    }

    [Fact]
    public void InvalidProcessTransitionException_Constructors_ShouldSetExpectedMessagesAndProperties()
    {
        var exDefault = new InvalidProcessTransitionException();
        exDefault.Message.Should().Be("An invalid process state transition was attempted.");
        exDefault.ProcessId.Should().BeNull();
        exDefault.CurrentStatus.Should().Be(default);
        exDefault.AttemptedStatus.Should().Be(default);

        var exMsg = new InvalidProcessTransitionException("bad transition");
        exMsg.Message.Should().Be("bad transition");

        var inner = new InvalidOperationException("transition error");
        var exInner = new InvalidProcessTransitionException("bad transition", inner);
        exInner.InnerException.Should().BeSameAs(inner);

        var pid = ProcessId.NewId();
        var exPid = new InvalidProcessTransitionException(pid, ProcessStatus.Completed, ProcessStatus.Running, "Already terminal");
        exPid.ProcessId.Should().Be(pid);
        exPid.CurrentStatus.Should().Be(ProcessStatus.Completed);
        exPid.AttemptedStatus.Should().Be(ProcessStatus.Running);
        exPid.Message.Should().Be($"Invalid state transition for process '{pid}' from '{ProcessStatus.Completed}' to '{ProcessStatus.Running}': Already terminal");
    }

    [Fact]
    public void CompensationFailedException_Constructors_ShouldSetExpectedMessagesAndProperties()
    {
        var exDefault = new CompensationFailedException();
        exDefault.Message.Should().Be("A saga compensation step failed.");
        exDefault.ProcessId.Should().BeNull();
        exDefault.StepName.Should().BeEmpty();

        var exMsg = new CompensationFailedException("comp failed");
        exMsg.Message.Should().Be("comp failed");
        exMsg.StepName.Should().BeEmpty();

        var inner = new InvalidOperationException("network error");
        var exInner = new CompensationFailedException("comp failed", inner);
        exInner.InnerException.Should().BeSameAs(inner);
        exInner.StepName.Should().BeEmpty();

        var pid = ProcessId.NewId();
        var exPid = new CompensationFailedException(pid, "CancelOrder", "Downstream service error");
        exPid.ProcessId.Should().Be(pid);
        exPid.StepName.Should().Be("CancelOrder");
        exPid.InnerException.Should().BeNull();
        exPid.Message.Should().Be($"Compensation step 'CancelOrder' failed for saga '{pid}': Downstream service error");

        var exPidInner = new CompensationFailedException(pid, "CancelOrder", "Downstream service error", inner);
        exPidInner.ProcessId.Should().Be(pid);
        exPidInner.StepName.Should().Be("CancelOrder");
        exPidInner.InnerException.Should().BeSameAs(inner);
        exPidInner.Message.Should().Be($"Compensation step 'CancelOrder' failed for saga '{pid}': Downstream service error");
    }

    #endregion
}
