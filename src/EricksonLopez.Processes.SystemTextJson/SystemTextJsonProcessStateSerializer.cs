// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.SystemTextJson;

/// <summary>
/// Provides a JSON state serializer utilizing compile-time <see cref="JsonTypeInfo{T}"/> metadata.
/// </summary>
/// <typeparam name="TState">The process state type.</typeparam>
public sealed class SystemTextJsonProcessStateSerializer<TState> : IProcessStateSerializer<TState>
    where TState : notnull
{
    private readonly JsonTypeInfo<TState> _jsonTypeInfo;
    private readonly int? _maxPayloadSizeBytes;

    /// <summary>
    /// Gets the maximum allowed payload size in bytes, or <see langword="null"/> if no arbitrary limit is enforced.
    /// </summary>
    public int? MaxPayloadSizeBytes => _maxPayloadSizeBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonProcessStateSerializer{TState}"/> class with the specified type metadata.
    /// </summary>
    /// <param name="jsonTypeInfo">The source-generated <see cref="JsonTypeInfo{TState}"/> metadata.</param>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/></exception>
    public SystemTextJsonProcessStateSerializer(JsonTypeInfo<TState> jsonTypeInfo)
        : this(jsonTypeInfo, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonProcessStateSerializer{TState}"/> class with the specified type metadata and optional payload size limit.
    /// </summary>
    /// <param name="jsonTypeInfo">The source-generated <see cref="JsonTypeInfo{TState}"/> metadata.</param>
    /// <param name="maxPayloadSizeBytes">The maximum allowed payload size in bytes, or <see langword="null"/> for unlimited.</param>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPayloadSizeBytes"/> is less than or equal to zero</exception>
    public SystemTextJsonProcessStateSerializer(JsonTypeInfo<TState> jsonTypeInfo, int? maxPayloadSizeBytes)
    {
        _jsonTypeInfo = jsonTypeInfo ?? throw new ArgumentNullException(nameof(jsonTypeInfo));
        if (maxPayloadSizeBytes is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPayloadSizeBytes), "Maximum payload size in bytes must be greater than zero.");
        }

        _maxPayloadSizeBytes = maxPayloadSizeBytes;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The serialized payload size exceeds <see cref="MaxPayloadSizeBytes"/></exception>
    public byte[] Serialize(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, _jsonTypeInfo);

        if (_maxPayloadSizeBytes.HasValue && bytes.Length > _maxPayloadSizeBytes.Value)
        {
            throw new InvalidOperationException($"Serialized state payload size ({bytes.Length} bytes) exceeds the configured maximum allowed size of {_maxPayloadSizeBytes.Value} bytes.");
        }

        return bytes;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The incoming payload size exceeds <see cref="MaxPayloadSizeBytes"/></exception>
    /// <exception cref="JsonException">The deserialized payload is <see langword="null"/></exception>
    public TState Deserialize(ReadOnlySpan<byte> data)
    {
        if (_maxPayloadSizeBytes.HasValue && data.Length > _maxPayloadSizeBytes.Value)
        {
            throw new InvalidOperationException($"Incoming state payload size ({data.Length} bytes) exceeds the configured maximum allowed size of {_maxPayloadSizeBytes.Value} bytes.");
        }

        var result = JsonSerializer.Deserialize(data, _jsonTypeInfo);
        if (result is null)
        {
            throw new JsonException($"Failed to deserialize payload into state of type '{typeof(TState).Name}'.");
        }

        return result;
    }
}





