// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Provides fluent factory methods to compose multi-step schema migration pipelines into a single migrator.
/// </summary>
public static class ProcessStateMigrationPipeline
{
    /// <summary>
    /// Starts building a sequential state migration pipeline beginning at the specified initial version.
    /// </summary>
    /// <typeparam name="TInitialState">The type of the initial state schema.</typeparam>
    /// <param name="initialVersion">The version of the initial schema.</param>
    /// <returns>A new <see cref="ProcessStateMigrationPipelineBuilder{TInitialState}"/> instance.</returns>
    public static ProcessStateMigrationPipelineBuilder<TInitialState> Create<TInitialState>(ProcessVersion initialVersion)
        where TInitialState : notnull
    {
        return new ProcessStateMigrationPipelineBuilder<TInitialState>(initialVersion);
    }
}
