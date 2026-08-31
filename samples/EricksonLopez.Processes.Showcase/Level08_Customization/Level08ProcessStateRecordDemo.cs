// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level08_Customization;

// ---------------------------------------------------------------------------
// Domain model used for ProcessStateRecord mapping
// ---------------------------------------------------------------------------

public sealed record ShipmentState(
    string ShipmentId,
    string Destination,
    string Carrier,
    bool IsDelivered) : IProcessState;

/// <summary>
/// Level 8-D: ProcessStateRecord — Flat DTO for Raw Database Storage Engines
/// Demonstrates the role of <see cref="ProcessStateRecord"/> as the flat database-level DTO
/// used by storage adapters to persist and read <see cref="ProcessInstance{TState}"/> data.
/// Shows all required and optional properties and explains each field's storage role.
/// </summary>
public static class Level08ProcessStateRecordDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 08-D: ProcessStateRecord — FLAT DTO FOR RAW STORAGE ENGINES");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        await Task.CompletedTask; // synchronous demo, Task for consistency

        // -----------------------------------------------------------------------
        // 1. Construct a ProcessStateRecord as a storage adapter would
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 1] Constructing ProcessStateRecord (as done by storage adapters)");

        var now = DateTimeOffset.UtcNow;
        var record = new ProcessStateRecord
        {
            // Primary key — stored as string representation of Guid
            ProcessId = ProcessId.NewId().Value.ToString(),

            // Logical process type — stored as string (e.g. "shipment.fulfillment")
            ProcessType = "shipment.fulfillment",

            // Schema version — integer persisted as string
            Version = "1",

            // Lifecycle status — integer (0=Initialized, 1=Running, ..., 6=Failed)
            Status = (int)ProcessStatus.Running,

            // OCC revision token — long integer for Compare-And-Swap
            Revision = Revision.Initial.Value,

            // Business identifier — used for cross-aggregate lookups
            CorrelationId = CorrelationId.NewId().Value,

            // Serialized JSON/JSONB payload — produced by IProcessStateSerializer
            StatePayload = "{\"ShipmentId\":\"SHP-001\",\"Destination\":\"New York\",\"Carrier\":\"FedEx\",\"IsDelivered\":false}",

            // Audit timestamps
            CreatedAt = now,
            UpdatedAt = now,

            // Terminal timestamp — null while process is still active
            CompletedAt = null
        };

        // -----------------------------------------------------------------------
        // 2. Print all ProcessStateRecord properties
        // -----------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  ProcessStateRecord fields:");
        Console.WriteLine($"    ProcessId     = '{record.ProcessId}'");
        Console.WriteLine($"    ProcessType   = '{record.ProcessType}'");
        Console.WriteLine($"    Version       = '{record.Version}'");
        Console.WriteLine($"    Status        = {record.Status} ({(ProcessStatus)record.Status})");
        Console.WriteLine($"    Revision      = {record.Revision}");
        Console.WriteLine($"    CorrelationId = '{record.CorrelationId}'");
        Console.WriteLine($"    StatePayload  = '{record.StatePayload}'");
        Console.WriteLine($"    CreatedAt     = {record.CreatedAt:O}");
        Console.WriteLine($"    UpdatedAt     = {record.UpdatedAt:O}");
        Console.WriteLine($"    CompletedAt   = {(record.CompletedAt.HasValue ? record.CompletedAt.Value.ToString("O") : "null (process still active)")}");

        // -----------------------------------------------------------------------
        // 3. Simulate mapping ProcessInstance → ProcessStateRecord (adapter pattern)
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 2] ProcessInstance → ProcessStateRecord (mapping pattern used by stores)");
        Console.WriteLine("  Storage adapters (PostgreSqlProcessStore, SqlServerProcessStore, etc.)");
        Console.WriteLine("  convert ProcessInstance<TState> → flat DB columns → ProcessStateRecord");
        Console.WriteLine();
        Console.WriteLine("  Adapter mapping pattern (pseudocode):");
        Console.WriteLine("    var payloadBytes = serializer.Serialize(instance.State);");
        Console.WriteLine("    var record = new ProcessStateRecord");
        Console.WriteLine("    {");
        Console.WriteLine("        ProcessId     = instance.Id.Value.ToString(),");
        Console.WriteLine("        ProcessType   = instance.Type.Value,");
        Console.WriteLine("        Version       = instance.Version.Value.ToString(),");
        Console.WriteLine("        Status        = (int)instance.Status,");
        Console.WriteLine("        Revision      = instance.Revision.Value,");
        Console.WriteLine("        CorrelationId = instance.CorrelationId.Value,");
        Console.WriteLine("        StatePayload  = Encoding.UTF8.GetString(payloadBytes),");
        Console.WriteLine("        CreatedAt     = instance.CreatedAt,");
        Console.WriteLine("        UpdatedAt     = instance.UpdatedAt,");
        Console.WriteLine("        CompletedAt   = instance.CompletedAt");
        Console.WriteLine("    };");

        // -----------------------------------------------------------------------
        // 4. Simulate mapping ProcessStateRecord → ProcessInstance (read path)
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 3] ProcessStateRecord → ProcessInstance (read path mapping)");
        Console.WriteLine("  Reverse mapping used by GetByIdAsync() / GetByCorrelationIdAsync():");
        Console.WriteLine();
        Console.WriteLine("    var id            = new ProcessId(Guid.Parse(record.ProcessId));");
        Console.WriteLine("    var type          = ProcessType.From(record.ProcessType);");
        Console.WriteLine("    var version       = new ProcessVersion(int.Parse(record.Version));");
        Console.WriteLine("    var status        = (ProcessStatus)record.Status;");
        Console.WriteLine("    var revision      = Revision.From(record.Revision);");
        Console.WriteLine("    var correlationId = CorrelationId.From(record.CorrelationId);");
        Console.WriteLine("    var payloadBytes  = Encoding.UTF8.GetBytes(record.StatePayload);");
        Console.WriteLine("    var state         = serializer.Deserialize(payloadBytes);");
        Console.WriteLine("    var instance      = new ProcessInstance<TState>(");
        Console.WriteLine("        id, type, version, status, revision, correlationId,");
        Console.WriteLine("        record.CreatedAt, record.UpdatedAt, record.CompletedAt, state);");

        // -----------------------------------------------------------------------
        // 5. CompletedAt — terminal processes
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Step 4] CompletedAt — set only when process reaches a terminal status");
        var terminalRecord = record with
        {
            Status = (int)ProcessStatus.Completed,
            Revision = record.Revision + 1,
            UpdatedAt = now.AddMinutes(5),
            CompletedAt = now.AddMinutes(5)
        };
        Console.WriteLine($"  Terminal record Status:      {terminalRecord.Status} ({(ProcessStatus)terminalRecord.Status})");
        Console.WriteLine($"  Terminal record CompletedAt: {terminalRecord.CompletedAt:O}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 08-D ProcessStateRecord demo completed successfully.");
        Console.ResetColor();
    }
}
