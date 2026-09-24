// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes.Testing.Doubles;

/// <summary>
/// Provides JSON serialization metadata for storage test models.
/// </summary>
[JsonSerializable(typeof(SampleOrderState))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(ComplexOrderState))]
[JsonSerializable(typeof(ProcessId))]
[JsonSerializable(typeof(CorrelationId))]
[JsonSerializable(typeof(ProcessVersion))]
[JsonSerializable(typeof(Revision))]
public sealed partial class SharedStorageTestJsonContext : JsonSerializerContext
{
}
