// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents the schema and definition version of a process.
/// </summary>
public readonly record struct ProcessVersion : IComparable<ProcessVersion>, IComparable, ISpanFormattable, ISpanParsable<ProcessVersion>
{
    /// <summary>
    /// Gets the integer version value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessVersion"/> struct with the specified version number.
    /// </summary>
    /// <param name="value">The version number (must be greater than or equal to 1).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1</exception>
    public ProcessVersion(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Process version must be greater than or equal to 1.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the initial version (<c>1</c>).
    /// </summary>
    public static ProcessVersion Initial => new(1);

    /// <summary>
    /// Creates a <see cref="ProcessVersion"/> from an integer value.
    /// </summary>
    /// <param name="value">The version number (must be greater than or equal to 1).</param>
    /// <returns>A new <see cref="ProcessVersion"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1</exception>
    public static ProcessVersion From(int value) => new(value);

    /// <summary>
    /// Creates a <see cref="ProcessVersion"/> from an integer value.
    /// </summary>
    /// <param name="value">The version number (must be greater than or equal to 1).</param>
    /// <returns>A new <see cref="ProcessVersion"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1</exception>
    public static ProcessVersion FromInt32(int value) => new(value);

    /// <summary>
    /// Converts this instance to its integer value.
    /// </summary>
    /// <returns>The version integer value.</returns>
    public int ToInt32() => Value;

    /// <summary>
    /// Returns the next incremented version.
    /// </summary>
    /// <returns>A new <see cref="ProcessVersion"/> incremented by one.</returns>
    public ProcessVersion Next() => new(Value + 1);

    /// <summary>
    /// Parses a string into a <see cref="ProcessVersion"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="ProcessVersion"/>.</returns>
    public static ProcessVersion Parse(string s) => Parse(s, null);

    /// <inheritdoc />
    public static ProcessVersion Parse(string s, IFormatProvider? provider) =>
        new(int.Parse(s, provider ?? CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out ProcessVersion result)
    {
        if (int.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out var val) && val >= 1)
        {
            result = new ProcessVersion(val);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="ProcessVersion"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="ProcessVersion"/>.</returns>
    public static ProcessVersion Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static ProcessVersion Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        new(int.Parse(s, provider ?? CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ProcessVersion result)
    {
        if (int.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out var val) && val >= 1)
        {
            result = new ProcessVersion(val);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(ProcessVersion other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ProcessVersion other) return CompareTo(other);
        throw new ArgumentException("Object must be of type ProcessVersion", nameof(obj));
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Determines whether one <see cref="ProcessVersion"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first process version to compare.</param>
    /// <param name="right">The second process version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ProcessVersion left, ProcessVersion right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessVersion"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first process version to compare.</param>
    /// <param name="right">The second process version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ProcessVersion left, ProcessVersion right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessVersion"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first process version to compare.</param>
    /// <param name="right">The second process version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ProcessVersion left, ProcessVersion right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="ProcessVersion"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first process version to compare.</param>
    /// <param name="right">The second process version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ProcessVersion left, ProcessVersion right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="ProcessVersion"/> to its integer value.
    /// </summary>
    /// <param name="version">The process version to convert.</param>
    public static implicit operator int(ProcessVersion version) => version.Value;

    /// <summary>
    /// Converts an integer to a <see cref="ProcessVersion"/> instance.
    /// </summary>
    /// <param name="value">The version number.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1</exception>
    public static explicit operator ProcessVersion(int value) => new(value);
}





