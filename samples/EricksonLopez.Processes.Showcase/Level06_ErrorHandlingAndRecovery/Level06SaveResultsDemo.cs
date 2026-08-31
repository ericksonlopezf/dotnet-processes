// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Testing;

namespace EricksonLopez.Processes.Showcase.Level06_ErrorHandlingAndRecovery;

// ---------------------------------------------------------------------------
// Domain model for save-result demonstration
// ---------------------------------------------------------------------------

public sealed record InventoryState(string ProductId, int Quantity) : IProcessState;

/// <summary>
/// Level 6-C: All ProcessSaveResult Values — NotFound, PersistenceError, ConcurrencyConflict, Success
/// Demonstrates how to use <see cref="FaultInjectingProcessStore{TState}"/> to simulate each
/// <see cref="ProcessSaveResult"/> outcome and shows the recommended handling pattern.
/// </summary>
public static class Level06SaveResultsDemo
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 06-C: ALL PROCESSSAVERESULT VALUES (NotFound, PersistenceError, Success)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // -----------------------------------------------------------------------
        // Case 1: ProcessSaveResult.Success (baseline — already covered in Level05)
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 1] ProcessSaveResult.Success — normal write succeeds");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();
            var processId = ProcessId.NewId();
            var correlationId = CorrelationId.NewId();
            var instance = ProcessInstance<InventoryState>.Create(
                id: processId,
                type: ProcessType.From("inventory.check"),
                version: ProcessVersion.Initial,
                correlationId: correlationId,
                initialState: new InventoryState("SKU-001", 100),
                now: DateTimeOffset.UtcNow);

            var result = await store.SaveAsync(instance);
            Console.WriteLine($"  SaveAsync result: {result}");   // Expected: Success
            Console.WriteLine($"  IsSuccess:        {result == ProcessSaveResult.Success}");
        }

        // -----------------------------------------------------------------------
        // Case 2: ProcessSaveResult.ConcurrencyConflict — revision mismatch
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 2] ProcessSaveResult.ConcurrencyConflict — optimistic lock failure");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();

            // Force 1 conflict then succeed
            store.ConcurrencyConflictsToSimulate = 1;

            var processId = ProcessId.NewId();
            var instance = ProcessInstance<InventoryState>.Create(
                id: processId,
                type: ProcessType.From("inventory.check"),
                version: ProcessVersion.Initial,
                correlationId: CorrelationId.NewId(),
                initialState: new InventoryState("SKU-002", 50),
                now: DateTimeOffset.UtcNow);

            var firstResult = await store.SaveAsync(instance);
            Console.WriteLine($"  First SaveAsync:  {firstResult}");   // Expected: ConcurrencyConflict

            // Retry pattern: re-read state and re-apply transition
            store.ConcurrencyConflictsToSimulate = 0;
            // (re-read from store would be needed in real case; simulated here for clarity)
            var retryResult = await store.SaveAsync(instance);
            Console.WriteLine($"  Retry SaveAsync:  {retryResult}");   // Expected: Success
        }

        // -----------------------------------------------------------------------
        // Case 3: ProcessSaveResult.NotFound — attempted update on missing record
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 3] ProcessSaveResult.NotFound — forced via FaultInjectingProcessStore");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();

            // Force the next SaveAsync to return NotFound regardless
            store.ForcedSaveResult = ProcessSaveResult.NotFound;

            var processId = ProcessId.NewId();
            var instance = ProcessInstance<InventoryState>.Create(
                id: processId,
                type: ProcessType.From("inventory.check"),
                version: ProcessVersion.Initial,
                correlationId: CorrelationId.NewId(),
                initialState: new InventoryState("SKU-003", 25),
                now: DateTimeOffset.UtcNow);

            var result = await store.SaveAsync(instance);
            Console.WriteLine($"  SaveAsync result: {result}");   // Expected: NotFound

            // Recommended handling pattern
            if (result == ProcessSaveResult.NotFound)
            {
                Console.WriteLine("  → Handling: Process not found in storage. Re-initiation or dead-letter required.");
            }

            // Reset for further demos
            store.ForcedSaveResult = null;
        }

        // -----------------------------------------------------------------------
        // Case 4: ProcessSaveResult.PersistenceError — storage-level failure
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 4] ProcessSaveResult.PersistenceError — forced via FaultInjectingProcessStore");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();

            // Force the next SaveAsync to return PersistenceError
            store.ForcedSaveResult = ProcessSaveResult.PersistenceError;

            var processId = ProcessId.NewId();
            var instance = ProcessInstance<InventoryState>.Create(
                id: processId,
                type: ProcessType.From("inventory.check"),
                version: ProcessVersion.Initial,
                correlationId: CorrelationId.NewId(),
                initialState: new InventoryState("SKU-004", 10),
                now: DateTimeOffset.UtcNow);

            var result = await store.SaveAsync(instance);
            Console.WriteLine($"  SaveAsync result: {result}");   // Expected: PersistenceError

            // Recommended handling pattern
            if (result == ProcessSaveResult.PersistenceError)
            {
                Console.WriteLine("  → Handling: Infrastructure-level failure. Log, alert, and schedule retry with circuit breaker.");
            }

            store.ForcedSaveResult = null;
        }

        // -----------------------------------------------------------------------
        // Case 5: ExceptionToThrowOnSave — transient infrastructure exception
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 5] Exception injection on SaveAsync — infrastructure fault pattern");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();

            // Inject a transient IO exception
            store.ExceptionToThrowOnSave = new InvalidOperationException("Database connection pool exhausted.");

            var processId = ProcessId.NewId();
            var instance = ProcessInstance<InventoryState>.Create(
                id: processId,
                type: ProcessType.From("inventory.check"),
                version: ProcessVersion.Initial,
                correlationId: CorrelationId.NewId(),
                initialState: new InventoryState("SKU-005", 5),
                now: DateTimeOffset.UtcNow);

            try
            {
                await store.SaveAsync(instance);
                Console.WriteLine("  (unexpected: no exception)");
            }
#pragma warning disable CA1031
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  Caught expected exception: {ex.Message}");
                Console.WriteLine("  → Handling: Transient infrastructure error. Apply exponential backoff and retry.");
            }
#pragma warning restore CA1031

            store.ExceptionToThrowOnSave = null;
        }

        // -----------------------------------------------------------------------
        // Case 6: ExistsAsync — ForcedExistsResult demonstration
        // -----------------------------------------------------------------------
        Console.WriteLine("\n[Case 6] FaultInjectingProcessStore.ExistsAsync — forced result injection");
        {
            var store = new FaultInjectingProcessStore<InventoryState>();
            var processId = ProcessId.NewId();

            // Force ExistsAsync to return true even though nothing was written
            store.ForcedExistsResult = true;
            var forcedExists = await store.ExistsAsync(processId);
            Console.WriteLine($"  ForcedExistsResult=true → ExistsAsync: {forcedExists}");

            store.ForcedExistsResult = false;
            var forcedNotExists = await store.ExistsAsync(processId);
            Console.WriteLine($"  ForcedExistsResult=false → ExistsAsync: {forcedNotExists}");
        }

        // -----------------------------------------------------------------------
        // Summary: ProcessSaveResult enum values
        // -----------------------------------------------------------------------
        Console.WriteLine("\n  All ProcessSaveResult values:");
        foreach (var value in Enum.GetValues<ProcessSaveResult>())
        {
            Console.WriteLine($"    {(int)value} = ProcessSaveResult.{value}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 06-C All ProcessSaveResult Handling demo completed successfully.");
        Console.ResetColor();
    }
}
