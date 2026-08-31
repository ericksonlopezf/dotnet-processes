// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.Testing;

[Trait("Category", "Unit")]
public class InMemoryProcessStoreTests
{
    private sealed record TestState(string Data) : IProcessState;

    [Fact]
    public async Task SaveAsync_NewInstance_ShouldSucceed()
    {
        var store = new InMemoryProcessStore<TestState>();
        var id = ProcessId.NewId();
        var instance = ProcessInstance<TestState>.Create(
            id,
            ProcessType.From("test.process"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new TestState("Initial Data"),
            DateTimeOffset.UtcNow);

        var result = await store.SaveAsync(instance);

        result.Should().Be(ProcessSaveResult.Success);
        var exists = await store.ExistsAsync(id);
        exists.Should().BeTrue();

        var loaded = await store.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.Revision.Should().Be(Revision.Initial);
        loaded.State.Data.Should().Be("Initial Data");
    }

    [Fact]
    public async Task SaveAsync_UpdatedInstance_WithMatchingRevision_ShouldSucceed()
    {
        var store = new InMemoryProcessStore<TestState>();
        var id = ProcessId.NewId();
        var instance = ProcessInstance<TestState>.Create(
            id,
            ProcessType.From("test.process"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new TestState("Step 1"),
            DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        var updated = instance.Advance(new TestState("Step 2"), ProcessStatus.Running, DateTimeOffset.UtcNow);
        var result = await store.SaveAsync(updated);

        result.Should().Be(ProcessSaveResult.Success);
        var loaded = await store.GetByIdAsync(id);
        loaded!.Revision.Value.Should().Be(2);
        loaded.State.Data.Should().Be("Step 2");
    }

    [Fact]
    public async Task SaveAsync_ConflictingRevision_ShouldReturnConcurrencyConflict()
    {
        var store = new InMemoryProcessStore<TestState>();
        var id = ProcessId.NewId();
        var instance = ProcessInstance<TestState>.Create(
            id,
            ProcessType.From("test.process"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new TestState("Step 1"),
            DateTimeOffset.UtcNow);

        await store.SaveAsync(instance);

        // Advance instance to Rev 2 and save
        var updated = instance.Advance(new TestState("Step 2"), ProcessStatus.Running, DateTimeOffset.UtcNow);
        await store.SaveAsync(updated);

        // Stale instance advancing from Rev 1 to Rev 2 (while DB is already at Rev 2)
        var staleAdvance = instance.Advance(new TestState("Stale Step 2"), ProcessStatus.Running, DateTimeOffset.UtcNow);
        var conflictResult = await store.SaveAsync(staleAdvance);

        conflictResult.Should().Be(ProcessSaveResult.ConcurrencyConflict);

        // Value in store should remain Step 2
        var loaded = await store.GetByIdAsync(id);
        loaded!.State.Data.Should().Be("Step 2");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ShouldReturnNull()
    {
        var store = new InMemoryProcessStore<TestState>();
        var loaded = await store.GetByIdAsync(ProcessId.NewId());
        loaded.Should().BeNull();

        var exists = await store.ExistsAsync(ProcessId.NewId());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_NullInstance_ShouldThrowArgumentNullException()
    {
        var store = new InMemoryProcessStore<TestState>();
        var act = async () => await store.SaveAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}





