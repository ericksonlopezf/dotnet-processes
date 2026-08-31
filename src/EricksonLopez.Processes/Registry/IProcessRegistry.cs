// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Defines a registry for storing and resolving process definitions, handlers, and correlation extractors.
/// </summary>
public interface IProcessRegistry
{
    /// <summary>
    /// Determines whether a process definition of the specified type and version is registered.
    /// </summary>
    /// <param name="processType">The logical process type identifier.</param>
    /// <param name="version">The schema or definition version.</param>
    /// <returns><see langword="true"/> if registered; otherwise, <see langword="false"/>.</returns>
    bool IsRegistered(ProcessType processType, ProcessVersion version);

    /// <summary>
    /// Gets the collection of all registered process types and versions.
    /// </summary>
    IReadOnlyCollection<(ProcessType Type, ProcessVersion Version)> RegisteredProcesses => Array.Empty<(ProcessType, ProcessVersion)>();
}




