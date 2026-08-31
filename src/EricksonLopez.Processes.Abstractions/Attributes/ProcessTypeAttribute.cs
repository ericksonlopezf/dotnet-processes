// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Specifies the logical process type identifier for a process state or definition class.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ProcessTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the unique logical process type identifier.
    /// </summary>
    public string ProcessType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTypeAttribute"/> class with the specified process type.
    /// </summary>
    /// <param name="processType">The logical process type identifier.</param>
    /// <exception cref="ArgumentException"><paramref name="processType"/> is <see langword="null"/> or white-space</exception>
    public ProcessTypeAttribute(string processType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processType);
        ProcessType = processType;
    }
}




