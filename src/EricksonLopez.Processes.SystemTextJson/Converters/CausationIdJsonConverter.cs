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
/// Converts <see cref="CausationId"/> instances to or from JSON.
/// </summary>
public sealed class CausationIdJsonConverter : JsonConverter<CausationId>
{
    /// <inheritdoc />
    public override CausationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new CausationId(reader.GetString()!);
        }

        throw new JsonException("Expected string value for CausationId.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CausationId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}




