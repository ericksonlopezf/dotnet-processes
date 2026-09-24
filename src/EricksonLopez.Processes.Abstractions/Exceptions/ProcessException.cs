// Copyright © Erickson Lopez. MIT License.
using System;

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
