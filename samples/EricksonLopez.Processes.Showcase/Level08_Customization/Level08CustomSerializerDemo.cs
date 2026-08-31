// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Showcase.Level08_Customization;

public sealed record CompactState(int Id, string Code, decimal Balance) : IProcessState;

/// <summary>
/// Custom implementation of IProcessStateSerializer using a zero-allocation UTF8 text format.
/// </summary>
public sealed class CompactFormatProcessStateSerializer : IProcessStateSerializer<CompactState>
{
    public byte[] Serialize(CompactState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        // Custom compact delimiter format: "Id|Code|Balance"
        var formatted = $"{state.Id.ToString(CultureInfo.InvariantCulture)}|{state.Code}|{state.Balance.ToString(CultureInfo.InvariantCulture)}";
        return Encoding.UTF8.GetBytes(formatted);
    }

    public CompactState Deserialize(ReadOnlySpan<byte> data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('|');
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid compact state payload: '{text}'");
        }

        return new CompactState(
            Id: int.Parse(parts[0], CultureInfo.InvariantCulture),
            Code: parts[1],
            Balance: decimal.Parse(parts[2], CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Level 8-B: Custom IProcessStateSerializer Implementation
/// </summary>
public static class Level08CustomSerializerDemo
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================================");
        Console.WriteLine(" LEVEL 08-B: CUSTOM IPROCESSSTATESERIALIZER EXTENSION");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        var serializer = new CompactFormatProcessStateSerializer();
        var originalState = new CompactState(42, "PRJ-ALPHA-01", 125000.50m);

        // 1. Serialize
        var bytes = serializer.Serialize(originalState);
        Console.WriteLine($"Original State: Id={originalState.Id}, Code='{originalState.Code}', Balance={originalState.Balance.ToString("C", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Serialized Byte Length: {bytes.Length} bytes");
        Console.WriteLine($"Serialized String View: '{Encoding.UTF8.GetString(bytes)}'");

        // 2. Deserialize
        var restoredState = serializer.Deserialize(bytes);
        Console.WriteLine($"\nRestored State: Id={restoredState.Id}, Code='{restoredState.Code}', Balance={restoredState.Balance.ToString("C", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Round-Trip Equality Check: {originalState == restoredState}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 08-B Custom Serializer demo completed successfully.");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
