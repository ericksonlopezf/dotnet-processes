// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Coordinates process lifecycle execution, handler invocation, optimistic concurrency retries, and persistence.
/// </summary>
/// <typeparam name="TState">The strongly typed domain state type.</typeparam>
public sealed class ProcessCoordinator<TState>
    where TState : notnull
{
    private readonly IProcessStore<TState> _store;
    private readonly int _maxConcurrencyRetries;
    private readonly TimeProvider _timeProvider;
    private readonly Func<int, TimeSpan> _backoffStrategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessCoordinator{TState}"/> class with the specified store and options.
    /// </summary>
    /// <param name="store">The persistence store for process instances.</param>
    /// <param name="options">The optional coordinator configuration options, or <see langword="null"/> for defaults.</param>
    /// <param name="timeProvider">The optional time provider instance, or <see langword="null"/> to use <see cref="TimeProvider.System"/>.</param>
    /// <param name="backoffStrategy">The optional backoff retry delay strategy, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/></exception>
    public ProcessCoordinator(
        IProcessStore<TState> store,
        ProcessCoordinatorOptions? options = null,
        TimeProvider? timeProvider = null,
        Func<int, TimeSpan>? backoffStrategy = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        var opt = options ?? new ProcessCoordinatorOptions();
        _maxConcurrencyRetries = Math.Max(0, opt.MaxConcurrencyRetries);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _backoffStrategy = backoffStrategy ?? (attempt => TimeSpan.FromMilliseconds(opt.InitialBackoffDelay.TotalMilliseconds * attempt));
    }

    /// <summary>
    /// Calculates a linear backoff delay of 10 milliseconds per retry attempt.
    /// </summary>
    /// <param name="attempt">The retry attempt index.</param>
    /// <returns>The calculated backoff <see cref="TimeSpan"/> duration.</returns>
    public static TimeSpan DefaultBackoffStrategy(int attempt) =>
        TimeSpan.FromMilliseconds(10 * attempt);

    /// <summary>
    /// Executes a process handler against the targeted process instance with optimistic concurrency retries.
    /// </summary>
    /// <typeparam name="TEvent">The incoming event payload type.</typeparam>
    /// <param name="handler">The typed process handler to invoke.</param>
    /// <param name="correlation">The correlation extractor used to obtain identifiers from the event.</param>
    /// <param name="eventMessage">The incoming event instance to handle.</param>
    /// <param name="initialStateFactory">The factory function to initialize state when a new instance is created.</param>
    /// <param name="canInitiate">A value indicating whether this event is allowed to initiate a new process instance.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="ProcessExecutionResult{TState}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/>, <paramref name="correlation"/>, or <paramref name="eventMessage"/> is <see langword="null"/></exception>
    /// <exception cref="ProcessNotFoundException">The process instance was not found and <paramref name="canInitiate"/> is <see langword="false"/></exception>
    /// <exception cref="ConcurrencyConflictException">Optimistic concurrency retries exceeded the configured maximum limit</exception>
    public async ValueTask<ProcessExecutionResult<TState>> ExecuteAsync<TEvent>(
        IProcessHandler<TState, TEvent> handler,
        IProcessCorrelation<TEvent> correlation,
        TEvent eventMessage,
        Func<TEvent, TState>? initialStateFactory = null,
        bool canInitiate = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(correlation);
        ArgumentNullException.ThrowIfNull(eventMessage);

        var processId = correlation.ExtractProcessId(eventMessage);
        var correlationId = correlation.ExtractCorrelationId(eventMessage);
        var causationId = correlation.ExtractCausationId(eventMessage) ?? CausationId.NewId();
        var messageId = MessageId.NewId();

        var processType = handler.Type.Value;
        var processVersion = handler.Version.Value;

        using var activity = ProcessDiagnostics.ActivitySource.StartActivity(
            $"Process {processType}.Execute",
            ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("process.id", processId.ToString());
            activity.SetTag("process.type", processType);
            activity.SetTag("process.version", processVersion);
            activity.SetTag("correlation.id", correlationId.ToString());
        }

        var attempt = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (instance, shouldExitEarly) = await ResolveInstanceAsync(
                processId, correlationId, handler, eventMessage, initialStateFactory, canInitiate, cancellationToken);

            if (shouldExitEarly)
            {
                return new ProcessExecutionResult<TState>(instance, Array.Empty<ProcessEffect>(), ProcessSaveResult.Success);
            }

            var context = new ProcessContext(
                processId: processId,
                correlationId: correlationId,
                causationId: causationId,
                messageId: messageId,
                now: _timeProvider.GetUtcNow(),
                timeProvider: _timeProvider,
                cancellationToken: cancellationToken);

            var transitionResult = await handler.HandleAsync(instance.State, eventMessage, context);

            var updatedInstance = instance.Advance(
                newState: transitionResult.State,
                newStatus: transitionResult.Status,
                now: _timeProvider.GetUtcNow());

            var saveResult = await _store.SaveAsync(updatedInstance, cancellationToken);

            if (saveResult == ProcessSaveResult.Success)
            {
                return HandleSuccessSave(processType, processVersion, updatedInstance, transitionResult, stopwatch);
            }

            if (saveResult == ProcessSaveResult.ConcurrencyConflict)
            {
                attempt = await HandleConcurrencyRetryAsync(processType, processVersion, processId, instance.Revision, attempt, cancellationToken);
                continue;
            }

            return new ProcessExecutionResult<TState>(
                instance: updatedInstance,
                effects: transitionResult.Effects,
                saveResult: saveResult);
        }
    }

    private async ValueTask<(ProcessInstance<TState> Instance, bool ShouldExitEarly)> ResolveInstanceAsync<TEvent>(
        ProcessId processId,
        CorrelationId correlationId,
        IProcessHandler<TState, TEvent> handler,
        TEvent eventMessage,
        Func<TEvent, TState>? initialStateFactory,
        bool canInitiate,
        CancellationToken cancellationToken)
    {
        var instance = await _store.GetByIdAsync(processId, cancellationToken);
        if (instance is null)
        {
            if (!canInitiate || initialStateFactory is null)
            {
                throw new ProcessNotFoundException(
                    $"Process '{handler.Type.Value}' with ID '{processId}' not found and incoming message cannot initiate it.");
            }

            var initialState = initialStateFactory(eventMessage);
            var created = ProcessInstance<TState>.Create(
                id: processId,
                type: handler.Type,
                version: handler.Version,
                correlationId: correlationId,
                initialState: initialState,
                now: _timeProvider.GetUtcNow());

            ProcessDiagnostics.RecordProcessStarted(handler.Type.Value, handler.Version.Value);
            return (created, false);
        }

        if (instance.Status is ProcessStatus.Completed or ProcessStatus.Compensated or ProcessStatus.Failed)
        {
            return (instance, true);
        }

        return (instance, false);
    }

    private static ProcessExecutionResult<TState> HandleSuccessSave(
        string processType,
        int processVersion,
        ProcessInstance<TState> updatedInstance,
        ProcessTransitionResult<TState> transitionResult,
        Stopwatch stopwatch)
    {
        ProcessDiagnostics.RecordTransitionDuration(processType, stopwatch.Elapsed.TotalMilliseconds);

        if (updatedInstance.Status == ProcessStatus.Completed)
        {
            ProcessDiagnostics.RecordProcessCompleted(processType, processVersion);
        }
        else if (updatedInstance.Status == ProcessStatus.Compensated)
        {
            ProcessDiagnostics.RecordProcessCompensated(processType, processVersion);
        }
        else if (updatedInstance.Status == ProcessStatus.Failed)
        {
            ProcessDiagnostics.RecordProcessFailed(processType, processVersion, transitionResult.FailureReason);
        }

        return new ProcessExecutionResult<TState>(
            instance: updatedInstance,
            effects: transitionResult.Effects,
            saveResult: ProcessSaveResult.Success);
    }

    private async ValueTask<int> HandleConcurrencyRetryAsync(
        string processType,
        int processVersion,
        ProcessId processId,
        Revision revision,
        int attempt,
        CancellationToken cancellationToken)
    {
        ProcessDiagnostics.RecordConcurrencyConflict(processType, processVersion);
        attempt++;

        if (attempt > _maxConcurrencyRetries)
        {
            throw new ConcurrencyConflictException(processId, revision);
        }

        await Task.Delay(_backoffStrategy(attempt), cancellationToken);
        return attempt;
    }

    /// <summary>
    /// Executes reverse-order compensation steps for a saga using optimistic concurrency control.
    /// </summary>
    /// <typeparam name="TSaga">The saga definition and compensation handler type.</typeparam>
    /// <param name="processId">The unique process identifier of the saga to compensate.</param>
    /// <param name="recordedSteps">The list of steps recorded during forward execution.</param>
    /// <param name="saga">The saga instance providing compensation logic.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="ProcessExecutionResult{TState}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recordedSteps"/> or <paramref name="saga"/> is <see langword="null"/></exception>
    /// <exception cref="ProcessNotFoundException">The process instance was not found in storage</exception>
    /// <exception cref="ConcurrencyConflictException">Optimistic concurrency retries exceeded the configured maximum limit</exception>
    public async ValueTask<ProcessExecutionResult<TState>> CompensateAsync<TSaga>(
        ProcessId processId,
        IReadOnlyList<CompensationStep> recordedSteps,
        TSaga saga,
        CancellationToken cancellationToken = default)
        where TSaga : IProcess<TState>, ICompensationHandler<TState>
    {
        ArgumentNullException.ThrowIfNull(recordedSteps);
        ArgumentNullException.ThrowIfNull(saga);

        var processType = saga.Type.Value;
        var processVersion = saga.Version.Value;

        using var activity = ProcessDiagnostics.ActivitySource.StartActivity(
            $"Process {processType}.Compensate",
            ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag("process.id", processId.ToString());
            activity.SetTag("process.type", processType);
            activity.SetTag("process.version", processVersion);
        }

        var attempt = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var instance = await _store.GetByIdAsync(processId, cancellationToken);
            if (instance is null)
            {
                throw new ProcessNotFoundException($"Process '{saga.Type.Value}' with ID '{processId}' not found for compensation.");
            }

            if (instance.Status is ProcessStatus.Compensated or ProcessStatus.Failed)
            {
                return new ProcessExecutionResult<TState>(instance, Array.Empty<ProcessEffect>(), ProcessSaveResult.Success);
            }

            var context = new ProcessContext(
                processId: processId,
                correlationId: instance.CorrelationId,
                causationId: CausationId.NewId(),
                messageId: MessageId.NewId(),
                now: _timeProvider.GetUtcNow(),
                timeProvider: _timeProvider,
                cancellationToken: cancellationToken);

            var transitionResult = await SagaCompensationEngine.ExecuteCompensationAsync(
                instance.State,
                recordedSteps,
                saga,
                context);

            var updatedInstance = instance.Advance(
                newState: transitionResult.State,
                newStatus: transitionResult.Status,
                now: _timeProvider.GetUtcNow());

            var saveResult = await _store.SaveAsync(updatedInstance, cancellationToken);

            if (saveResult == ProcessSaveResult.Success)
            {
                return HandleSuccessSave(processType, processVersion, updatedInstance, transitionResult, stopwatch);
            }

            if (saveResult == ProcessSaveResult.ConcurrencyConflict)
            {
                attempt = await HandleConcurrencyRetryAsync(processType, processVersion, processId, instance.Revision, attempt, cancellationToken);
                continue;
            }

            return new ProcessExecutionResult<TState>(updatedInstance, transitionResult.Effects, saveResult);
        }
    }
}





