// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a strongly typed, immutable identifier for a process instance.
/// </summary>
public readonly record struct ProcessId : IComparable<ProcessId>, IComparable, ISpanFormattable, ISpanParsable<ProcessId>
{
    /// <summary>
    /// Gets the underlying <see cref="Guid"/> value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessId"/> struct with the specified <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The underlying unique identifier.</param>
    public ProcessId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets an empty <see cref="ProcessId"/>.
    /// </summary>
    public static ProcessId Empty => new(Guid.Empty);

    /// <summary>
    /// Creates a new <see cref="ProcessId"/> from a time-ordered UUIDv7.
    /// </summary>
    /// <returns>A new unique <see cref="ProcessId"/>.</returns>
    public static ProcessId NewId() => new(Guid.CreateVersion7());

    /// <summary>
    /// Creates a <see cref="ProcessId"/> from an existing <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The underlying unique identifier GUID.</param>
    /// <returns>A new <see cref="ProcessId"/> instance.</returns>
    public static ProcessId From(Guid value) => new(value);

    /// <summary>
    /// Creates a <see cref="ProcessId"/> from an existing <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The underlying unique identifier GUID.</param>
    /// <returns>A new <see cref="ProcessId"/> instance.</returns>
    public static ProcessId FromGuid(Guid value) => new(value);

    /// <summary>
    /// Converts this instance to its underlying <see cref="Guid"/> value.
    /// </summary>
    /// <returns>The underlying <see cref="Guid"/> value.</returns>
    public Guid ToGuid() => Value;

    /// <summary>
    /// Parses a string into a <see cref="ProcessId"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="ProcessId"/>.</returns>
    public static ProcessId Parse(string s) => Parse(s, null);

    /// <inheritdoc />
    public static ProcessId Parse(string s, IFormatProvider? provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s);
        return new ProcessId(Guid.Parse(s, provider));
    }

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out ProcessId result)
    {
        if (s is not null && Guid.TryParse(s, provider, out var parsedGuid))
        {
            result = new ProcessId(parsedGuid);
            return true;
        }

        result = Empty;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="ProcessId"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="ProcessId"/>.</returns>
    public static ProcessId Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static ProcessId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        new(Guid.Parse(s, provider));

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ProcessId result)
    {
        if (Guid.TryParse(s, provider, out var parsedGuid))
        {
            result = new ProcessId(parsedGuid);
            return true;
        }

        result = Empty;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(ProcessId other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ProcessId other) return CompareTo(other);
        throw new ArgumentException("Object must be of type ProcessId", nameof(obj));
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format);

    /// <summary>
    /// Determines whether one <see cref="ProcessId"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first process identifier to compare.</param>
    /// <param name="right">The second process identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ProcessId left, ProcessId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessId"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first process identifier to compare.</param>
    /// <param name="right">The second process identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ProcessId left, ProcessId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessId"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first process identifier to compare.</param>
    /// <param name="right">The second process identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ProcessId left, ProcessId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessId"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first process identifier to compare.</param>
    /// <param name="right">The second process identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ProcessId left, ProcessId right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="ProcessId"/> to its underlying <see cref="Guid"/> value.
    /// </summary>
    /// <param name="id">The process identifier to convert.</param>
    public static implicit operator Guid(ProcessId id) => id.Value;

    /// <summary>
    /// Converts a <see cref="Guid"/> to a <see cref="ProcessId"/> instance.
    /// </summary>
    /// <param name="value">The unique identifier GUID.</param>
    public static explicit operator ProcessId(Guid value) => new(value);
}





