// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines a contract for serializing and deserializing process domain state instances.
/// </summary>
/// <typeparam name="TState">The process domain state type.</typeparam>
public interface IProcessStateSerializer<TState>
    where TState : notnull
{
    /// <summary>
    /// Serializes the specified state instance into a byte array.
    /// </summary>
    /// <param name="state">The process state instance to serialize.</param>
    /// <returns>The serialized byte array.</returns>
    byte[] Serialize(TState state);

    /// <summary>
    /// Deserializes the byte span into a process state instance.
    /// </summary>
    /// <param name="data">The serialized byte span to deserialize.</param>
    /// <returns>The deserialized process state instance.</returns>
    TState Deserialize(ReadOnlySpan<byte> data);
}




