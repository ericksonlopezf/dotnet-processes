// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a monotonically increasing revision token used for optimistic concurrency control in state persistence.
/// </summary>
public readonly record struct Revision : IComparable<Revision>, IComparable, ISpanFormattable, ISpanParsable<Revision>
{
    /// <summary>
    /// Gets the long revision value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Revision"/> struct with the specified revision value.
    /// </summary>
    /// <param name="value">The revision value (must be greater than or equal to 0).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 0</exception>
    public Revision(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Revision must be greater than or equal to 0.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the zero uncommitted revision (<c>0</c>).
    /// </summary>
    public static Revision None => new(0);

    /// <summary>
    /// Gets the initial committed revision (<c>1</c>).
    /// </summary>
    public static Revision Initial => new(1);

    /// <summary>
    /// Creates a <see cref="Revision"/> from a long integer value.
    /// </summary>
    /// <param name="value">The revision value (must be greater than or equal to 0).</param>
    /// <returns>A new <see cref="Revision"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 0</exception>
    public static Revision From(long value) => new(value);

    /// <summary>
    /// Creates a <see cref="Revision"/> from a long integer value.
    /// </summary>
    /// <param name="value">The revision value (must be greater than or equal to 0).</param>
    /// <returns>A new <see cref="Revision"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 0</exception>
    public static Revision FromInt64(long value) => new(value);

    /// <summary>
    /// Converts this instance to its long integer value.
    /// </summary>
    /// <returns>The revision long integer value.</returns>
    public long ToInt64() => Value;

    /// <summary>
    /// Returns the next incremented revision.
    /// </summary>
    /// <returns>A new <see cref="Revision"/> incremented by one.</returns>
    public Revision Next() => new(Value + 1);

    /// <summary>
    /// Parses a string into a <see cref="Revision"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="Revision"/>.</returns>
    public static Revision Parse(string s) => Parse(s, null);

    /// <inheritdoc />
    public static Revision Parse(string s, IFormatProvider? provider) =>
        new(long.Parse(s, provider ?? CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out Revision result)
    {
        if (long.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out var val) && val >= 0)
        {
            result = new Revision(val);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="Revision"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="Revision"/>.</returns>
    public static Revision Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static Revision Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        new(long.Parse(s, provider ?? CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Revision result)
    {
        if (long.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out var val) && val >= 0)
        {
            result = new Revision(val);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(Revision other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Revision other) return CompareTo(other);
        throw new ArgumentException("Object must be of type Revision", nameof(obj));
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
    /// Determines whether one <see cref="Revision"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first revision to compare.</param>
    /// <param name="right">The second revision to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(Revision left, Revision right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="Revision"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first revision to compare.</param>
    /// <param name="right">The second revision to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(Revision left, Revision right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="Revision"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first revision to compare.</param>
    /// <param name="right">The second revision to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(Revision left, Revision right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="Revision"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first revision to compare.</param>
    /// <param name="right">The second revision to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(Revision left, Revision right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="Revision"/> to its long integer value.
    /// </summary>
    /// <param name="revision">The revision to convert.</param>
    public static implicit operator long(Revision revision) => revision.Value;

    /// <summary>
    /// Converts a long integer to a <see cref="Revision"/> instance.
    /// </summary>
    /// <param name="value">The revision value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 0</exception>
    public static explicit operator Revision(long value) => new(value);
}





