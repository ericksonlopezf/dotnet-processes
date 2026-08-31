// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;
using Xunit;

namespace EricksonLopez.Processes.Tests.Testing;

/// <summary>
/// Validates thread-safety and OCC correctness of <see cref="InMemoryProcessStore{TState}"/>
/// under high-concurrency scenarios. Corresponds to the ARCHITECTURAL_AUDIT §18 requirement:
/// "100 parallel tasks executing the same process — InMemoryProcessStore implements OCC real with Interlocked
/// — Verify that the final state is consistent."
/// </summary>
[Trait("Category", "Concurrency")]
public class ConcurrentInMemoryProcessStoreTests
{
    private sealed record CounterState(int Count) : IProcessState;

    private sealed record IncrementEvent(ProcessId TargetId, int Amount);

    private sealed class IncrementCorrelation : IProcessCorrelation<IncrementEvent>
    {
        public ProcessId ExtractProcessId(IncrementEvent @event) => @event.TargetId;
        public CorrelationId ExtractCorrelationId(IncrementEvent @event) => CorrelationId.From(@event.TargetId.ToString());
    }

    private sealed class IncrementHandler : IProcessHandler<CounterState, IncrementEvent>
    {
        public ProcessType Type => ProcessType.From("concurrency.test");
        public ProcessVersion Version => ProcessVersion.Initial;

        public ValueTask<ProcessTransitionResult<CounterState>> HandleAsync(
            CounterState state,
            IncrementEvent eventMessage,
            ProcessContext context)
        {
            var updated = new CounterState(state.Count + eventMessage.Amount);
            return ValueTask.FromResult(ProcessTransitionResult<CounterState>.Advance(updated, ProcessStatus.Running));
        }
    }

