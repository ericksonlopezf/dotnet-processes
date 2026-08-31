// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level08_Customization;

// ---------------------------------------------------------------------------
// Custom ISagaSnapshotRepository<TState> — in-memory implementation
// ---------------------------------------------------------------------------

public sealed record SubscriptionState(
    string SubscriptionId,
    string Plan,
    int BillingCyclesCompleted,
    bool IsActive) : IProcessState;

/// <summary>
/// In-memory snapshot repository for demonstration purposes.
/// In production, snapshots would be persisted to durable storage (database, blob, etc.)
/// alongside the main process state table.
/// </summary>
public sealed class InMemorySagaSnapshotRepository<TState> : ISagaSnapshotRepository<TState>
    where TState : notnull
{
    private readonly ConcurrentDictionary<ProcessId, (Revision Revision, TState State)> _snapshots = new();

    /// <inheritdoc />
    public ValueTask SaveSnapshotAsync(
        ProcessId processId,
        Revision revision,
        TState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        _snapshots[processId] = (revision, state);

        Console.WriteLine($"  [Snapshot] Saved: processId={processId}, revision={revision.Value}");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<(Revision Revision, TState State)?> GetLatestSnapshotAsync(
        ProcessId processId,
        CancellationToken cancellationToken = default)
    {
        if (_snapshots.TryGetValue(processId, out var snapshot))
        {
            Console.WriteLine($"  [Snapshot] Found: processId={processId}, revision={snapshot.Revision.Value}");
            return ValueTask.FromResult<(Revision, TState)?>((snapshot.Revision, snapshot.State));
        }

        Console.WriteLine($"  [Snapshot] Not found for processId={processId}");
        return ValueTask.FromResult<(Revision, TState)?>(null);
    }

    /// <summary>Gets the total number of stored snapshots.</summary>
    public int Count => _snapshots.Count;
}

/// <summary>
/// Level 8-C: ISagaSnapshotRepository — Custom Snapshot Storage
/// Demonstrates implementing <see cref="ISagaSnapshotRepository{TState}"/> for capturing
/// and restoring periodic state checkpoints. Useful for long-running sagas with many
/// revision steps, where re-hydrating from scratch would be costly.
/// </summary>
public static class Level08SnapshotRepositoryDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 08-C: ISagaSnapshotRepository — CUSTOM SNAPSHOT STORAGE");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var snapshotRepo = new InMemorySagaSnapshotRepository<SubscriptionState>();

        var processId = ProcessId.NewId();
        var initialState = new SubscriptionState("SUB-2024-001", "Pro", 0, true);

        // -----------------------------------------------------------------------
        // 1. SaveSnapshotAsync — capture checkpoint at revision 5
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 1] SaveSnapshotAsync — capture checkpoint at Revision 5");
        var revisionAtCheckpoint = Revision.From(5);
        await snapshotRepo.SaveSnapshotAsync(processId, revisionAtCheckpoint, initialState);

        // -----------------------------------------------------------------------
        // 2. Simulate forward progress — state evolves after the snapshot
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 2] Forward execution — state evolves (billing cycles progress)");
        var latestState = initialState with { BillingCyclesCompleted = 12 };
        var revisionLatest = Revision.From(50);
        await snapshotRepo.SaveSnapshotAsync(processId, revisionLatest, latestState);

        // -----------------------------------------------------------------------
        // 3. GetLatestSnapshotAsync — restore latest checkpoint
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 3] GetLatestSnapshotAsync — restore from checkpoint");
        var snapshot = await snapshotRepo.GetLatestSnapshotAsync(processId);

        if (snapshot.HasValue)
        {
            var (restoredRevision, restoredState) = snapshot.Value;
            Console.WriteLine($"  Restored at Revision:  {restoredRevision.Value}");
            Console.WriteLine($"  SubscriptionId:        {restoredState.SubscriptionId}");
            Console.WriteLine($"  Plan:                  {restoredState.Plan}");
            Console.WriteLine($"  BillingCycles:         {restoredState.BillingCyclesCompleted}");
            Console.WriteLine($"  IsActive:              {restoredState.IsActive}");
        }

        // -----------------------------------------------------------------------
        // 4. GetLatestSnapshotAsync on unknown process — returns null
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 4] GetLatestSnapshotAsync for unknown processId — returns null");
        var unknownSnapshot = await snapshotRepo.GetLatestSnapshotAsync(ProcessId.NewId());
        Console.WriteLine($"  Snapshot result: {(unknownSnapshot.HasValue ? "found" : "null (expected)")}");

        // -----------------------------------------------------------------------
        // 5. Explain when to use snapshots
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Guidance] When to use ISagaSnapshotRepository:");
        Console.WriteLine("  • Long-running sagas with hundreds or thousands of events");
        Console.WriteLine("  • Re-hydration performance optimization (skip early revisions)");
        Console.WriteLine("  • Audit trail checkpoints for regulatory compliance");
        Console.WriteLine("  • Combined with IProcessStore for full durability");
        Console.WriteLine("  Note: ISagaSnapshotRepository is separate from IProcessStore.");
        Console.WriteLine("  Application code must call SaveSnapshotAsync explicitly when appropriate.");
        Console.WriteLine($"\n  Total snapshots stored: {snapshotRepo.Count}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 08-C ISagaSnapshotRepository demo completed successfully.");
        Console.ResetColor();
    }
}
