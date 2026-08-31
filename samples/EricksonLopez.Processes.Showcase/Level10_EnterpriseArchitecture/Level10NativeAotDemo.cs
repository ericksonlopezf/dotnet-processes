// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EricksonLopez.Processes;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Generated;
using EricksonLopez.Processes.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Processes.Showcase.Level10_EnterpriseArchitecture;

public sealed record NativePaymentState(
    string PaymentId,
    decimal Amount,
    string Status) : IProcessState;

[JsonSerializable(typeof(NativePaymentState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(ProcessType))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(CausationId))]
[JsonSerializable(typeof(MessageId))]
internal sealed partial class ShowcaseAotJsonContext : JsonSerializerContext
{
}

[ProcessDefinition("native.payment.workflow", 1)]
public sealed class NativePaymentWorkflow : IProcess<NativePaymentState>
{
    public ProcessType Type => ProcessType.From("native.payment.workflow");
    public ProcessVersion Version => ProcessVersion.Initial;
}

/// <summary>
/// Level 10-B: Native AOT Compilation &amp; Roslyn Source Generator Integration
/// </summary>
public static class Level10NativeAotDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 10-B: NATIVE AOT & ROSLYN COMPILE-TIME SOURCE GENERATION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // 1. Demonstrate Roslyn Source Generated Registry
        var registry = GeneratedProcessRegistry.CreateRegistry();
        Console.WriteLine($"Discovered & Registered Processes at Compile-Time (Zero-Reflection):");
        foreach (var (type, version) in registry.RegisteredProcesses)
        {
            Console.WriteLine($"  • Process: '{type.Value}' (v{version.Value})");
        }

        // 2. Demonstrate Generated DI extension
        var services = new ServiceCollection();
        services.AddGeneratedProcesses();
        var sp = services.BuildServiceProvider();
        var resolvedRegistry = sp.GetRequiredService<IProcessRegistry>();

        var isRegistered = resolvedRegistry.IsRegistered(
            ProcessType.From("native.payment.workflow"),
            ProcessVersion.Initial);

        Console.WriteLine($"\nDI Resolved Registry verification ('native.payment.workflow' v1): {isRegistered}");

        // 3. Demonstrate 100% Reflection-Free AOT Serialization
        var serializer = new SystemTextJsonProcessStateSerializer<NativePaymentState>(
            ShowcaseAotJsonContext.Default.NativePaymentState);

        var state = new NativePaymentState("PAY-AOT-9988", 999.50m, "Authorized");
        var bytes = serializer.Serialize(state);
        var restored = serializer.Deserialize(bytes);

        Console.WriteLine($"\nAOT Serialized Byte Length: {bytes.Length} bytes");
        Console.WriteLine($"AOT Restored PaymentId:     {restored.PaymentId}");
        Console.WriteLine($"AOT Restored Status:        {restored.Status}");
        Console.WriteLine($"State Exact Match:          {state == restored}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✔ Level 10-B Native AOT & Source Generator demo completed successfully.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
