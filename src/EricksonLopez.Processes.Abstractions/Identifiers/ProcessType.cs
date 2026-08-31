// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents the logical, immutable name and category of a process definition.
/// </summary>
public readonly record struct ProcessType : IComparable<ProcessType>, IComparable, ISpanFormattable, ISpanParsable<ProcessType>
{
    /// <summary>
    /// Gets the string token representing the process type.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessType"/> struct with the specified logical process type name.
    /// </summary>
    /// <param name="value">The logical process type name.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public ProcessType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Creates a <see cref="ProcessType"/> from the specified string value.
    /// </summary>
    /// <param name="value">The logical process type name.</param>
    /// <returns>A new <see cref="ProcessType"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static ProcessType From(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="ProcessType"/> from the specified string value.
    /// </summary>
    /// <param name="value">The logical process type name.</param>
    /// <returns>A new <see cref="ProcessType"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static ProcessType FromString(string value) => new(value);

    /// <summary>
    /// Parses a string into a <see cref="ProcessType"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="ProcessType"/>.</returns>
    public static ProcessType Parse(string s) => new(s);

    /// <inheritdoc />
    public static ProcessType Parse(string s, IFormatProvider? provider) => new(s);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out ProcessType result)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            result = new ProcessType(s);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="ProcessType"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="ProcessType"/>.</returns>
    public static ProcessType Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static ProcessType Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (s.IsWhiteSpace())
        {
            throw new ArgumentException("Span cannot be empty or whitespace.", nameof(s));
        }

        return new ProcessType(s.ToString());
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ProcessType result)
    {
        if (!s.IsWhiteSpace())
        {
            result = new ProcessType(s.ToString());
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(ProcessType other) =>
        string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ProcessType other) return CompareTo(other);
        throw new ArgumentException("Object must be of type ProcessType", nameof(obj));
    }

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => Value ?? string.Empty;

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (Value is null)
        {
            charsWritten = 0;
            return true;
        }

        if (destination.Length >= Value.Length)
        {
            Value.AsSpan().CopyTo(destination);
            charsWritten = Value.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <summary>
    /// Determines whether one <see cref="ProcessType"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first process type to compare.</param>
    /// <param name="right">The second process type to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ProcessType left, ProcessType right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessType"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first process type to compare.</param>
    /// <param name="right">The second process type to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ProcessType left, ProcessType right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessType"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first process type to compare.</param>
    /// <param name="right">The second process type to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ProcessType left, ProcessType right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessType"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first process type to compare.</param>
    /// <param name="right">The second process type to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ProcessType left, ProcessType right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="ProcessType"/> to its underlying string representation.
    /// </summary>
    /// <param name="type">The process type to convert.</param>
    public static implicit operator string(ProcessType type) => type.Value;

    /// <summary>
    /// Converts a string to a <see cref="ProcessType"/> instance.
    /// </summary>
    /// <param name="value">The logical process type name.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static explicit operator ProcessType(string value) => new(value);
}





