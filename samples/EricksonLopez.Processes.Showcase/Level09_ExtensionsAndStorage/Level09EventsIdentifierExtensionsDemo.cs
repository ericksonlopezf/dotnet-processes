// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Events;
using EventsCausationId = EricksonLopez.Events.Identifiers.CausationId;
using EventsCorrelationId = EricksonLopez.Events.Identifiers.CorrelationId;
using ProcessesCausationId = EricksonLopez.Processes.Abstractions.CausationId;
// Explicit aliases to disambiguate between the two Processes and Events namespaces
using ProcessesCorrelationId = EricksonLopez.Processes.Abstractions.CorrelationId;

namespace EricksonLopez.Processes.Showcase.Level09_ExtensionsAndStorage;

/// <summary>
/// Level 9-B: ProcessEventsIdentifierExtensions — Cross-Library Identifier Bridging
/// Demonstrates all four extension methods provided by
/// <see cref="ProcessEventsIdentifierExtensions"/> for converting identifiers
/// between the <c>EricksonLopez.Processes.Abstractions</c> namespace and the
/// <c>EricksonLopez.Events.Identifiers</c> namespace:
/// <list type="bullet">
///   <item><see cref="ProcessEventsIdentifierExtensions.ToEventsCorrelationId"/></item>
///   <item><see cref="ProcessEventsIdentifierExtensions.ToProcessesCorrelationId"/></item>
///   <item><see cref="ProcessEventsIdentifierExtensions.ToEventsCausationId"/></item>
///   <item><see cref="ProcessEventsIdentifierExtensions.ToProcessesCausationId"/></item>
/// </list>
/// </summary>
public static class Level09EventsIdentifierExtensionsDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 09-B: ProcessEventsIdentifierExtensions — CROSS-LIBRARY ID BRIDGING");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // -----------------------------------------------------------------------
        // Context: Two sibling libraries use separate identifier value objects.
        // EricksonLopez.Processes uses  CorrelationId / CausationId (Processes.Abstractions)
        // EricksonLopez.Events uses     CorrelationId / CausationId (Events.Identifiers)
        // ProcessEventsIdentifierExtensions bridges them without runtime reflection.
        // -----------------------------------------------------------------------

        Console.WriteLine("\n[Background]");
        Console.WriteLine("  EricksonLopez.Processes → EricksonLopez.Processes.Abstractions.CorrelationId");
        Console.WriteLine("  EricksonLopez.Events    → EricksonLopez.Events.Identifiers.CorrelationId");
        Console.WriteLine("  Both are distinct value types with the same .Value string property.");
        Console.WriteLine("  ProcessEventsIdentifierExtensions provides lossless, zero-allocation conversion.");

        // -----------------------------------------------------------------------
        // 1. ToEventsCorrelationId() — Processes → Events
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Extension 1] CorrelationId.ToEventsCorrelationId()");
        Console.WriteLine("  Signature: ProcessesCorrelationId → EventsCorrelationId");

        var processCorrelationId = ProcessesCorrelationId.From("TRANSACTION-TX-67890");
        EventsCorrelationId eventsCorrelationId = processCorrelationId.ToEventsCorrelationId();

        Console.WriteLine($"  Input  (Processes): CorrelationId.Value = '{processCorrelationId.Value}'");
        Console.WriteLine($"  Output (Events):    CorrelationId.Value = '{eventsCorrelationId.Value}'");
        Console.WriteLine($"  Values match: {processCorrelationId.Value == eventsCorrelationId.Value}");

        // -----------------------------------------------------------------------
        // 2. ToProcessesCorrelationId() — Events → Processes
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Extension 2] EventsCorrelationId.ToProcessesCorrelationId()");
        Console.WriteLine("  Signature: EventsCorrelationId → ProcessesCorrelationId");

        ProcessesCorrelationId roundTrippedCorrelation = eventsCorrelationId.ToProcessesCorrelationId();

        Console.WriteLine($"  Input  (Events):    CorrelationId.Value = '{eventsCorrelationId.Value}'");
        Console.WriteLine($"  Output (Processes): CorrelationId.Value = '{roundTrippedCorrelation.Value}'");
        Console.WriteLine($"  Round-trip identity: {processCorrelationId == roundTrippedCorrelation}");

        // -----------------------------------------------------------------------
        // 3. ToEventsCausationId() — Processes → Events
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Extension 3] CausationId.ToEventsCausationId()");
        Console.WriteLine("  Signature: ProcessesCausationId → EventsCausationId");

        var processCausationId = ProcessesCausationId.From("CMD-ORDER-CREATED-001");
        EventsCausationId eventsCausationId = processCausationId.ToEventsCausationId();

        Console.WriteLine($"  Input  (Processes): CausationId.Value = '{processCausationId.Value}'");
        Console.WriteLine($"  Output (Events):    CausationId.Value = '{eventsCausationId.Value}'");
        Console.WriteLine($"  Values match: {processCausationId.Value == eventsCausationId.Value}");

        // -----------------------------------------------------------------------
        // 4. ToProcessesCausationId() — Events → Processes
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Extension 4] EventsCausationId.ToProcessesCausationId()");
        Console.WriteLine("  Signature: EventsCausationId → ProcessesCausationId");

        ProcessesCausationId roundTrippedCausation = eventsCausationId.ToProcessesCausationId();

        Console.WriteLine($"  Input  (Events):    CausationId.Value = '{eventsCausationId.Value}'");
        Console.WriteLine($"  Output (Processes): CausationId.Value = '{roundTrippedCausation.Value}'");
        Console.WriteLine($"  Round-trip identity: {processCausationId == roundTrippedCausation}");

        // -----------------------------------------------------------------------
        // 5. Integration pattern — EventProcessDispatcher and IEventPublisher
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Integration Pattern] EventProcessDispatcher with identifier bridging");
        Console.WriteLine("  When using EventProcessDispatcher, the dispatcher automatically calls");
        Console.WriteLine("  IEventPublisher.PublishAsync() with the raw effect payload.");
        Console.WriteLine("  If your IEvent implementation carries a CorrelationId from the Events namespace,");
        Console.WriteLine("  use these extensions to propagate correlation context from the process manager:");
        Console.WriteLine();
        Console.WriteLine("  // Inside an IEventPublisher handler:");
        Console.WriteLine("  var processCorrelId = eventMessage.CorrelationId.ToProcessesCorrelationId();");
        Console.WriteLine("  // … pass processCorrelId to ProcessCoordinator.ExecuteAsync(…)");
        Console.WriteLine();
        Console.WriteLine("  // Inside a process handler emitting effects:");
        Console.WriteLine("  var eventsCorrelId = context.CorrelationId.ToEventsCorrelationId();");
        Console.WriteLine("  // … embed eventsCorrelId into the outbound IEvent payload");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 09-B ProcessEventsIdentifierExtensions demo completed successfully.");
        Console.ResetColor();

        return Task.CompletedTask;
    }
}
