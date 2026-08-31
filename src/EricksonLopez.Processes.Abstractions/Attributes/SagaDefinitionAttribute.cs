// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Specifies that a class is a saga definition with compensation capabilities discovered for compile-time registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SagaDefinitionAttribute : Attribute
{
    /// <summary>
    /// Gets the unique logical saga process type identifier.
    /// </summary>
    public string ProcessType { get; }

    /// <summary>
    /// Gets the schema or definition version number.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaDefinitionAttribute"/> class with the specified process type and version.
    /// </summary>
    /// <param name="processType">The logical saga process type identifier.</param>
    /// <param name="version">The schema or definition version number.</param>
    /// <exception cref="ArgumentException"><paramref name="processType"/> is <see langword="null"/> or white-space</exception>
    public SagaDefinitionAttribute(string processType, int version = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processType);
        ProcessType = processType;
        Version = version;
    }
}




