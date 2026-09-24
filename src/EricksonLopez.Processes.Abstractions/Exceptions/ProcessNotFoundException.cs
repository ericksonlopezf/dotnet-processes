// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNotFoundException"/> class with a specified error message and process identifier.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="processId">The unique process identifier that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/>.</param>
    public ProcessNotFoundException(string message, ProcessId processId, Exception? innerException = null)
        : base(message, processId, innerException)
    {
    }
}
