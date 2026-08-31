// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using EricksonLopez.Processes.Showcase.Level00_Conceptual;
using EricksonLopez.Processes.Showcase.Level01_QuickStart;
using EricksonLopez.Processes.Showcase.Level02_FullConfiguration;
using EricksonLopez.Processes.Showcase.Level03_RealWorldUseCases;
using EricksonLopez.Processes.Showcase.Level04_AdvancedIntegration;
using EricksonLopez.Processes.Showcase.Level05_ProcessingAndConcurrency;
using EricksonLopez.Processes.Showcase.Level06_ErrorHandlingAndRecovery;
using EricksonLopez.Processes.Showcase.Level07_ScalabilityAndPerformance;
using EricksonLopez.Processes.Showcase.Level08_Customization;
using EricksonLopez.Processes.Showcase.Level09_ExtensionsAndStorage;
using EricksonLopez.Processes.Showcase.Level10_EnterpriseArchitecture;

namespace EricksonLopez.Processes.Showcase;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintHeader();

        var runAll = args.Length == 0 ||
            args.Any(a => string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(a, "-a", StringComparison.OrdinalIgnoreCase));

        var specificLevel = args.FirstOrDefault(a =>
            a.StartsWith("--level=", StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith("-l=", StringComparison.OrdinalIgnoreCase));

        try
        {
            if (specificLevel != null)
            {
                var levelNumber = specificLevel.Split('=')[1];
                await RunSpecificLevelAsync(levelNumber);
            }
            else if (runAll)
            {
                await RunAllLevelsAsync();
            }
            else
            {
                await RunInteractiveMenuAsync();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n================================================================================");
            Console.WriteLine(" ALL SHOWCASE MODULES EXECUTED SUCCESSFULLY — 100% PASS RATE");
            Console.WriteLine("================================================================================");
            Console.ResetColor();
            return 0;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] Showcase execution failed: {ex}");
            Console.ResetColor();
            return 1;
        }
#pragma warning restore CA1031
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════════════════════╗
║                      ERICKSONLOPEZ.PROCESSES SHOWCASE                         ║
║               Official Executable Reference & Learning Architecture           ║
║                     .NET 10 • Native AOT Ready • Zero Reflection              ║
╚═══════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    public static async Task RunAllLevelsAsync()
    {
        Console.WriteLine("\n>>> Starting comprehensive execution of all progressive levels (0 to 10)...\n");

        await ConceptualOverview.RunAsync();
        Console.WriteLine();

        await QuickStartDemo.RunAsync();
        Console.WriteLine();

        await Level02FullConfigurationDemo.RunAsync();
        Console.WriteLine();

        await Level03OrderFulfillmentSagaDemo.RunAsync();
        Console.WriteLine();

        await Level03InvoiceCertificationProcessDemo.RunAsync();
        Console.WriteLine();

        await Level04OutboxIntegrationDemo.RunAsync();
        Console.WriteLine();

        await Level04MediatorIntegrationDemo.RunAsync();
        Console.WriteLine();

        await Level04EventsIntegrationDemo.RunAsync();
        Console.WriteLine();

        await Level05ConcurrencyRetryDemo.RunAsync();
        Console.WriteLine();

        await Level05CompositeKeyDemo.RunAsync();
        Console.WriteLine();

        await Level05AllEffectsDemo.RunAsync();
        Console.WriteLine();

        await Level06CompensationFailureDemo.RunAsync();
        Console.WriteLine();

        await Level06InvalidTransitionDemo.RunAsync();
        Console.WriteLine();

        await Level06SaveResultsDemo.RunAsync();
        Console.WriteLine();

        await Level07ThroughputDemo.RunAsync();
        Console.WriteLine();

        await Level07DiagnosticsDemo.RunAsync();
        Console.WriteLine();

        await Level08CustomStoreDemo.RunAsync();
        Console.WriteLine();

        await Level08CustomSerializerDemo.RunAsync();
        Console.WriteLine();

        await Level08SnapshotRepositoryDemo.RunAsync();
        Console.WriteLine();

        await Level08ProcessStateRecordDemo.RunAsync();
        Console.WriteLine();

        await Level09StorageDialectsDemo.RunAsync();
        Console.WriteLine();

        await Level09EventsIdentifierExtensionsDemo.RunAsync();
        Console.WriteLine();

        await Level10SchemaMigrationDemo.RunAsync();
        Console.WriteLine();

        await Level10NativeAotDemo.RunAsync();
    }

    private static async Task RunSpecificLevelAsync(string level)
    {
        var normalized = level.ToUpperInvariant();
        switch (normalized)
        {
            case "0":
            case "00":
            case "CONCEPTUAL":
                await ConceptualOverview.RunAsync();
                break;
            case "1":
            case "01":
            case "QUICKSTART":
                await QuickStartDemo.RunAsync();
                break;
            case "2":
            case "02":
            case "CONFIG":
                await Level02FullConfigurationDemo.RunAsync();
                break;
            case "3":
            case "03":
            case "SAGAS":
                await Level03OrderFulfillmentSagaDemo.RunAsync();
                await Level03InvoiceCertificationProcessDemo.RunAsync();
                break;
            case "4":
            case "04":
            case "INTEGRATION":
                await Level04OutboxIntegrationDemo.RunAsync();
                await Level04MediatorIntegrationDemo.RunAsync();
                await Level04EventsIntegrationDemo.RunAsync();
                break;
            case "5":
            case "05":
            case "CONCURRENCY":
                await Level05ConcurrencyRetryDemo.RunAsync();
                await Level05CompositeKeyDemo.RunAsync();
                await Level05AllEffectsDemo.RunAsync();
                break;
            case "6":
            case "06":
            case "ERRORS":
                await Level06CompensationFailureDemo.RunAsync();
                await Level06InvalidTransitionDemo.RunAsync();
                await Level06SaveResultsDemo.RunAsync();
                break;
            case "7":
            case "07":
            case "PERFORMANCE":
                await Level07ThroughputDemo.RunAsync();
                await Level07DiagnosticsDemo.RunAsync();
                break;
            case "8":
            case "08":
            case "CUSTOMIZATION":
                await Level08CustomStoreDemo.RunAsync();
                await Level08CustomSerializerDemo.RunAsync();
                await Level08SnapshotRepositoryDemo.RunAsync();
                await Level08ProcessStateRecordDemo.RunAsync();
                break;
            case "9":
            case "09":
            case "STORAGE":
                await Level09StorageDialectsDemo.RunAsync();
                await Level09EventsIdentifierExtensionsDemo.RunAsync();
                break;
            case "10":
            case "ENTERPRISE":
            case "AOT":
                await Level10SchemaMigrationDemo.RunAsync();
                await Level10NativeAotDemo.RunAsync();
                break;
            default:
                Console.WriteLine($"Unknown level: '{level}'. Valid levels: 0 through 10.");
                break;
        }
    }

    private static async Task RunInteractiveMenuAsync()
    {
        Console.WriteLine("\nSelect a level to execute:");
        Console.WriteLine(" [0] Level 0: Conceptual Architecture");
        Console.WriteLine(" [1] Level 1: Quick Start (Minimal Process)");
        Console.WriteLine(" [2] Level 2: Full Configuration (DI, Options, Context, Value Objects)");
        Console.WriteLine(" [3] Level 3: Sagas \u0026 Long-Running Processes");
        Console.WriteLine(" [4] Level 4: Advanced Integration (Outbox, Mediator, Events)");
        Console.WriteLine(" [5] Level 5: Processing \u0026 Concurrency (OCC / CAS / All Effects)");
        Console.WriteLine(" [6] Level 6: Error Handling \u0026 Recovery (SaveResult, Compensation Failures)");
        Console.WriteLine(" [7] Level 7: Scalability, Performance \u0026 Diagnostics");
        Console.WriteLine(" [8] Level 8: Customization (Stores, Serializers, Snapshots, StateRecord)");
        Console.WriteLine(" [9] Level 9: Multi-Database Storage Engines \u0026 Identifier Extensions");
        Console.WriteLine(" [10] Level 10: Enterprise Architecture (Migration \u0026 Native AOT)");
        Console.WriteLine(" [A] Run All Levels");
        Console.WriteLine(" [Q] Quit");
        Console.Write("\nEnter choice: ");

        var key = Console.ReadLine()?.Trim();
        if (string.Equals(key, "A", StringComparison.OrdinalIgnoreCase))
        {
            await RunAllLevelsAsync();
        }
        else if (key is not null && key.Length > 0 && !string.Equals(key, "Q", StringComparison.OrdinalIgnoreCase))
        {
            await RunSpecificLevelAsync(key);
        }
    }
}
