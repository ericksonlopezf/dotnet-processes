// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level05_ProcessingAndConcurrency;

public sealed record CounterState(int Counter) : IProcessState
{
    public static CounterState Initial() => new(0);
}

public sealed record IncrementCounterEvent(Guid CounterId);

public sealed class CounterProcess :
    IProcess<CounterState>,
    IProcessHandler<CounterState, IncrementCounterEvent>
{
    public ProcessType Type => ProcessType.From("counter.process");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<CounterState>> HandleAsync(
        CounterState state,
        IncrementCounterEvent eventMessage,
        ProcessContext context)
    {
        var updated = state with { Counter = state.Counter + 1 };
        return ValueTask.FromResult(ProcessTransitionResult<CounterState>.Advance(updated));
    }
}

public sealed class IncrementCorrelation : IProcessCorrelation<IncrementCounterEvent>
{
    public ProcessId ExtractProcessId(IncrementCounterEvent @event) => ProcessId.From(@event.CounterId);
    public CorrelationId ExtractCorrelationId(IncrementCounterEvent @event) => CorrelationId.From(@event.CounterId.ToString());
}

/// <summary>
/// Level 5-A: Optimistic Concurrency Control (OCC) &amp; Automatic CAS Retry Loop
/// </summary>
public static class Level05ConcurrencyRetryDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 05-A: OPTIMISTIC CONCURRENCY CONTROL (OCC / CAS) & RETRY LOOP");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Setup FaultInjectingProcessStore with simulated transient CAS conflicts
        var faultStore = new FaultInjectingProcessStore<CounterState>();
        var coordinator = new ProcessCoordinator<CounterState>(
            store: faultStore,
            options: new ProcessCoordinatorOptions { MaxConcurrencyRetries = 3, InitialBackoffDelay = TimeSpan.FromMilliseconds(10) });

        var process = new CounterProcess();
        var correlation = new IncrementCorrelation();
        var counterId = Guid.NewGuid();

        // Step 1: Initial write (success)
        await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new IncrementCounterEvent(counterId),
            initialStateFactory: _ => CounterState.Initial(),
            canInitiate: true);

        Console.WriteLine("Process initialized with counter = 1.");

        // Step 2: Inject 2 consecutive concurrency conflicts
        faultStore.ConcurrencyConflictsToSimulate = 2;
        Console.WriteLine("Injected 2 simulated transient Concurrency Conflicts into storage...");

        // Coordinator should transparently re-fetch instance, re-execute handler, and succeed on 3rd attempt
        var result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new IncrementCounterEvent(counterId),
            canInitiate: false);

        Console.WriteLine();
        Console.WriteLine($"Execution Result IsSuccess: {result.IsSuccess}");
        Console.WriteLine($"Remaining Simulated Faults: {faultStore.ConcurrencyConflictsToSimulate}");
        Console.WriteLine($"Final Counter Value:        {result.Instance.State.Counter}");
        Console.WriteLine($"Final Revision:             {result.Instance.Revision.Value}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 05-A Concurrency & CAS retry loop completed successfully.");
        Console.ResetColor();
    }
}
