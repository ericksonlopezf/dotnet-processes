// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents errors that occur during process manager or saga execution.
/// </summary>
public class ProcessException : Exception
{
    /// <summary>
    /// Gets the process identifier associated with the exception, if available.
    /// </summary>
    public ProcessId? ProcessId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class.
    /// </summary>
    public ProcessException()
        : base("An error occurred during process execution.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProcessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public ProcessException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a specified error message, process identifier, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="processId">The process identifier associated with the exception, or <see langword="null"/>.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public ProcessException(string message, ProcessId? processId, Exception? innerException = null)
        : base(message, innerException)
    {
        ProcessId = processId;
    }
}

/// <summary>
/// Represents errors caused by attempting to access a process instance that was not found in storage.
/// </summary>
public sealed class ProcessNotFoundException : ProcessException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNotFoundException"/> class.
    /// </summary>
    public ProcessNotFoundException()
        : base("Process instance was not found in storage.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProcessNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public ProcessNotFoundException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNotFoundException"/> class for the specified process identifier.
    /// </summary>
    /// <param name="processId">The unique process identifier that was not found.</param>
    public ProcessNotFoundException(ProcessId processId)
        : base($"Process instance with ID '{processId}' was not found in storage.", processId)
    {
    }
}

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

/// <summary>
/// Represents errors caused by attempting an invalid or forbidden process state transition.
/// </summary>
public sealed class InvalidProcessTransitionException : ProcessException
{
    /// <summary>
    /// Gets the current lifecycle status of the process instance.
    /// </summary>
    public ProcessStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the target lifecycle status that was attempted.
    /// </summary>
    public ProcessStatus AttemptedStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProcessTransitionException"/> class.
    /// </summary>
    public InvalidProcessTransitionException()
        : base("An invalid process state transition was attempted.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProcessTransitionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidProcessTransitionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProcessTransitionException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public InvalidProcessTransitionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProcessTransitionException"/> class with transition status details.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="currentStatus">The current status of the process.</param>
    /// <param name="attemptedStatus">The attempted target status.</param>
    /// <param name="reason">The explanation for why the transition is invalid.</param>
    public InvalidProcessTransitionException(ProcessId processId, ProcessStatus currentStatus, ProcessStatus attemptedStatus, string reason)
        : base($"Invalid state transition for process '{processId}' from '{currentStatus}' to '{attemptedStatus}': {reason}", processId)
    {
        CurrentStatus = currentStatus;
        AttemptedStatus = attemptedStatus;
    }
}

/// <summary>
/// Represents errors caused by a failure during saga compensation execution.
/// </summary>
public sealed class CompensationFailedException : ProcessException
{
    /// <summary>
    /// Gets the name of the step that failed to compensate.
    /// </summary>
    public string StepName { get; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationFailedException"/> class.
    /// </summary>
    public CompensationFailedException()
        : base("A saga compensation step failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationFailedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CompensationFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationFailedException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public CompensationFailedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationFailedException"/> class with the step name and failure details.
    /// </summary>
    /// <param name="processId">The unique process identifier.</param>
    /// <param name="stepName">The name of the step that failed to compensate.</param>
    /// <param name="reason">The explanation of the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public CompensationFailedException(ProcessId processId, string stepName, string reason, Exception? innerException = null)
        : base($"Compensation step '{stepName}' failed for saga '{processId}': {reason}", processId, innerException)
    {
        StepName = stepName;
    }
}




