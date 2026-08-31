// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents a recorded milestone of a completed saga step, storing the payload required for reverse compensation.
/// </summary>
public sealed record CompensationStep
{
    /// <summary>
    /// Gets the unique logical step name.
    /// </summary>
    public string StepName { get; init; }

    /// <summary>
    /// Gets the payload recorded during forward execution.
    /// </summary>
    public object Payload { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the step was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationStep"/> record with the specified step name, payload, and timestamp.
    /// </summary>
    /// <param name="stepName">The unique logical name of the step.</param>
    /// <param name="payload">The payload recorded during forward execution.</param>
    /// <param name="recordedAt">The UTC timestamp when the step was recorded.</param>
    /// <exception cref="ArgumentException"><paramref name="stepName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/></exception>
    public CompensationStep(string stepName, object payload, DateTimeOffset recordedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        StepName = stepName;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        RecordedAt = recordedAt;
    }

    /// <summary>
    /// Creates a new <see cref="CompensationStep"/> instance with the specified step name, payload, and timestamp.
    /// </summary>
    /// <typeparam name="TPayload">The type of the recorded payload.</typeparam>
    /// <param name="stepName">The unique logical name of the step.</param>
    /// <param name="payload">The recorded step payload.</param>
    /// <param name="recordedAt">The UTC timestamp when the step was recorded.</param>
    /// <returns>A new <see cref="CompensationStep"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="stepName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/></exception>
    public static CompensationStep Create<TPayload>(string stepName, TPayload payload, DateTimeOffset recordedAt) where TPayload : notnull =>
        new(stepName, payload, recordedAt);

    /// <summary>
    /// Extracts the recorded payload cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <returns>The payload cast to <typeparamref name="T"/>.</returns>
    public T ExtractPayload<T>() => (T)Payload;

    /// <summary>
    /// Attempts to extract the recorded payload as the specified type.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="payload">When this method returns, contains the typed payload if successful; otherwise, the default value for <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the payload is of type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
    public bool TryExtractPayload<T>(out T? payload)
    {
        if (Payload is T typed)
        {
            payload = typed;
            return true;
        }

        payload = default;
        return false;
    }
}




