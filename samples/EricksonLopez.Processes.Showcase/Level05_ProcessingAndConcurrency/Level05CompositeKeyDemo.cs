// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level05_ProcessingAndConcurrency;

/// <summary>
/// Level 5-B: Composite Correlation Keys &amp; Deterministic CorrelationId Extraction
/// </summary>
public static class Level05CompositeKeyDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 05-B: COMPOSITE CORRELATION KEYS & DETERMINISTIC UUID MAPPING");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Construct multi-part composite keys from business parameters
        var tenantId = "TENANT-CORP-99";
        var invoiceNumber = "INV-2026-004412";
        var fiscalYear = 2026;

        var key2Parts = CompositeCorrelationKey.From(tenantId, invoiceNumber);
        var key3Parts = CompositeCorrelationKey.From(tenantId, fiscalYear, invoiceNumber);

        Console.WriteLine($"2-Part Key Normalized Value: '{key2Parts.Value}'");
        Console.WriteLine($"3-Part Key Normalized Value: '{key3Parts.Value}'");

        // 2. Generate deterministic UUIDv5-style CorrelationId from composite key
        var correlationId1 = key3Parts.ToCorrelationId();
        var correlationId2 = key3Parts.ToCorrelationId();

        Console.WriteLine();
        Console.WriteLine($"Deterministic CorrelationId: '{correlationId1.Value}'");
        Console.WriteLine($"Deterministic Repeatability Check (id1 == id2): {correlationId1 == correlationId2}");

        // 3. Different key parts produce distinct deterministic CorrelationIds
        var otherKey = CompositeCorrelationKey.From(tenantId, fiscalYear, "INV-2026-004413");
        var otherCorrelationId = otherKey.ToCorrelationId();
        Console.WriteLine($"Distinct Key CorrelationId:   '{otherCorrelationId.Value}'");
        Console.WriteLine($"Different Keys Distinctness: {correlationId1 != otherCorrelationId}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 05-B Composite Correlation Key demo completed successfully.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
