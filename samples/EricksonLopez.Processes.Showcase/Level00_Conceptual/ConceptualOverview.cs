// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Showcase.Level00_Conceptual;

/// <summary>
/// Level 0: Conceptual Overview
/// Explains architectural axioms, design philosophy, and comparisons with alternative workflow technologies.
/// </summary>
public static class ConceptualOverview
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 00: CONCEPTUAL ARCHITECTURE & DESIGN PHILOSOPHY");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        Console.WriteLine(@"
1. WHAT IS ERICKSONLOPEZ.PROCESSES?
-----------------------------------
EricksonLopez.Processes is a zero-reflection, trimming-safe, and Native AOT-ready
Process Manager and Saga library for modern .NET 10+.

It provides high-performance, deterministic primitives for:
  • Modeling distributed business workflows and sagas as pure state transitions.
  • Enforcing Optimistic Concurrency Control (OCC) with atomic Revision CAS tokens.
  • Emitting side-effect intents (Commands, Events, Timeouts, Compensations) decoupled from network transports.
  • Orchestrating reverse-order (LIFO) saga compensations on failure.
  • Migrating schema versions deterministically over time.

2. CORE AXIOMS:
---------------
  Axiom 1: Model the Process, Not the Infrastructure.
           Workflows describe how business state evolves and what effects are intended.
           Core has 0 external infrastructure dependencies.

  Axiom 2: Persist the State, Not the Runtime.
           Workflows do not hold threads or stay in memory. Instances hydrate from durable
           storage on trigger, apply deterministic transitions, commit state with OCC, and exit.

  Axiom 3: Explicit Compensation Over Magical Rollback.
           Distributed transactions cannot be undone with ACID rollbacks. Compensations are
           explicit domain actions executed in reverse dependency order.

  Axiom 4: Native AOT & Trimming as First-Class Constraints.
           Zero reflection (no Activator.CreateInstance, no Assembly.GetTypes).
           Roslyn Source Generators produce compile-time static dispatch tables.

3. ARCHITECTURAL COMPARISON:
----------------------------
| Dimension               | EricksonLopez.Processes | Temporal / Cadence | MassTransit Sagas | Elsa / WorkflowCore |
| :---------------------- | :---------------------- | :----------------- | :----------------- | :------------------ |
| Reflection / AOT        | 100% Zero-Reflection    | Heavy Dynamic Gen  | Runtime Reflection | Runtime Dynamic     |
| Persistence Model       | Pure OCC CAS Revisions  | Event History / DB | EF Core / Mappings | Blob / DB Engine    |
| Transport Coupling      | Pure Effect Intents     | gRPC Server Daemon | MassTransit Bus    | In-Process / HTTP   |
| Memory Footprint        | Extremely Low (Bytes)   | High (Host daemon) | Medium             | High                |
| Reverse Compensation    | Native LIFO Sagas       | Custom Activities  | Custom State Mach. | Custom Activities   |
");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Conceptual overview completed.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
