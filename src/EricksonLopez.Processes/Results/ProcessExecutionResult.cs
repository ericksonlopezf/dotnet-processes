// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Represents the final result of coordinating and persisting a process step.
/// </summary>
/// <typeparam name="TState">The domain state type.</typeparam>
public sealed record ProcessExecutionResult<TState>
    where TState : notnull
{
    /// <summary>
    /// Gets the persisted process instance after the transition.
    /// </summary>
    public ProcessInstance<TState> Instance { get; init; }

    /// <summary>
    /// Gets the list of side-effect intents emitted by the transition.
    /// </summary>
    public IReadOnlyList<ProcessEffect> Effects { get; init; }

    /// <summary>
    /// Gets the persistence outcome resulting from saving the instance.
    /// </summary>
    public ProcessSaveResult SaveResult { get; init; }

    /// <summary>
    /// Gets a value indicating whether the execution succeeded and state was saved.
    /// </summary>
    public bool IsSuccess => SaveResult == ProcessSaveResult.Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExecutionResult{TState}"/> record with the instance, effects, and save result.
    /// </summary>
    /// <param name="instance">The persisted process instance after the transition.</param>
    /// <param name="effects">The list of side-effect intents emitted by the transition.</param>
    /// <param name="saveResult">The persistence outcome resulting from saving the instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/></exception>
    public ProcessExecutionResult(
        ProcessInstance<TState> instance,
        IReadOnlyList<ProcessEffect> effects,
        ProcessSaveResult saveResult)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        Effects = effects ?? Array.Empty<ProcessEffect>();
        SaveResult = saveResult;
    }
}





