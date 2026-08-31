// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level07_ScalabilityAndPerformance;

// ---------------------------------------------------------------------------
// Domain model for diagnostics demonstration
// ---------------------------------------------------------------------------

public sealed record MetricsOrderState(string OrderId, decimal Total) : IProcessState;
public sealed record MetricsOrderPlacedEvent(Guid OrderId, decimal Total);

public sealed class MetricsOrderProcess :
    IProcess<MetricsOrderState>,
    IProcessHandler<MetricsOrderState, MetricsOrderPlacedEvent>
{
    public ProcessType Type => ProcessType.From("order.metrics-demo");
    public ProcessVersion Version => ProcessVersion.Initial;

    public ValueTask<ProcessTransitionResult<MetricsOrderState>> HandleAsync(
        MetricsOrderState state,
        MetricsOrderPlacedEvent evt,
        ProcessContext context)
    {
        var updated = state with { OrderId = evt.OrderId.ToString(), Total = evt.Total };
        return ValueTask.FromResult(ProcessTransitionResult<MetricsOrderState>.Complete(updated));
    }
}

public sealed class MetricsOrderCorrelation : IProcessCorrelation<MetricsOrderPlacedEvent>
{
    public ProcessId ExtractProcessId(MetricsOrderPlacedEvent @event) => ProcessId.From(@event.OrderId);
    public CorrelationId ExtractCorrelationId(MetricsOrderPlacedEvent @event) => CorrelationId.From(@event.OrderId.ToString());
}

/// <summary>
/// Level 7-B: ProcessDiagnostics — OpenTelemetry ActivitySource and Metrics
/// Demonstrates the built-in observability contract exposed by <see cref="ProcessDiagnostics"/>:
/// the ActivitySource name, all public metric recording methods, and integration guidance
/// for OpenTelemetry pipelines.
/// </summary>
public static class Level07DiagnosticsDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 07-B: PROCESSDIAGNOSTICS — OPENTELEMETRY ACTIVITYSOURCE & METRICS");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // -----------------------------------------------------------------------
        // 1. ActivitySource — trace spans are automatically created by the coordinator
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Section 1] ActivitySource — distributed tracing");
        Console.WriteLine($"  ActivitySource.Name: '{ProcessDiagnostics.ActivitySource.Name}'");
        Console.WriteLine($"  ActivitySource.Version: '{ProcessDiagnostics.ActivitySource.Version ?? "(none)"}'");
        Console.WriteLine();
        Console.WriteLine("  OpenTelemetry Registration (application startup):");
        Console.WriteLine("    builder.Services.AddOpenTelemetry()");
        Console.WriteLine($"       .WithTracing(tracing => tracing.AddSource(\"{ProcessDiagnostics.ActivitySource.Name}\"))");
        Console.WriteLine();
        Console.WriteLine("  Spans emitted per coordinator call:");
        Console.WriteLine("    • 'Process <type>.Execute'   — one span per ExecuteAsync()");
        Console.WriteLine("    • 'Process <type>.Compensate' — one span per CompensateAsync()");
        Console.WriteLine("  Tags set on each span:");
        Console.WriteLine("    • process.id, process.type, process.version");

        // -----------------------------------------------------------------------
        // 2. Metrics — counters and histograms registered by ProcessDiagnostics
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Section 2] Metrics — counters and histograms");
        Console.WriteLine("  OpenTelemetry Registration:");
        Console.WriteLine($"    builder.Services.AddOpenTelemetry()");
        Console.WriteLine($"       .WithMetrics(metrics => metrics.AddMeter(\"{ProcessDiagnostics.ActivitySource.Name}\"))");
        Console.WriteLine();
        Console.WriteLine("  Counters emitted:");
        Console.WriteLine("    • ericksonlopez.processes.started       — tags: process.type, process.version");
        Console.WriteLine("    • ericksonlopez.processes.completed      — tags: process.type, process.version");
        Console.WriteLine("    • ericksonlopez.processes.failed         — tags: process.type, process.version, error.reason");
        Console.WriteLine("    • ericksonlopez.processes.compensated    — tags: process.type, process.version");
        Console.WriteLine("    • ericksonlopez.processes.conflicts      — tags: process.type, process.version");
        Console.WriteLine("  Histograms emitted:");
        Console.WriteLine("    • ericksonlopez.processes.transition_ms  — tags: process.type");

        // -----------------------------------------------------------------------
        // 3. Execute a real coordinator operation to trigger built-in diagnostics
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Section 3] Live coordinator execution (triggers internal diagnostic recording)");

        var store = new InMemoryProcessStore<MetricsOrderState>();
        var coordinator = new ProcessCoordinator<MetricsOrderState>(store);
        var process = new MetricsOrderProcess();
        var correlation = new MetricsOrderCorrelation();

        // Subscribe to the ActivitySource to capture spans in this demo (no-op in console apps)
        // In production, the OpenTelemetry SDK handles this automatically.
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ProcessDiagnostics.ActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                Console.WriteLine($"  [ActivityListener] Span started: '{activity.OperationName}'");
            },
            ActivityStopped = activity =>
            {
                Console.WriteLine($"  [ActivityListener] Span stopped: '{activity.OperationName}' (duration: {activity.Duration.TotalMilliseconds:F1}ms)");
            }
        };
        ActivitySource.AddActivityListener(listener);

        var result = await coordinator.ExecuteAsync(
            handler: process,
            correlation: correlation,
            eventMessage: new MetricsOrderPlacedEvent(Guid.NewGuid(), 1299.00m),
            initialStateFactory: e => new MetricsOrderState(e.OrderId.ToString(), e.Total),
            canInitiate: true);

        Console.WriteLine($"\n  Execution IsSuccess:   {result.IsSuccess}");
        Console.WriteLine($"  Process final status:  {result.Instance.Status}");

        // -----------------------------------------------------------------------
        // 4. Manual metric recording — direct API calls
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Section 4] Manual metric recording via ProcessDiagnostics");
        Console.WriteLine("  (Normally called internally by ProcessCoordinator; shown here for API documentation)");

        ProcessDiagnostics.RecordProcessStarted("order.metrics-demo", 1);
        Console.WriteLine("  ProcessDiagnostics.RecordProcessStarted(\"order.metrics-demo\", 1) — invoked");

        ProcessDiagnostics.RecordProcessCompleted("order.metrics-demo", 1);
        Console.WriteLine("  ProcessDiagnostics.RecordProcessCompleted(\"order.metrics-demo\", 1) — invoked");

        ProcessDiagnostics.RecordProcessFailed("order.metrics-demo", 1, "timeout");
        Console.WriteLine("  ProcessDiagnostics.RecordProcessFailed(\"order.metrics-demo\", 1, \"timeout\") — invoked");

        ProcessDiagnostics.RecordProcessCompensated("order.metrics-demo", 1);
        Console.WriteLine("  ProcessDiagnostics.RecordProcessCompensated(\"order.metrics-demo\", 1) — invoked");

        ProcessDiagnostics.RecordConcurrencyConflict("order.metrics-demo", 1);
        Console.WriteLine("  ProcessDiagnostics.RecordConcurrencyConflict(\"order.metrics-demo\", 1) — invoked");

        ProcessDiagnostics.RecordTransitionDuration("order.metrics-demo", 12.5);
        Console.WriteLine("  ProcessDiagnostics.RecordTransitionDuration(\"order.metrics-demo\", 12.5) — invoked");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 07-B ProcessDiagnostics OpenTelemetry demo completed successfully.");
        Console.ResetColor();
    }
}
