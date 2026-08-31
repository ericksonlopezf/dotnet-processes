// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level06_ErrorHandlingAndRecovery;

public sealed record StrictState(string Status) : IProcessState;
public sealed record NonInitiatingEvent(Guid ProcessId);

public sealed class StrictProcess :
    IProcess<StrictState>,
    IProcessHandler<StrictState, NonInitiatingEvent>
{
    public ProcessType Type => ProcessType.From("strict.process");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<StrictState>> HandleAsync(
        StrictState state,
        NonInitiatingEvent eventMessage,
        ProcessContext context)
    {
        return ValueTask.FromResult(ProcessTransitionResult<StrictState>.Advance(state));
    }
}

public sealed class StrictCorrelation : IProcessCorrelation<NonInitiatingEvent>
{
    public ProcessId ExtractProcessId(NonInitiatingEvent @event) => ProcessId.From(@event.ProcessId);
    public CorrelationId ExtractCorrelationId(NonInitiatingEvent @event) => CorrelationId.From(@event.ProcessId.ToString());
}

/// <summary>
/// Level 6-B: Exception Taxonomy &amp; Error Guarantees
/// Demonstrates ProcessNotFoundException, ConcurrencyConflictException, and InvalidProcessTransitionException.
/// </summary>
public static class Level06InvalidTransitionDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 06-B: EXCEPTION TAXONOMY & PROCESS RECOVERY GUARANTEES");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<StrictState>();
        var coordinator = new ProcessCoordinator<StrictState>(store);
        var process = new StrictProcess();
        var correlation = new StrictCorrelation();
        var missingId = Guid.NewGuid();

        // 1. ProcessNotFoundException when event cannot initiate a missing process
        Console.WriteLine("Attempting to execute non-initiating event on a non-existent process...");
        try
        {
            await coordinator.ExecuteAsync(
                handler: process,
                correlation: correlation,
                eventMessage: new NonInitiatingEvent(missingId),
                canInitiate: false);
        }
        catch (ProcessNotFoundException ex)
        {
            Console.WriteLine($"  [Caught Expected] ProcessNotFoundException: {ex.Message}");
        }

        // 2. ConcurrencyConflictException when max retries are exceeded
        Console.WriteLine("\nSimulating exhausted concurrency retry attempts (FaultStore)...");
        var faultStore = new FaultInjectingProcessStore<StrictState>
        {
            ConcurrencyConflictsToSimulate = 10 // exceeds max retries of 2
        };
        var retryCoordinator = new ProcessCoordinator<StrictState>(
            store: faultStore,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 2, InitialBackoffDelay = TimeSpan.FromMilliseconds(5) });

        var testId = Guid.NewGuid();
        // Seed instance
        await faultStore.InnerStore.SaveAsync(ProcessInstance<StrictState>.Create(
            id: ProcessId.From(testId),
            type: process.Type,
            version: process.Version,
            correlationId: CorrelationId.From(testId),
            initialState: new StrictState("Initialized"),
            now: DateTimeOffset.UtcNow));

        try
        {
            await retryCoordinator.ExecuteAsync(
                handler: process,
                correlation: correlation,
                eventMessage: new NonInitiatingEvent(testId),
                canInitiate: false);
        }
        catch (ConcurrencyConflictException ex)
        {
            Console.WriteLine($"  [Caught Expected] ConcurrencyConflictException for process '{ex.ProcessId}' at Revision '{ex.ExpectedRevision.Value}'");
        }

        // 3. Demonstrate InvalidProcessTransitionException type safety
        var invalidEx = new InvalidProcessTransitionException(
            processId: ProcessId.From(testId),
            currentStatus: ProcessStatus.Completed,
            attemptedStatus: ProcessStatus.Running,
            reason: "Terminal processes cannot transition back to Running.");

        Console.WriteLine($"\nConstructed InvalidProcessTransitionException: {invalidEx.Message}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 06-B Exception Taxonomy & Recovery demo completed successfully.");
        Console.ResetColor();
    }
}
