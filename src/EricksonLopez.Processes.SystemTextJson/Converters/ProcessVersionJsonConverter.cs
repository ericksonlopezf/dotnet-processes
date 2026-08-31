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
/// Converts <see cref="ProcessVersion"/> instances to or from JSON.
/// </summary>
public sealed class ProcessVersionJsonConverter : JsonConverter<ProcessVersion>
{
    /// <inheritdoc />
    public override ProcessVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var val) && val >= 1)
        {
            return new ProcessVersion(val);
        }

        throw new JsonException("Expected positive integer value for ProcessVersion.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ProcessVersion value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value.Value);
    }
}




