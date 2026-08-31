// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level07_ScalabilityAndPerformance;

public sealed record FastCounterState(int Value) : IProcessState;
public sealed record FastStepEvent(Guid Id, int Increment);

public sealed class FastThroughputProcess :
    IProcess<FastCounterState>,
    IProcessHandler<FastCounterState, FastStepEvent>
{
    public ProcessType Type => ProcessType.From("fast.throughput.process");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<FastCounterState>> HandleAsync(
        FastCounterState state,
        FastStepEvent eventMessage,
        ProcessContext context)
    {
        return ValueTask.FromResult(ProcessTransitionResult<FastCounterState>.Advance(
            state with { Value = state.Value + eventMessage.Increment }));
    }
}

public sealed class FastStepCorrelation : IProcessCorrelation<FastStepEvent>
{
    public ProcessId ExtractProcessId(FastStepEvent @event) => ProcessId.From(@event.Id);
    public CorrelationId ExtractCorrelationId(FastStepEvent @event) => CorrelationId.From(@event.Id.ToString());
}

/// <summary>
/// Level 7: Scalability, Performance &amp; Zero-Allocation Throughput
/// </summary>
public static class Level07ThroughputDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 07: SCALABILITY & HIGH-THROUGHPUT ZERO-REFLECTION EXECUTION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var store = new InMemoryProcessStore<FastCounterState>();
        var coordinator = new ProcessCoordinator<FastCounterState>(store);
        var process = new FastThroughputProcess();
        var correlation = new FastStepCorrelation();

        const int iterations = 2000;
        var processId = Guid.NewGuid();

        // 1. Initial creation
        await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new FastStepEvent(processId, 1),
            initialStateFactory: _ => new FastCounterState(0),
            canInitiate: true);

        // 2. Measure sustained sequential transition throughput
        Console.WriteLine($"Executing {iterations:N0} sequential state transitions with atomic CAS commits...");

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            await coordinator.ExecuteAsync(
                handler: process,
                correlation: correlation,
                eventMessage: new FastStepEvent(processId, 1),
                canInitiate: false);
        }
        sw.Stop();

        var opsPerSec = iterations / sw.Elapsed.TotalSeconds;

        Console.WriteLine();
        Console.WriteLine($"Total Transitions: {iterations:N0}");
        Console.WriteLine($"Elapsed Time:      {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Throughput:        {opsPerSec:N0} transitions/sec");
        Console.WriteLine($"Diagnostic Source: '{ProcessDiagnostics.SourceName}' Active");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 07 High-Throughput Scalability demo completed successfully.");
        Console.ResetColor();
    }
}
