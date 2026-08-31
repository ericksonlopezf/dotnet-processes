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
/// Converts <see cref="CorrelationId"/> instances to or from JSON.
/// </summary>
public sealed class CorrelationIdJsonConverter : JsonConverter<CorrelationId>
{
    /// <inheritdoc />
    public override CorrelationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new CorrelationId(reader.GetString()!);
        }

        throw new JsonException("Expected string value for CorrelationId.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CorrelationId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}




