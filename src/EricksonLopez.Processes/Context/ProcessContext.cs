// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Processes.Abstractions;

namespace EricksonLopez.Processes;

/// <summary>
/// Provides contextual execution metadata and services to a process handler or saga step.
/// </summary>
public sealed class ProcessContext
{
    /// <summary>
    /// Gets the unique identifier of the process instance.
    /// </summary>
    public ProcessId ProcessId { get; }

    /// <summary>
    /// Gets the overarching business correlation identifier.
    /// </summary>
    public CorrelationId CorrelationId { get; }

    /// <summary>
    /// Gets the causation identifier of the trigger message.
    /// </summary>
    public CausationId CausationId { get; }

    /// <summary>
    /// Gets the unique identifier of the incoming message.
    /// </summary>
    public MessageId MessageId { get; }

    /// <summary>
    /// Gets the deterministic UTC timestamp when execution began.
    /// </summary>
    public DateTimeOffset Now { get; }

    /// <summary>
    /// Gets the time provider used for timestamp and delay calculations.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets contextual key-value items passed into this execution.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Items { get; }

    /// <summary>
    /// Gets the cancellation token for the current execution step.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessContext"/> class with the specified metadata and services.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="correlationId">The business correlation identifier.</param>
    /// <param name="causationId">The causation identifier of the trigger message.</param>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="now">The UTC timestamp when execution began.</param>
    /// <param name="timeProvider">The optional time provider instance, or <see langword="null"/> to use <see cref="TimeProvider.System"/>.</param>
    /// <param name="items">The optional contextual items dictionary, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the execution step.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Process execution context encapsulates all required pipeline parameters")]
    public ProcessContext(
        ProcessId processId,
        CorrelationId correlationId,
        CausationId causationId,
        MessageId messageId,
        DateTimeOffset now,
        TimeProvider? timeProvider = null,
        IReadOnlyDictionary<string, object?>? items = null,
        CancellationToken cancellationToken = default)
    {
        ProcessId = processId;
        CorrelationId = correlationId;
        CausationId = causationId;
        MessageId = messageId;
        Now = now;
        TimeProvider = timeProvider ?? TimeProvider.System;
        Items = items ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Creates a new <see cref="ProcessContext"/> with defaults for optional message identifiers and timestamps.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="correlationId">The business correlation identifier.</param>
    /// <param name="causationId">The optional causation identifier, or <see langword="null"/> to derive from the message identifier.</param>
    /// <param name="messageId">The optional message identifier, or <see langword="null"/> to generate a new time-ordered identifier.</param>
    /// <param name="timeProvider">The optional time provider instance, or <see langword="null"/> to use <see cref="TimeProvider.System"/>.</param>
    /// <param name="items">The optional contextual items dictionary, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the execution step.</param>
    /// <returns>A new <see cref="ProcessContext"/> instance.</returns>
    public static ProcessContext Create(
        ProcessId processId,
        CorrelationId correlationId,
        CausationId? causationId = null,
        MessageId? messageId = null,
        TimeProvider? timeProvider = null,
        IReadOnlyDictionary<string, object?>? items = null,
        CancellationToken cancellationToken = default)
    {
        var provider = timeProvider ?? TimeProvider.System;
        var msgId = messageId ?? MessageId.NewId();
        var causeId = causationId ?? CausationId.From(msgId.Value);

        return new ProcessContext(
            processId: processId,
            correlationId: correlationId,
            causationId: causeId,
            messageId: msgId,
            now: provider.GetUtcNow(),
            timeProvider: provider,
            items: items,
            cancellationToken: cancellationToken);
    }
}




