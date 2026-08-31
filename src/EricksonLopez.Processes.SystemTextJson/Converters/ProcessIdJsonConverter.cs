// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.SystemTextJson;

/// <summary>
/// Converts <see cref="ProcessId"/> instances to or from JSON.
/// </summary>
public sealed class ProcessIdJsonConverter : JsonConverter<ProcessId>
{
    /// <inheritdoc />
    public override ProcessId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out var guid))
        {
            return new ProcessId(guid);
        }

        throw new JsonException("Expected string representation of a GUID for ProcessId.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ProcessId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}




