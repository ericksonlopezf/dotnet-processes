// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a strongly typed identifier for the direct trigger or parent message that caused an action.
/// </summary>
public readonly record struct CausationId : IComparable<CausationId>, IComparable, ISpanFormattable, ISpanParsable<CausationId>
{
    /// <summary>
    /// Gets the string value of the causation identifier.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CausationId"/> struct with the specified string value.
    /// </summary>
    /// <param name="value">The causation identifier string.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public CausationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="CausationId"/> from a time-ordered UUIDv7.
    /// </summary>
    /// <returns>A new <see cref="CausationId"/> instance.</returns>
    public static CausationId NewId() => new(Guid.CreateVersion7().ToString());

    /// <summary>
    /// Creates a <see cref="CausationId"/> from the specified string.
    /// </summary>
    /// <param name="value">The causation identifier string.</param>
    /// <returns>A new <see cref="CausationId"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static CausationId From(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="CausationId"/> from the specified string.
    /// </summary>
    /// <param name="value">The causation identifier string.</param>
    /// <returns>A new <see cref="CausationId"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static CausationId FromString(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="CausationId"/> from the specified <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The unique identifier GUID.</param>
    /// <returns>A new <see cref="CausationId"/> instance.</returns>
    public static CausationId FromGuid(Guid value) => new(value.ToString());

    /// <summary>
    /// Parses a string into a <see cref="CausationId"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="CausationId"/>.</returns>
    public static CausationId Parse(string s) => new(s);

    /// <inheritdoc />
    public static CausationId Parse(string s, IFormatProvider? provider) => new(s);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out CausationId result)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            result = new CausationId(s);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="CausationId"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="CausationId"/>.</returns>
    public static CausationId Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static CausationId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (s.IsWhiteSpace())
        {
            throw new ArgumentException("Span cannot be empty or whitespace.", nameof(s));
        }

        return new CausationId(s.ToString());
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CausationId result)
    {
        if (!s.IsWhiteSpace())
        {
            result = new CausationId(s.ToString());
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(CausationId other) =>
        string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is CausationId other) return CompareTo(other);
        throw new ArgumentException("Object must be of type CausationId", nameof(obj));
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
    /// Determines whether one <see cref="CausationId"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first causation identifier to compare.</param>
    /// <param name="right">The second causation identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(CausationId left, CausationId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="CausationId"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first causation identifier to compare.</param>
    /// <param name="right">The second causation identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(CausationId left, CausationId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="CausationId"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first causation identifier to compare.</param>
    /// <param name="right">The second causation identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(CausationId left, CausationId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="CausationId"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first causation identifier to compare.</param>
    /// <param name="right">The second causation identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(CausationId left, CausationId right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="CausationId"/> to its underlying string representation.
    /// </summary>
    /// <param name="id">The causation identifier to convert.</param>
    public static implicit operator string(CausationId id) => id.Value;

    /// <summary>
    /// Converts a string to a <see cref="CausationId"/> instance.
    /// </summary>
    /// <param name="value">The causation identifier string.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static explicit operator CausationId(string value) => new(value);
}





