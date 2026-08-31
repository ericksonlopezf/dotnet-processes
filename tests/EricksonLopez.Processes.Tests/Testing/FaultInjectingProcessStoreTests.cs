// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Processes.Tests.Testing;

[Trait("Category", "Unit")]
public class FaultInjectingProcessStoreTests
{
    public sealed record TestState(string Data) : IProcessState;

    private static ProcessInstance<TestState> CreateInstance(ProcessId id, string data = "Initial")
    {
        return ProcessInstance<TestState>.Create(
            id,
            ProcessType.From("fault.test"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new TestState(data),
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DefaultConstructor_UsesInMemoryProcessStore()
    {
        var store = new FaultInjectingProcessStore<TestState>();
        store.InnerStore.Should().NotBeNull();
        store.InnerStore.Should().BeOfType<InMemoryProcessStore<TestState>>();

        var id = ProcessId.NewId();
        var instance = CreateInstance(id);
        var saveResult = await store.SaveAsync(instance);

        saveResult.Should().Be(ProcessSaveResult.Success);
        var exists = await store.ExistsAsync(id);
        exists.Should().BeTrue();

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
    }

    [Fact]
    public async Task ConcurrencyConflictsToSimulate_ShouldReturnConflictUntilExhausted()
    {
        var store = new FaultInjectingProcessStore<TestState>
        {
            ConcurrencyConflictsToSimulate = 2
        };
        store.ConcurrencyConflictsToSimulate.Should().Be(2);

        var id = ProcessId.NewId();
        var instance = CreateInstance(id);

        // Attempt 1: conflict
        var result1 = await store.SaveAsync(instance);
        result1.Should().Be(ProcessSaveResult.ConcurrencyConflict);
        store.ConcurrencyConflictsToSimulate.Should().Be(1);

        // Attempt 2: conflict
        var result2 = await store.SaveAsync(instance);
        result2.Should().Be(ProcessSaveResult.ConcurrencyConflict);
        store.ConcurrencyConflictsToSimulate.Should().Be(0);

        // Attempt 3: success (exhausted)
        var result3 = await store.SaveAsync(instance);
        result3.Should().Be(ProcessSaveResult.Success);
    }

    [Fact]
    public void ConcurrencyConflictsToSimulate_ShouldClampNegativeValuesToZero()
    {
        var store = new FaultInjectingProcessStore<TestState>
        {
            ConcurrencyConflictsToSimulate = -5
        };
        store.ConcurrencyConflictsToSimulate.Should().Be(0);
    }

    [Fact]
    public async Task ForcedSaveResult_ShouldReturnConfiguredResult()
    {
        var store = new FaultInjectingProcessStore<TestState>
        {
            ForcedSaveResult = ProcessSaveResult.PersistenceError
        };
        store.ForcedSaveResult.Should().Be(ProcessSaveResult.PersistenceError);

        var instance = CreateInstance(ProcessId.NewId());
        var result = await store.SaveAsync(instance);

        result.Should().Be(ProcessSaveResult.PersistenceError);
    }

    [Fact]
    public async Task ExceptionToThrowOnSave_ShouldThrowConfiguredException()
    {
        var expectedEx = new InvalidOperationException("Simulated database connection loss");
        var store = new FaultInjectingProcessStore<TestState>
        {
            ExceptionToThrowOnSave = expectedEx
        };
        store.ExceptionToThrowOnSave.Should().BeSameAs(expectedEx);

        var instance = CreateInstance(ProcessId.NewId());
        var act = async () => await store.SaveAsync(instance);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(expectedEx);
    }

    [Fact]
    public async Task ExceptionToThrowOnGet_ShouldThrowConfiguredException()
    {
        var expectedEx = new TimeoutException("Simulated read timeout");
        var store = new FaultInjectingProcessStore<TestState>
        {
            ExceptionToThrowOnGet = expectedEx
        };
        store.ExceptionToThrowOnGet.Should().BeSameAs(expectedEx);

        var act = async () => await store.GetByIdAsync(ProcessId.NewId());

        var thrown = await act.Should().ThrowAsync<TimeoutException>();
        thrown.Which.Should().BeSameAs(expectedEx);
    }

    [Fact]
    public async Task SaveAsync_NullInstance_ShouldThrowArgumentNullException()
    {
        var mockInner = NSubstitute.Substitute.For<IProcessStore<TestState>>();
        var store = new FaultInjectingProcessStore<TestState>(mockInner)
        {
            ForcedSaveResult = ProcessSaveResult.Success
        };

        var act = async () => await store.SaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("instance");
        await mockInner.DidNotReceive().SaveAsync(NSubstitute.Arg.Any<ProcessInstance<TestState>>(), NSubstitute.Arg.Any<CancellationToken>());
    }
}






