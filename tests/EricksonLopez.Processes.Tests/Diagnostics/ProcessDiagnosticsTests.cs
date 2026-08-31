// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Processes.Tests.Diagnostics;

[Trait("Category", "Unit")]
public class ProcessDiagnosticsTests
{
    [Fact]
    public void ProcessDiagnostics_Instruments_ShouldBeInitialized()
    {
        ProcessDiagnostics.SourceName.Should().Be("EricksonLopez.Processes");
        ProcessDiagnostics.ActivitySource.Name.Should().Be("EricksonLopez.Processes");
        ProcessDiagnostics.ActivitySource.Version.Should().Be("1.0.0");
        ProcessDiagnostics.Meter.Name.Should().Be("EricksonLopez.Processes");
        ProcessDiagnostics.Meter.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void RecordProcessStarted_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.started")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.started")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordProcessStarted("order.process", 2);

        unit.Should().Be("count");
        description.Should().Be("Total number of processes started");
        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 2);
    }

    [Fact]
    public void RecordProcessCompleted_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.completed")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.completed")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordProcessCompleted("order.process", 3);

        unit.Should().Be("count");
        description.Should().Be("Total number of processes successfully completed");
        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 3);
    }

    [Fact]
    public void RecordProcessFailed_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.failed")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.failed")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordProcessFailed("order.process", 1, "Payment timeout");

        unit.Should().Be("count");
        description.Should().Be("Total number of processes that failed");
        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 1);
        capturedTags.Should().Contain(t => t.Key == "error.reason" && (string)t.Value! == "Payment timeout");
    }

    [Fact]
    public void RecordProcessFailed_WithNullReason_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.failed")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.failed")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordProcessFailed("order.process", 1, null);

        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 1);
        capturedTags.Should().Contain(t => t.Key == "error.reason" && t.Value == null);
    }

    [Fact]
    public void RecordProcessCompensated_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.compensated")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.compensated")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordProcessCompensated("travel.saga", 4);

        unit.Should().Be("count");
        description.Should().Be("Total number of sagas successfully compensated");
        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "travel.saga");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 4);
    }

    [Fact]
    public void RecordConcurrencyConflict_ShouldEmitCorrectMetricAndTags()
    {
        long capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.concurrency_conflicts")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.concurrency_conflicts")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordConcurrencyConflict("order.process", 2);

        unit.Should().Be("count");
        description.Should().Be("Total number of optimistic concurrency conflicts");
        capturedValue.Should().Be(1);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
        capturedTags.Should().Contain(t => t.Key == "process.version" && (int)t.Value! == 2);
    }

    [Fact]
    public void RecordTransitionDuration_ShouldEmitCorrectHistogramAndTags()
    {
        double capturedValue = 0;
        KeyValuePair<string, object?>[]? capturedTags = null;
        string? unit = null;
        string? description = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == ProcessDiagnostics.SourceName && instrument.Name == "processes.transition.duration")
            {
                unit = instrument.Unit;
                description = instrument.Description;
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "processes.transition.duration")
            {
                capturedValue = measurement;
                capturedTags = tags.ToArray();
            }
        });
        listener.Start();

        ProcessDiagnostics.RecordTransitionDuration("order.process", 54.3);

        unit.Should().Be("ms");
        description.Should().Be("Duration of process state transitions in milliseconds");
        capturedValue.Should().Be(54.3);
        capturedTags.Should().NotBeNull();
        capturedTags.Should().Contain(t => t.Key == "process.type" && (string)t.Value! == "order.process");
    }
}






