// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Provides a thread-safe registry holding registered process types and versions.
/// </summary>
public sealed class ProcessRegistry : IProcessRegistry
{
    private readonly ConcurrentDictionary<(ProcessType, ProcessVersion), byte> _registrations = new();

    /// <summary>
    /// Registers a process type and version in the registry.
    /// </summary>
    /// <param name="processType">The logical process type identifier.</param>
    /// <param name="version">The schema or definition version.</param>
    public void Register(ProcessType processType, ProcessVersion version)
    {
        _registrations.TryAdd((processType, version), 0);
    }

    /// <inheritdoc />
    public bool IsRegistered(ProcessType processType, ProcessVersion version) =>
        _registrations.ContainsKey((processType, version));

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2365:Properties should not copy collections", Justification = "Point-in-time snapshot array of registered process definitions")]
    public IReadOnlyCollection<(ProcessType Type, ProcessVersion Version)> RegisteredProcesses =>
        _registrations.Keys.ToArray();
}




