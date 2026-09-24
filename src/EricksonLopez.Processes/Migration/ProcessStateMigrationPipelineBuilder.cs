// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Constructs sequential, deterministic state migration pipelines.
/// </summary>
/// <typeparam name="TCurrentState">The state type at the current migration step.</typeparam>
public sealed class ProcessStateMigrationPipelineBuilder<TCurrentState>
    where TCurrentState : notnull
{
    private readonly ProcessVersion _fromVersion;
    private readonly ProcessVersion _currentVersion;
    private readonly Func<object, object> _pipeline;

    internal ProcessStateMigrationPipelineBuilder(ProcessVersion initialVersion)
    {
        _fromVersion = initialVersion;
        _currentVersion = initialVersion;
        _pipeline = state => state;
    }

    private ProcessStateMigrationPipelineBuilder(
        ProcessVersion fromVersion,
        ProcessVersion currentVersion,
        Func<object, object> pipeline)
    {
        _fromVersion = fromVersion;
        _currentVersion = currentVersion;
        _pipeline = pipeline;
    }

    /// <summary>
    /// Appends a typed <see cref="IProcessStateMigrator{TFrom, TTo}"/> step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextState">The destination state type for this step.</typeparam>
    /// <param name="migrator">The migrator instance to append.</param>
    /// <returns>A new <see cref="ProcessStateMigrationPipelineBuilder{TNextState}"/> advanced to <typeparamref name="TNextState"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="migrator"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The migrator source version does not match the current pipeline version</exception>
    public ProcessStateMigrationPipelineBuilder<TNextState> AddStep<TNextState>(
        IProcessStateMigrator<TCurrentState, TNextState> migrator)
        where TNextState : notnull
    {
        ArgumentNullException.ThrowIfNull(migrator);

        if (migrator.FromVersion != _currentVersion)
        {
            throw new InvalidOperationException(
                $"Migrator source version '{migrator.FromVersion.Value}' does not match pipeline current version '{_currentVersion.Value}'.");
        }

        var prev = _pipeline;
        Func<object, object> next = state => migrator.Migrate((TCurrentState)prev(state));

        return new ProcessStateMigrationPipelineBuilder<TNextState>(_fromVersion, migrator.ToVersion, next);
    }

    /// <summary>
    /// Appends a transformation function step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextState">The destination state type for this step.</typeparam>
    /// <param name="targetVersion">The version of the state schema produced by this step.</param>
    /// <param name="transformer">The migration transformation function.</param>
    /// <returns>A new <see cref="ProcessStateMigrationPipelineBuilder{TNextState}"/> advanced to <typeparamref name="TNextState"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transformer"/> is <see langword="null"/></exception>
    public ProcessStateMigrationPipelineBuilder<TNextState> AddStep<TNextState>(
        ProcessVersion targetVersion,
        Func<TCurrentState, TNextState> transformer)
        where TNextState : notnull
    {
        ArgumentNullException.ThrowIfNull(transformer);

        var prev = _pipeline;
        Func<object, object> next = state => transformer((TCurrentState)prev(state));

        return new ProcessStateMigrationPipelineBuilder<TNextState>(_fromVersion, targetVersion, next);
    }

    /// <summary>
    /// Builds the composed <see cref="IProcessStateMigrator{TFrom, TTo}"/> executing all steps sequentially.
    /// </summary>
    /// <typeparam name="TFrom">The initial source state type.</typeparam>
    /// <returns>An instance of <see cref="IProcessStateMigrator{TFrom, TTo}"/> that executes all pipeline steps.</returns>
    public IProcessStateMigrator<TFrom, TCurrentState> Build<TFrom>()
        where TFrom : notnull
    {
        var finalFunc = _pipeline;
        return new ComposedProcessStateMigrator<TFrom, TCurrentState>(
            _fromVersion,
            _currentVersion,
            initial => (TCurrentState)finalFunc(initial));
    }
}
