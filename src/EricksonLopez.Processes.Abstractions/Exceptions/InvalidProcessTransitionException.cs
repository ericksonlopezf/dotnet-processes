// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

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
