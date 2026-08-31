// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Defines the core contract for a Process Manager definition.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "Generic marker interface associates process definitions with their domain state type")]
public interface IProcess<TState>
    where TState : notnull
{
    /// <summary>
    /// Gets the unique logical process type identity.
    /// </summary>
    ProcessType Type { get; }

    /// <summary>
    /// Gets the schema or definition version of this process.
    /// </summary>
    ProcessVersion Version { get; }
}




