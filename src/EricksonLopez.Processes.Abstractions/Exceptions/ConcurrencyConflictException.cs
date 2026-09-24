// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents errors caused by an optimistic concurrency conflict during process persistence.
/// </summary>
public sealed class ConcurrencyConflictException : ProcessException
{
    /// <summary>
    /// Gets the revision expected by the storage persistence operation.
    /// </summary>
    public Revision ExpectedRevision { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    public ConcurrencyConflictException()
        : base("A concurrency conflict occurred during process persistence.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public ConcurrencyConflictException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class with the specified process identifier and expected revision.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="expectedRevision">The expected revision token.</param>
    public ConcurrencyConflictException(ProcessId processId, Revision expectedRevision)
        : base($"Concurrency conflict detected for process '{processId}'. Expected revision '{expectedRevision}'.", processId)
    {
        ExpectedRevision = expectedRevision;
    }
}
