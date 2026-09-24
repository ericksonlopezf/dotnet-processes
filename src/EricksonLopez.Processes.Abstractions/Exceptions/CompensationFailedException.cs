// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Processes.Abstractions;

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
