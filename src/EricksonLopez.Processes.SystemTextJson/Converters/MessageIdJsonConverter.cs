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
/// Converts <see cref="MessageId"/> instances to or from JSON.
/// </summary>
public sealed class MessageIdJsonConverter : JsonConverter<MessageId>
{
    /// <inheritdoc />
    public override MessageId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new MessageId(reader.GetString()!);
        }

        throw new JsonException("Expected string value for MessageId.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MessageId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}




