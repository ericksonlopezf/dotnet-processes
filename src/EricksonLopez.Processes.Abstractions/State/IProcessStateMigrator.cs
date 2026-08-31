// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Defines a contract for migrating process state from a source schema to a target schema version.
/// </summary>
/// <typeparam name="TFrom">The source state schema type.</typeparam>
/// <typeparam name="TTo">The destination state schema type.</typeparam>
public interface IProcessStateMigrator<in TFrom, out TTo>
    where TFrom : notnull
    where TTo : notnull
{
    /// <summary>
    /// Gets the source schema version.
    /// </summary>
    ProcessVersion FromVersion { get; }

    /// <summary>
    /// Gets the target schema version.
    /// </summary>
    ProcessVersion ToVersion { get; }

    /// <summary>
    /// Migrates the specified source state to the target schema.
    /// </summary>
    /// <param name="sourceState">The source state instance to migrate.</param>
    /// <returns>The migrated state instance conforming to the target schema.</returns>
    TTo Migrate(TFrom sourceState);
}




