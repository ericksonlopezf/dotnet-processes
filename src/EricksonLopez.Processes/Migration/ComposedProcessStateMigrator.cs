// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

internal sealed class ComposedProcessStateMigrator<TFrom, TTo> : IProcessStateMigrator<TFrom, TTo>
    where TFrom : notnull
    where TTo : notnull
{
    private readonly Func<TFrom, TTo> _migrateFunc;

    public ProcessVersion FromVersion { get; }
    public ProcessVersion ToVersion { get; }

    public ComposedProcessStateMigrator(
        ProcessVersion fromVersion,
        ProcessVersion toVersion,
        Func<TFrom, TTo> migrateFunc)
    {
        FromVersion = fromVersion;
        ToVersion = toVersion;
        _migrateFunc = migrateFunc ?? throw new ArgumentNullException(nameof(migrateFunc));
    }

    public TTo Migrate(TFrom sourceState)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        return _migrateFunc(sourceState);
    }
}
