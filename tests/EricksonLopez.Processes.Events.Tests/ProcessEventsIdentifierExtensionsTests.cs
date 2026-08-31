// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Events;
using Xunit;

namespace EricksonLopez.Processes.Events.Tests;

public class ProcessEventsIdentifierExtensionsTests
{
    [Fact]
    public void CorrelationId_EventsInterop_ShouldConvertCorrectly()
    {
        var processesCorr = new CorrelationId("corr-123");
        var eventsCorr = processesCorr.ToEventsCorrelationId();

        eventsCorr.Value.Should().Be("corr-123");

        var convertedBack = eventsCorr.ToProcessesCorrelationId();
        convertedBack.Value.Should().Be("corr-123");
        convertedBack.Should().Be(processesCorr);
    }

    [Fact]
    public void CausationId_EventsInterop_ShouldConvertCorrectly()
    {
        var processesCause = new CausationId("cause-456");
        var eventsCause = processesCause.ToEventsCausationId();

        eventsCause.Value.Should().Be("cause-456");

        var convertedBack = eventsCause.ToProcessesCausationId();
        convertedBack.Value.Should().Be("cause-456");
        convertedBack.Should().Be(processesCause);
    }
}
