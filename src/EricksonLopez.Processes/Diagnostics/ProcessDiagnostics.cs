// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes;

/// <summary>
/// Provides standard observability instruments for process execution and tracing.
/// </summary>
public static class ProcessDiagnostics
{
    /// <summary>
    /// The diagnostic telemetry source name.
    /// </summary>
    public const string SourceName = "EricksonLopez.Processes";

    /// <summary>
    /// The <see cref="ActivitySource"/> used for tracing process lifecycles, state transitions, and compensations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");

    /// <summary>
    /// The <see cref="Meter"/> used for emitting process execution metrics.
    /// </summary>
    public static readonly Meter Meter = new(SourceName, "1.0.0");

    private const string CountUnit = "count";
    private const string ProcessTypeTag = "process.type";
    private const string ProcessVersionTag = "process.version";

    private static readonly Counter<long> ProcessesStartedCounter =
        Meter.CreateCounter<long>("processes.started", CountUnit, "Total number of processes started");

    private static readonly Counter<long> ProcessesCompletedCounter =
        Meter.CreateCounter<long>("processes.completed", CountUnit, "Total number of processes successfully completed");

    private static readonly Counter<long> ProcessesFailedCounter =
        Meter.CreateCounter<long>("processes.failed", CountUnit, "Total number of processes that failed");

    private static readonly Counter<long> ProcessesCompensatedCounter =
        Meter.CreateCounter<long>("processes.compensated", CountUnit, "Total number of sagas successfully compensated");

    private static readonly Counter<long> ConcurrencyConflictsCounter =
        Meter.CreateCounter<long>("processes.concurrency_conflicts", CountUnit, "Total number of optimistic concurrency conflicts");

    private static readonly Histogram<double> TransitionDurationHistogram =
        Meter.CreateHistogram<double>("processes.transition.duration", "ms", "Duration of process state transitions in milliseconds");

    /// <summary>
    /// Records a metric event indicating that a process instance was started.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="version">The schema or definition version number.</param>
    public static void RecordProcessStarted(string processType, int version)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType },
            { ProcessVersionTag, version }
        };
        ProcessesStartedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a metric event indicating that a process instance completed successfully.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="version">The schema or definition version number.</param>
    public static void RecordProcessCompleted(string processType, int version)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType },
            { ProcessVersionTag, version }
        };
        ProcessesCompletedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a metric event indicating that a process instance failed.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="version">The schema or definition version number.</param>
    /// <param name="reason">The optional failure reason explanation.</param>
    public static void RecordProcessFailed(string processType, int version, string? reason)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType },
            { ProcessVersionTag, version },
            { "error.reason", reason }
        };
        ProcessesFailedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a metric event indicating that a saga successfully completed compensation.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="version">The schema or definition version number.</param>
    public static void RecordProcessCompensated(string processType, int version)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType },
            { ProcessVersionTag, version }
        };
        ProcessesCompensatedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a metric event indicating an optimistic concurrency conflict during state persistence.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="version">The schema or definition version number.</param>
    public static void RecordConcurrencyConflict(string processType, int version)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType },
            { ProcessVersionTag, version }
        };
        ConcurrencyConflictsCounter.Add(1, tags);
    }

    /// <summary>
    /// Records the execution duration of a process state transition.
    /// </summary>
    /// <param name="processType">The logical process type name.</param>
    /// <param name="durationMs">The elapsed duration in milliseconds.</param>
    public static void RecordTransitionDuration(string processType, double durationMs)
    {
        var tags = new TagList
        {
            { ProcessTypeTag, processType }
        };
        TransitionDurationHistogram.Record(durationMs, tags);
    }
}





