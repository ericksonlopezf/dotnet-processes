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
/// Provides options configuring custom JSON converters for process primitives and identifiers.
/// </summary>
public static class ProcessJsonSerializerOptions
{
    /// <summary>
    /// Configures the specified <see cref="JsonSerializerOptions"/> with custom converters for all process primitives.
    /// </summary>
    /// <param name="options">The JSON serializer options to configure.</param>
    /// <returns>The configured <see cref="JsonSerializerOptions"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Converters.Add(new ProcessIdJsonConverter());
        options.Converters.Add(new ProcessTypeJsonConverter());
        options.Converters.Add(new ProcessVersionJsonConverter());
        options.Converters.Add(new RevisionJsonConverter());
        options.Converters.Add(new CorrelationIdJsonConverter());
        options.Converters.Add(new CausationIdJsonConverter());
        options.Converters.Add(new MessageIdJsonConverter());

        return options;
    }

    /// <summary>
    /// Creates a new <see cref="JsonSerializerOptions"/> pre-configured with all process converters and an optional type resolver.
    /// </summary>
    /// <param name="typeInfoResolver">The optional JSON type info resolver, or <see langword="null"/>.</param>
    /// <returns>A new pre-configured <see cref="JsonSerializerOptions"/> instance.</returns>
    public static JsonSerializerOptions Create(IJsonTypeInfoResolver? typeInfoResolver = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        if (typeInfoResolver is not null)
        {
            options.TypeInfoResolver = typeInfoResolver;
        }

        return Configure(options);
    }
}




