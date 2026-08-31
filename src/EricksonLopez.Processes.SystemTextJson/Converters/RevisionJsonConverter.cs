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
/// Converts <see cref="Revision"/> instances to or from JSON.
/// </summary>
public sealed class RevisionJsonConverter : JsonConverter<Revision>
{
    /// <inheritdoc />
    public override Revision Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var val) && val >= 0)
        {
            return new Revision(val);
        }

        throw new JsonException("Expected non-negative integer value for Revision.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Revision value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value.Value);
    }
}




