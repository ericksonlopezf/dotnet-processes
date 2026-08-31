// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Events.Contracts;
using EricksonLopez.Events.Identifiers;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Events;

namespace EricksonLopez.Processes.Showcase.Level04_AdvancedIntegration;

public sealed record SampleDomainEvent(string EventName, Guid AggregateId) : IDomainEvent
{
    public EventId Id { get; init; } = EventId.New();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class InMemoryEventPublisher : IEventPublisher
{
    public List<IEvent> PublishedEvents { get; } = new();

    public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        PublishedEvents.Add(@event);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Level 4-C: Domain Event Bus Integration &amp; Identifier Mapping
/// </summary>
public static class Level04EventsIntegrationDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 04-C: EVENT BUS INTEGRATION & IDENTIFIER EXTENSIONS (IEventProcessDispatcher)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Demonstrate Identifier Extensions
        var procCorrId = EricksonLopez.Processes.Abstractions.CorrelationId.From("TX-CORR-12345");
        var eventCorrId = procCorrId.ToEventsCorrelationId();
        var roundTripProcCorrId = eventCorrId.ToProcessesCorrelationId();

        var procCauseId = EricksonLopez.Processes.Abstractions.CausationId.From("CAUSE-98765");
        var eventCauseId = procCauseId.ToEventsCausationId();
        var roundTripProcCauseId = eventCauseId.ToProcessesCausationId();

        Console.WriteLine($"Processes CorrelationId: '{procCorrId.Value}' -> Events CorrelationId: '{eventCorrId.Value}' (Matches: {procCorrId == roundTripProcCorrId})");
        Console.WriteLine($"Processes CausationId:   '{procCauseId.Value}' -> Events CausationId:   '{eventCauseId.Value}' (Matches: {procCauseId == roundTripProcCauseId})");

        // 2. Dispatch domain events via EventProcessDispatcher
        var publisher = new InMemoryEventPublisher();
        var dispatcher = new EventProcessDispatcher(publisher);
        var processId = ProcessId.NewId();

        var domainEvent = new SampleDomainEvent("OrderPaymentCaptured", processId.Value);
        var effects = new ProcessEffect[]
        {
            ProcessEffect.CreateEvent(domainEvent)
        };

        Console.WriteLine($"\nDispatching {effects.Length} ProcessEffects via EventProcessDispatcher...");
        await dispatcher.DispatchEffectsAsync(effects, processId);

        Console.WriteLine($"Published Domain Events Count: {publisher.PublishedEvents.Count}");
        Console.WriteLine($"Published Event ID:            {publisher.PublishedEvents[0].Id.Value}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 04-C Event Bus integration completed successfully.");
        Console.ResetColor();
    }
}