    private static ProcessInstance<CounterState> CreateInitial(ProcessId id)
    {
        return ProcessInstance<CounterState>.Create(
            id,
            ProcessType.From("concurrency.test"),
            ProcessVersion.Initial,
            CorrelationId.NewId(),
            new CounterState(0),
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Critical concurrency test required by ARCHITECTURAL_AUDIT §18.
    /// 100 parallel tasks attempt to increment the counter on the same ProcessId.
    /// OCC guarantees that the final count equals the number of successful writes,
    /// and no revision can be skipped. All 100 tasks must eventually commit exactly one increment.
    /// </summary>
    [Fact]
    public async Task ConcurrentExecution_SameProcessId_ConsistentFinalState()
    {
        const int parallelTasks = 100;
        var store = new InMemoryProcessStore<CounterState>();
        var processId = ProcessId.NewId();

        // Seed the initial instance
        var initial = CreateInitial(processId);
        var seedResult = await store.SaveAsync(initial);
        seedResult.Should().Be(ProcessSaveResult.Success);

        var successCount = 0;

        // 100 tasks, each reading the latest revision and trying to increment count by 1.
        // OCC will cause conflicts which are absorbed by the retry loop.
        var tasks = Enumerable.Range(0, parallelTasks).Select(async _ =>
        {
            var maxAttempts = parallelTasks * 2; // generous retry budget
            var attempt = 0;

            while (attempt < maxAttempts)
            {
                attempt++;
                var current = await store.GetByIdAsync(processId);
                if (current is null) break;

                var next = current.Advance(
                    new CounterState(current.State.Count + 1),
                    ProcessStatus.Running,
                    DateTimeOffset.UtcNow);

                var result = await store.SaveAsync(next);

                if (result == ProcessSaveResult.Success)
                {
                    Interlocked.Increment(ref successCount);
                    break;
                }

                // ConcurrencyConflict: retry with a fresh read
                await Task.Yield();
            }
        });

        await Task.WhenAll(tasks);

        var finalInstance = await store.GetByIdAsync(processId);
        finalInstance.Should().NotBeNull();

        // Every successful write incremented Count by exactly 1.
        // Final count == number of successful writes.
        finalInstance!.State.Count.Should().Be(successCount);

        // All 100 tasks should have eventually succeeded (retry loop absorbs conflicts).
        successCount.Should().Be(parallelTasks,
            "every task must eventually commit exactly one increment");

        // The revision is always Initial (1) + number of successful advances.
        finalInstance.Revision.Value.Should().Be(Revision.Initial.Value + successCount);
    }

    /// <summary>
    /// Deterministic test that validates the OCC conflict detection mechanism in isolation.
    /// Two competing updates starting from the same revision must produce exactly one success
    /// and one ConcurrencyConflict — regardless of thread scheduling.
    /// </summary>
    [Fact]
    public async Task OccConflict_TwoCompetingWrites_OnlyOneSucceeds()
    {
        var store = new InMemoryProcessStore<CounterState>();
        var processId = ProcessId.NewId();

        var initial = CreateInitial(processId);
        await store.SaveAsync(initial);

        // Both competitors read the SAME revision (Initial = 1)
        var stateA = await store.GetByIdAsync(processId);
        var stateB = await store.GetByIdAsync(processId);

        stateA!.Revision.Should().Be(stateB!.Revision, "both must start from the same revision");

        var advancedA = stateA.Advance(new CounterState(stateA.State.Count + 10), ProcessStatus.Running, DateTimeOffset.UtcNow);
        var advancedB = stateB.Advance(new CounterState(stateB.State.Count + 20), ProcessStatus.Running, DateTimeOffset.UtcNow);

        var resultA = await store.SaveAsync(advancedA);
        var resultB = await store.SaveAsync(advancedB);

        // Exactly one succeeds, the other conflicts — order depends on which saves first
        var successes = new[] { resultA, resultB }.Count(r => r == ProcessSaveResult.Success);
        var conflicts = new[] { resultA, resultB }.Count(r => r == ProcessSaveResult.ConcurrencyConflict);

        successes.Should().Be(1, "exactly one competing write must win");
        conflicts.Should().Be(1, "the loser must get a ConcurrencyConflict");

        var finalState = await store.GetByIdAsync(processId);
        // Final count should be the winner's value (+10 or +20), not a mix
        finalState!.State.Count.Should().BeOneOf(10, 20);
        finalState.Revision.Value.Should().Be(2); // Initial(1) + one successful advance
    }


    /// <summary>
    /// Validates that concurrent writes from distinct ProcessIds never interfere with each other.
    /// </summary>
    [Fact]
    public async Task ConcurrentExecution_DistinctProcessIds_NoCrossContamination()
    {
        const int processCount = 50;
        var store = new InMemoryProcessStore<CounterState>();

        var ids = Enumerable.Range(0, processCount)
            .Select(_ => ProcessId.NewId())
            .ToArray();

        // Seed all processes
        foreach (var id in ids)
        {
            var instance = CreateInitial(id);
            await store.SaveAsync(instance);
        }

        // Each task owns a unique ProcessId — no OCC conflicts expected
        var tasks = ids.Select(async id =>
        {
            var current = await store.GetByIdAsync(id);
            var next = current!.Advance(
                new CounterState(current.State.Count + 1),
                ProcessStatus.Running,
                DateTimeOffset.UtcNow);

            var result = await store.SaveAsync(next);
            result.Should().Be(ProcessSaveResult.Success,
                $"process {id} should have no concurrency conflict since it owns a unique ID");
        });

        await Task.WhenAll(tasks);

        // Verify each process was incremented exactly once
        foreach (var id in ids)
        {
            var instance = await store.GetByIdAsync(id);
            instance!.State.Count.Should().Be(1);
            instance.Revision.Value.Should().Be(2); // Initial(1) + 1 advance
        }
    }

    /// <summary>
    /// Validates that a new instance cannot be inserted twice with the same ProcessId.
    /// The second insert under OCC should detect the already-existing revision
    /// and return ConcurrencyConflict.
    /// </summary>
    [Fact]
    public async Task ConcurrentCreation_SameProcessId_OnlyOneSucceeds()
    {
        const int parallelCreations = 20;
        var store = new InMemoryProcessStore<CounterState>();
        var sharedId = ProcessId.NewId();

        var successCount = 0;
        var conflictCount = 0;

        var tasks = Enumerable.Range(0, parallelCreations).Select(async _ =>
        {
            var instance = CreateInitial(sharedId);
            var result = await store.SaveAsync(instance);

            if (result == ProcessSaveResult.Success)
                Interlocked.Increment(ref successCount);
            else
                Interlocked.Increment(ref conflictCount);
        });

        await Task.WhenAll(tasks);

        // Exactly ONE creation must have succeeded
        successCount.Should().Be(1, "only one concurrent creation of the same ProcessId can succeed");

        // The remaining (parallelCreations - 1) should have received ConcurrencyConflict
        conflictCount.Should().Be(parallelCreations - 1);

        var finalInstance = await store.GetByIdAsync(sharedId);
        finalInstance.Should().NotBeNull();
        finalInstance!.Revision.Should().Be(Revision.Initial);
    }

    /// <summary>
    /// Validates high-concurrency stress execution through <see cref="ProcessCoordinator{TState}"/>
    /// coordinating 50 parallel tasks on the same instance with automatic CAS retries.
    /// </summary>
    [Fact]
    public async Task ConcurrentExecution_ProcessCoordinator_FiftyParallelIncrements_AllSucceedWithConsistentRevisionAndState()
    {
        const int parallelTasks = 50;
        var store = new InMemoryProcessStore<CounterState>();
        var coordinator = new ProcessCoordinator<CounterState>(
            store,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 100 },
            backoffStrategy: _ => TimeSpan.Zero);

        var handler = new IncrementHandler();
        var correlation = new IncrementCorrelation();
        var sharedId = ProcessId.NewId();

        // Seed initial process instance
        var initial = CreateInitial(sharedId);
        await store.SaveAsync(initial);

        var tasks = Enumerable.Range(0, parallelTasks).Select(async _ =>
        {
            var result = await coordinator.ExecuteAsync(
                handler: handler,
                correlation: correlation,
                eventMessage: new IncrementEvent(sharedId, 1),
                canInitiate: false);

            return result;
        });

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(parallelTasks);
        results.All(r => r.IsSuccess).Should().BeTrue("all 50 concurrent coordinator executions should succeed through CAS retries");

        var finalInstance = await store.GetByIdAsync(sharedId);
        finalInstance.Should().NotBeNull();
        finalInstance!.State.Count.Should().Be(parallelTasks, "final count must equal the exact number of parallel increment executions");
        finalInstance.Revision.Value.Should().Be(Revision.Initial.Value + parallelTasks, "revision must be incremented once per successful commit");
        finalInstance.Status.Should().Be(ProcessStatus.Running);
    }
}






