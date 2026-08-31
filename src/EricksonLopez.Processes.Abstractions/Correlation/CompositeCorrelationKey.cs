// Copyright © Erickson Lopez. MIT License.
using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a composite, deterministic correlation key constructed from multiple business identifiers.
/// </summary>
public readonly record struct CompositeCorrelationKey : IEquatable<CompositeCorrelationKey>
{
    private const string Separator = ":";

    /// <summary>
    /// Gets the normalized combined key string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCorrelationKey"/> struct with the specified key parts.
    /// </summary>
    /// <param name="parts">The key parts to combine.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parts"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="parts"/> is empty or contains elements that are <see langword="null"/> or white-space</exception>
    public CompositeCorrelationKey(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Length == 0)
        {
            throw new ArgumentException("At least one key part must be provided.", nameof(parts));
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
            {
                throw new ArgumentException($"Key part at index {i} cannot be null or whitespace.", nameof(parts));
            }
        }

        Value = string.Join(Separator, parts);
    }

    /// <summary>
    /// Creates a new composite correlation key from two strongly typed parts.
    /// </summary>
    /// <typeparam name="T1">The type of the first key part.</typeparam>
    /// <typeparam name="T2">The type of the second key part.</typeparam>
    /// <param name="part1">The first key part.</param>
    /// <param name="part2">The second key part.</param>
    /// <returns>A new <see cref="CompositeCorrelationKey"/> instance.</returns>
    public static CompositeCorrelationKey From<T1, T2>(T1 part1, T2 part2)
        where T1 : notnull
        where T2 : notnull =>
        new(part1.ToString()!, part2.ToString()!);

    /// <summary>
    /// Creates a new composite correlation key from three strongly typed parts.
    /// </summary>
    /// <typeparam name="T1">The type of the first key part.</typeparam>
    /// <typeparam name="T2">The type of the second key part.</typeparam>
    /// <typeparam name="T3">The type of the third key part.</typeparam>
    /// <param name="part1">The first key part.</param>
    /// <param name="part2">The second key part.</param>
    /// <param name="part3">The third key part.</param>
    /// <returns>A new <see cref="CompositeCorrelationKey"/> instance.</returns>
    public static CompositeCorrelationKey From<T1, T2, T3>(T1 part1, T2 part2, T3 part3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        new(part1.ToString()!, part2.ToString()!, part3.ToString()!);

    /// <summary>
    /// Creates a new composite correlation key from four strongly typed parts.
    /// </summary>
    /// <typeparam name="T1">The type of the first key part.</typeparam>
    /// <typeparam name="T2">The type of the second key part.</typeparam>
    /// <typeparam name="T3">The type of the third key part.</typeparam>
    /// <typeparam name="T4">The type of the fourth key part.</typeparam>
    /// <param name="part1">The first key part.</param>
    /// <param name="part2">The second key part.</param>
    /// <param name="part3">The third key part.</param>
    /// <param name="part4">The fourth key part.</param>
    /// <returns>A new <see cref="CompositeCorrelationKey"/> instance.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters", Justification = "Overload provided for 4 composite key parts")]
    public static CompositeCorrelationKey From<T1, T2, T3, T4>(T1 part1, T2 part2, T3 part3, T4 part4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull =>
        new(part1.ToString()!, part2.ToString()!, part3.ToString()!, part4.ToString()!);

    /// <summary>
    /// Converts this composite correlation key into a deterministic <see cref="CorrelationId"/>.
    /// </summary>
    /// <returns>A deterministic <see cref="CorrelationId"/> generated via SHA-256 hashing.</returns>
    public CorrelationId ToCorrelationId()
    {
        var bytes = Encoding.UTF8.GetBytes(Value);
        var hash = SHA256.HashData(bytes);

        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);

        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        var deterministicGuid = new Guid(guidBytes);
        return CorrelationId.From(deterministicGuid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
