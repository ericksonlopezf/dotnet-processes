// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a strongly typed unique identifier for a specific message, command, or event instance.
/// </summary>
public readonly record struct MessageId : IComparable<MessageId>, IComparable, ISpanFormattable, ISpanParsable<MessageId>
{
    /// <summary>
    /// Gets the string value of the message identifier.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageId"/> struct with the specified string value.
    /// </summary>
    /// <param name="value">The message identifier string.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public MessageId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="MessageId"/> from a time-ordered UUIDv7.
    /// </summary>
    /// <returns>A new <see cref="MessageId"/> instance.</returns>
    public static MessageId NewId() => new(Guid.CreateVersion7().ToString());

    /// <summary>
    /// Creates a <see cref="MessageId"/> from the specified string.
    /// </summary>
    /// <param name="value">The message identifier string.</param>
    /// <returns>A new <see cref="MessageId"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static MessageId From(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="MessageId"/> from the specified string.
    /// </summary>
    /// <param name="value">The message identifier string.</param>
    /// <returns>A new <see cref="MessageId"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static MessageId FromString(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="MessageId"/> from the specified <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The unique identifier GUID.</param>
    /// <returns>A new <see cref="MessageId"/> instance.</returns>
    public static MessageId FromGuid(Guid value) => new(value.ToString());

    /// <summary>
    /// Parses a string into a <see cref="MessageId"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed <see cref="MessageId"/>.</returns>
    public static MessageId Parse(string s) => new(s);

    /// <inheritdoc />
    public static MessageId Parse(string s, IFormatProvider? provider) => new(s);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out MessageId result)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            result = new MessageId(s);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Parses a span of characters into a <see cref="MessageId"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <returns>The parsed <see cref="MessageId"/>.</returns>
    public static MessageId Parse(ReadOnlySpan<char> s) => Parse(s, null);

    /// <inheritdoc />
    public static MessageId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (s.IsWhiteSpace())
        {
            throw new ArgumentException("Span cannot be empty or whitespace.", nameof(s));
        }

        return new MessageId(s.ToString());
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MessageId result)
    {
        if (!s.IsWhiteSpace())
        {
            result = new MessageId(s.ToString());
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public int CompareTo(MessageId other) =>
        string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is MessageId other) return CompareTo(other);
        throw new ArgumentException("Object must be of type MessageId", nameof(obj));
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
    /// Determines whether one <see cref="MessageId"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first message identifier to compare.</param>
    /// <param name="right">The second message identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(MessageId left, MessageId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="MessageId"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first message identifier to compare.</param>
    /// <param name="right">The second message identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(MessageId left, MessageId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="MessageId"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first message identifier to compare.</param>
    /// <param name="right">The second message identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(MessageId left, MessageId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="MessageId"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first message identifier to compare.</param>
    /// <param name="right">The second message identifier to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(MessageId left, MessageId right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Converts a <see cref="MessageId"/> to its underlying string representation.
    /// </summary>
    /// <param name="id">The message identifier to convert.</param>
    public static implicit operator string(MessageId id) => id.Value;

    /// <summary>
    /// Converts a string to a <see cref="MessageId"/> instance.
    /// </summary>
    /// <param name="value">The message identifier string.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or white-space</exception>
    public static explicit operator MessageId(string value) => new(value);
}





