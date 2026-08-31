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

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonProcessStateSerializer{TState}"/> class with the specified type metadata.
    /// </summary>
    /// <param name="jsonTypeInfo">The source-generated <see cref="JsonTypeInfo{TState}"/> metadata.</param>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/></exception>
    public SystemTextJsonProcessStateSerializer(JsonTypeInfo<TState> jsonTypeInfo)
    {
        _jsonTypeInfo = jsonTypeInfo ?? throw new ArgumentNullException(nameof(jsonTypeInfo));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/></exception>
    public byte[] Serialize(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.SerializeToUtf8Bytes(state, _jsonTypeInfo);
    }

    /// <inheritdoc />
    /// <exception cref="JsonException">The deserialized payload is <see langword="null"/></exception>
    public TState Deserialize(ReadOnlySpan<byte> data)
    {
        var result = JsonSerializer.Deserialize(data, _jsonTypeInfo);
        if (result is null)
        {
            throw new JsonException($"Failed to deserialize payload into state of type '{typeof(TState).Name}'.");
        }

        return result;
    }
}





